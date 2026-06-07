using System;
using Client.Controls;
using Client.Envir;
using Client.Rendering;
using Client.Scenes;
using Library;
using C = Library.Network.ClientPackets;

namespace Client
{
    /// <summary>
    /// Headless end-to-end flow driver, gated by the ZIRCON_AUTOTEST environment variable.
    /// Drives registration -> login -> character creation -> entering the game by enqueuing the
    /// same client packets the UI would, detecting scene transitions, and capturing a back-buffer
    /// screenshot at each stage so the full port can be validated without manual interaction.
    ///
    /// Idempotent: re-runs reuse the same account/character (registration/creation simply fail if
    /// they already exist, and the flow continues with login / start).
    /// </summary>
    public static class AutoTest
    {
        private const string EMail = "autotest@test.com";
        private const string Password = "test12345";
        private const string CharacterName = "AutoHero";
        private const string ShotDir = "/tmp/zircon_autotest";

        private enum Stage { Idle, Login, AwaitSelect, CreateChar, AwaitChar, StartGame, AwaitGame, Done, WalkTest }
        private static int _walkTicks;
        private static System.Drawing.Point _walkStart;

        private static bool _enabled;
        private static bool _checked;
        private static Stage _stage = Stage.Idle;
        private static DateTime _nextAction = DateTime.MinValue;
        private static string _pendingShot;
        private static int _shotSettle;

        public static bool Enabled
        {
            get
            {
                if (!_checked)
                {
                    _checked = true;
                    _enabled = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ZIRCON_AUTOTEST"));
                    if (_enabled)
                        try { System.IO.Directory.CreateDirectory(ShotDir); } catch { }
                }
                return _enabled;
            }
        }

        private static void Log(string msg)
        {
            Console.WriteLine($"[AUTOTEST] {msg}");
            Console.Out.Flush();
        }

        private static void Capture(string name)
        {
            _pendingShot = $"{ShotDir}/{name}.ppm";
            _shotSettle = 30; // let the UI settle a few frames before grabbing
        }

        private static void Wait(double seconds) => _nextAction = CEnvir.Now.AddSeconds(seconds);
        private static bool Ready => CEnvir.Now >= _nextAction;

        public static void Tick()
        {
            if (!Enabled) return;

            // Handle a pending screenshot (after a short settle delay).
            if (_pendingShot != null)
            {
                if (_shotSettle-- > 0) return;
                Client.Rendering.SDL3OpenGL.SDL3OpenGLRenderingPipeline.CaptureRequestPath = _pendingShot;
                Log($"capture -> {_pendingShot}");
                _pendingShot = null;
                Wait(1.0);
                return;
            }

            if (!Ready) return;

            switch (_stage)
            {
                case Stage.Idle:
                    if (DXControl.ActiveScene is LoginScene login && CEnvir.Connection != null &&
                        CEnvir.Connection.ServerConnected && CEnvir.Loaded && login.LoginBox.Visible)
                    {
                        Log("at login screen; registering account");
                        Capture("01_login");
                        CEnvir.Enqueue(new C.NewAccount
                        {
                            EMailAddress = EMail,
                            Password = Password,
                            RealName = "Auto Test",
                            BirthDate = new DateTime(1990, 1, 1),
                            Referral = string.Empty,
                            CheckSum = CEnvir.C,
                        });
                        _stage = Stage.Login;
                        Wait(2.0); // allow account creation (or "exists") to process
                    }
                    break;

                case Stage.Login:
                    Log("logging in");
                    CEnvir.Enqueue(new C.Login { EMailAddress = EMail, Password = Password, CheckSum = CEnvir.C });
                    _stage = Stage.AwaitSelect;
                    Wait(8.0); // timeout guard
                    break;

                case Stage.AwaitSelect:
                    if (DXControl.ActiveScene is SelectScene)
                    {
                        Log("reached character select");
                        Capture("02_select");
                        _stage = Stage.CreateChar;
                        Wait(1.5);
                    }
                    else if (Ready) // timed out waiting for select
                    {
                        Log("ERROR: did not reach SelectScene after login (account may need activation, or login failed)");
                        _stage = Stage.Done;
                    }
                    break;

                case Stage.CreateChar:
                    if (DXControl.ActiveScene is SelectScene sc1)
                    {
                        if (sc1.SelectBox != null && sc1.SelectBox.CharacterList.Count > 0)
                        {
                            Log("character already exists; skipping creation");
                            _stage = Stage.StartGame;
                        }
                        else
                        {
                            Log("creating character");
                            CEnvir.Enqueue(new C.NewCharacter
                            {
                                CharacterName = CharacterName,
                                Class = MirClass.Warrior,
                                Gender = MirGender.Male,
                                HairType = 1,
                                HairColour = System.Drawing.Color.FromArgb(255, 120, 80, 40),
                                ArmourColour = System.Drawing.Color.FromArgb(255, 120, 120, 200),
                                CheckSum = CEnvir.C,
                            });
                            _stage = Stage.AwaitChar;
                            Wait(8.0);
                        }
                    }
                    break;

                case Stage.AwaitChar:
                    if (DXControl.ActiveScene is SelectScene sc2 && sc2.SelectBox != null && sc2.SelectBox.CharacterList.Count > 0)
                    {
                        Log($"character created: {sc2.SelectBox.CharacterList[0].CharacterName}");
                        Capture("03_character");
                        _stage = Stage.StartGame;
                        Wait(1.5);
                    }
                    else if (Ready)
                    {
                        Log("ERROR: character was not created (name may be taken or invalid)");
                        _stage = Stage.Done;
                    }
                    break;

                case Stage.StartGame:
                    if (DXControl.ActiveScene is SelectScene sc3 && sc3.SelectBox != null && sc3.SelectBox.CharacterList.Count > 0)
                    {
                        int index = sc3.SelectBox.CharacterList[0].CharacterIndex;
                        Log($"starting game with character index {index}");
                        CEnvir.Enqueue(new C.StartGame { CharacterIndex = index });
                        _stage = Stage.AwaitGame;
                        Wait(15.0);
                    }
                    break;

                case Stage.AwaitGame:
                    if (DXControl.ActiveScene is GameScene && GameScene.Game != null)
                    {
                        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ZIRCON_WALKTEST")))
                        {
                            Log("IN GAME — starting walk test (holding left button toward up-left).");
                            _walkTicks = 0;
                            _walkStart = Client.Models.MapObject.User?.CurrentLocation ?? System.Drawing.Point.Empty;
                            _stage = Stage.WalkTest;
                            Wait(0);
                            break;
                        }
                        Log("IN GAME. Waiting for world to load, then capturing.");
                        _stage = Stage.Done;
                        Capture("04_ingame");
                        Wait(4.0); // let the map/player/objects load before the shot
                    }
                    else if (Ready)
                    {
                        Log("ERROR: did not reach GameScene after StartGame");
                        _stage = Stage.Done;
                    }
                    break;

                case Stage.WalkTest:
                {
                    var gs = DXControl.ActiveScene as GameScene;
                    if (gs?.MapControl == null) { _enabled = false; Log("WALKTEST: no map"); break; }
                    // Hold the left button over a point up-left of the player so ProcessInput keeps
                    // issuing move actions; UpdateGame's per-frame OnMouseMove keeps MouseControl on
                    // the map via CEnvir.MouseLocation.
                    // Sweep the target a little each second so we don't get stuck pointing at a
                    // monster (clicking a monster targets it instead of moving).
                    var targets = new[] { new System.Drawing.Point(512, 150), new System.Drawing.Point(850, 384), new System.Drawing.Point(512, 620), new System.Drawing.Point(180, 384) };
                    CEnvir.MouseLocation = targets[(_walkTicks / 45) % targets.Length];
                    gs.MapControl.MapButtons |= System.Windows.Forms.MouseButtons.Left;
                    if (_walkTicks % 15 == 0)
                        Log($"WALKTEST: frame {_walkTicks} user={Client.Models.MapObject.User?.CurrentLocation} mouseTgt={CEnvir.MouseLocation} mapLoc={gs.MapControl.MapLocation} mouseObj={(Client.Models.MapObject.MouseObject == null ? "null" : Client.Models.MapObject.MouseObject.Race.ToString())}");
                    if (++_walkTicks >= 180)
                    {
                        gs.MapControl.MapButtons &= ~System.Windows.Forms.MouseButtons.Left;
                        Log($"WALKTEST: done. start={_walkStart} end={Client.Models.MapObject.User?.CurrentLocation} (changed={_walkStart != (Client.Models.MapObject.User?.CurrentLocation ?? System.Drawing.Point.Empty)})");
                        Capture("06_walktest");
                        _enabled = false;
                    }
                    break;
                }

                case Stage.Done:
                    if (DXControl.ActiveScene is GameScene gsDiag && GameScene.Game != null &&
                        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ZIRCON_INGAMEDIAG")) && _pendingShot == null)
                    {
                        RunInGameDiag(gsDiag);
                        _enabled = false;
                        break;
                    }
                    // Final shot after the world has settled, then stop acting.
                    if (DXControl.ActiveScene is GameScene && GameScene.Game != null && _pendingShot == null)
                    {
                        Capture("05_ingame_settled");
                        _enabled = false; // one final capture, then idle
                        Log("flow complete");
                    }
                    else
                    {
                        _enabled = false;
                        Log("flow ended");
                    }
                    break;
            }
        }

        /// <summary>
        /// Diagnoses in-game mouse input: simulates a mouse move + left button down over the map and
        /// reports whether the MapControl becomes the MouseControl and receives the button (movement
        /// requires MouseControl == MapControl in MapControl.ProcessInput).
        /// </summary>
        private static void RunInGameDiag(GameScene gs)
        {
            var map = gs.MapControl;
            if (map == null) { Log("INGAMEDIAG: MapControl is null"); return; }

            Log($"INGAMEDIAG: MapControl IsControl={map.IsControl} PassThrough={map.PassThrough} IsVisible={map.IsVisible} Enabled={map.Enabled} DisplayArea={map.DisplayArea}");
            Log($"INGAMEDIAG: scene children={gs.Controls.Count}; user={Client.Models.MapObject.User?.CurrentLocation}");

            int mx = 412, my = 284; // a point on the open map, away from HUD/minimap
            CEnvir.MouseLocation = new System.Drawing.Point(mx, my);
            gs.OnMouseMove(new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.None, 0, mx, my, 0));
            var mc = DXControl.MouseControl;
            Log($"INGAMEDIAG: after mousemove({mx},{my}) MouseControl={(mc == null ? "null" : mc.GetType().Name)} ==MapControl? {ReferenceEquals(mc, map)}");

            gs.OnMouseDown(new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 1, mx, my, 0));
            Log($"INGAMEDIAG: after mousedown MapButtons={map.MapButtons} MapLocation={map.MapLocation} MouseControl==MapControl? {ReferenceEquals(DXControl.MouseControl, map)}");
        }
    }
}
