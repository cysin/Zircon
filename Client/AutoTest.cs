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

        private enum Stage { Idle, Login, AwaitSelect, CreateChar, AwaitChar, StartGame, AwaitGame, Done }

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

                case Stage.Done:
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
    }
}
