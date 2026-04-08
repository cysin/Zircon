using Client.Controls;
using Client.Envir;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SDL = Client.Platform.SDL3.SDL3Native;
using SDL3TTF = Client.Platform.SDL3.SDL3TTF;

namespace Client.Rendering.SDL3OpenGL
{
    public sealed class SDL3OpenGLRenderingPipeline : IRenderingPipeline
    {
        // ── State ───────────────────────────────────────────────────────────

        private IntPtr _window;
        private IntPtr _glContext;
        private bool _ownsWindow;

        private GLManager _manager;
        private GLSpriteRenderer _renderer;

        private float _opacity = 1f;
        private bool _blending;
        private float _blendRate = 1f;
        private BlendMode _blendMode = BlendMode.NORMAL;
        private float _lineWidth = 1f;
        private bool _fullscreen;

        private readonly List<ITextureCacheItem> _controlCache = new();
        private readonly List<ITextureCacheItem> _textureCache = new();
        private readonly List<ISoundCacheItem> _soundCache = new();

        private List<Size> _validResolutions;

        public string Id => RenderingPipelineIds.SDL3OpenGL;

        // ── Initialization ──────────────────────────────────────────────────

        public void Initialize(RenderingPipelineContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            object target = context.RenderTarget;
            Size gameSize = Config.GameSize;

            if (target is Client.Platform.SDL3.SDL3GameWindow sdl3Window)
            {
                // SDL3GameWindow was passed — reuse its window and GL context.
                _window = sdl3Window.NativeHandle;
                _glContext = sdl3Window.GLContext;
                _ownsWindow = false;

                SDL3Native.SDL_GL_MakeCurrent(_window, _glContext);
                SDL3Native.SDL_GL_SetSwapInterval(Config.VSync ? 1 : 0);

                GL.Initialize();
                GL.glEnable(GL.GL_BLEND);
                GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
                GL.glPixelStorei(0x0CF5, 1);

                SDL3Native.SDL_GetWindowSize(_window, out int sw, out int sh);
                GL.glViewport(0, 0, sw, sh);

                _manager = new GLManager();
                _manager.Initialize(new Size(sw, sh));
                _renderer = new GLSpriteRenderer();
                _renderer.Initialize();
                _validResolutions = QueryDisplayModes();
                _fullscreen = Config.FullScreen;
                SDL3TTF.EnsureInitialized();
                return;
            }
            else if (target is IntPtr existingWindow && existingWindow != IntPtr.Zero)
            {
                // An SDL3 window handle was passed in directly.
                _window = existingWindow;
                _ownsWindow = false;
            }
            else
            {
                // Create our own SDL3 window.
                if (!SDL3Native.SDL_Init(SDL3Native.SDL_INIT_VIDEO | SDL3Native.SDL_INIT_EVENTS))
                    throw new InvalidOperationException($"SDL_Init failed: {SDL3Native.GetError()}");

                SDL3Native.SDL_GL_SetAttribute(SDL3Native.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
                SDL3Native.SDL_GL_SetAttribute(SDL3Native.SDL_GL_CONTEXT_MINOR_VERSION, 3);
                SDL3Native.SDL_GL_SetAttribute(SDL3Native.SDL_GL_CONTEXT_PROFILE_MASK, SDL3Native.SDL_GL_CONTEXT_PROFILE_CORE);
                SDL3Native.SDL_GL_SetAttribute(SDL3Native.SDL_GL_DOUBLEBUFFER, 1);

                ulong flags = SDL3Native.SDL_WINDOW_OPENGL | SDL3Native.SDL_WINDOW_HIGH_PIXEL_DENSITY;
                if (Config.FullScreen)
                    flags |= SDL3Native.SDL_WINDOW_FULLSCREEN;
                if (Config.Borderless)
                    flags |= SDL3Native.SDL_WINDOW_BORDERLESS;

                _window = SDL3Native.SDL_CreateWindow("Zircon", gameSize.Width, gameSize.Height, flags);
                if (_window == IntPtr.Zero)
                    throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL3Native.GetError()}");

                _ownsWindow = true;
            }

            _fullscreen = Config.FullScreen;

            // Create OpenGL context
            _glContext = SDL3Native.SDL_GL_CreateContext(_window);
            if (_glContext == IntPtr.Zero)
                throw new InvalidOperationException($"SDL_GL_CreateContext failed: {SDL3Native.GetError()}");

            SDL3Native.SDL_GL_MakeCurrent(_window, _glContext);
            SDL3Native.SDL_GL_SetSwapInterval(Config.VSync ? 1 : 0);

            // Load GL function pointers
            GL.Initialize();

            // Set initial GL state
            GL.glEnable(GL.GL_BLEND);
            GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
            GL.glPixelStorei(0x0CF5, 1); // GL_UNPACK_ALIGNMENT = 1

            SDL3Native.SDL_GetWindowSize(_window, out int w, out int h);
            GL.glViewport(0, 0, w, h);

            // Initialise subsystems
            _manager = new GLManager();
            _manager.Initialize(new Size(w, h));

            _renderer = new GLSpriteRenderer();
            _renderer.Initialize();

            _validResolutions = QueryDisplayModes();

            SDL3TTF.EnsureInitialized();
        }

        // ── Message loop ────────────────────────────────────────────────────

        public void RunMessageLoop(object window, Action loop)
        {
            if (loop == null)
                throw new ArgumentNullException(nameof(loop));

            bool running = true;
            while (running)
            {
                while (SDL.SDL_PollEvent(out Client.Platform.SDL3.SDL_Event e))
                {
                    switch (e.type)
                    {
                        case SDL.SDL_EVENT_QUIT:
                            running = false;
                            break;

                        case SDL.SDL_EVENT_WINDOW_CLOSE_REQUESTED:
                            running = false;
                            break;

                        case SDL.SDL_EVENT_MOUSE_MOTION:
                        {
                            var pt = new Point((int)e.motion.x, (int)e.motion.y);
                            CEnvir.MouseLocation = pt;
                            var args = new MouseEventArgs(MouseButtons.None, 0, pt.X, pt.Y, 0);
                            try { DXControl.ActiveScene?.OnMouseMove(args); }
                            catch (Exception ex) { CEnvir.SaveException(ex); }
                            break;
                        }

                        case SDL.SDL_EVENT_MOUSE_BUTTON_DOWN:
                        {
                            var btn = MapMouseButton(e.button.button);
                            var args = new MouseEventArgs(btn, (int)e.button.clicks, (int)e.button.x, (int)e.button.y, 0);
                            try { DXControl.ActiveScene?.OnMouseDown(args); }
                            catch (Exception ex) { CEnvir.SaveException(ex); }
                            break;
                        }

                        case SDL.SDL_EVENT_MOUSE_BUTTON_UP:
                        {
                            var btn = MapMouseButton(e.button.button);
                            var args = new MouseEventArgs(btn, 0, (int)e.button.x, (int)e.button.y, 0);
                            // Send Click BEFORE MouseUp, because MouseUp clears FocusControl
                            // and the scene's OnMouseClick checks MouseControl == FocusControl
                            try { DXControl.ActiveScene?.OnMouseClick(args); }
                            catch (Exception ex) { CEnvir.SaveException(ex); }

                            try { DXControl.ActiveScene?.OnMouseUp(args); }
                            catch (Exception ex) { CEnvir.SaveException(ex); }
                            break;
                        }

                        case SDL.SDL_EVENT_MOUSE_WHEEL:
                        {
                            int delta = (int)(e.wheel.y * 120); // Match Windows WHEEL_DELTA
                            var args = new MouseEventArgs(MouseButtons.None, 0,
                                CEnvir.MouseLocation.X, CEnvir.MouseLocation.Y, delta);
                            try { DXControl.ActiveScene?.OnMouseWheel(args); }
                            catch (Exception ex) { CEnvir.SaveException(ex); }
                            break;
                        }

                        case SDL.SDL_EVENT_KEY_DOWN:
                        {
                            var key = MapKey(e.key.scancode);
                            Keys mods = Keys.None;
                            if ((e.key.mod & 0x0003) != 0) mods |= Keys.Shift;
                            if ((e.key.mod & 0x00C0) != 0) mods |= Keys.Control;
                            if ((e.key.mod & 0x0300) != 0) mods |= Keys.Alt;

                            CEnvir.Shift = (mods & Keys.Shift) != 0;
                            CEnvir.Ctrl = (mods & Keys.Control) != 0;
                            CEnvir.Alt = (mods & Keys.Alt) != 0;

                            var args = new KeyEventArgs(key | mods);

                            if (CEnvir.Alt && args.KeyCode == Keys.Enter)
                            {
                                ToggleFullScreen();
                                break;
                            }

                            // Route to active TextBox first
                            var activeTextBox = DXTextBox.ActiveTextBox;
                            if (activeTextBox?.TextBox != null)
                            {
                                activeTextBox.TextBox.OnKeyDownPublic(args);
                                if (args.Handled) break;
                            }

                            try { DXControl.ActiveScene?.OnKeyDown(args); }
                            catch (Exception ex) { CEnvir.SaveException(ex); }
                            break;
                        }

                        case SDL.SDL_EVENT_KEY_UP:
                        {
                            var key = MapKey(e.key.scancode);
                            Keys mods = Keys.None;
                            if ((e.key.mod & 0x0003) != 0) mods |= Keys.Shift;
                            if ((e.key.mod & 0x00C0) != 0) mods |= Keys.Control;
                            if ((e.key.mod & 0x0300) != 0) mods |= Keys.Alt;

                            CEnvir.Shift = (mods & Keys.Shift) != 0;
                            CEnvir.Ctrl = (mods & Keys.Control) != 0;
                            CEnvir.Alt = (mods & Keys.Alt) != 0;

                            var args = new KeyEventArgs(key | mods);
                            try { DXControl.ActiveScene?.OnKeyUp(args); }
                            catch (Exception ex) { CEnvir.SaveException(ex); }
                            break;
                        }

                        case SDL.SDL_EVENT_TEXT_INPUT:
                        {
                            // Route text input to active TextBox first
                            var activeTextBox2 = DXTextBox.ActiveTextBox;
                            if (activeTextBox2?.TextBox != null)
                            {
                                unsafe
                                {
                                    string inputText = Marshal.PtrToStringUTF8((IntPtr)e.text.text);
                                    if (!string.IsNullOrEmpty(inputText))
                                    {
                                        foreach (char c in inputText)
                                        {
                                            var kpe = new KeyPressEventArgs(c);
                                            activeTextBox2.TextBox.OnKeyPressPublic(kpe);
                                        }
                                        activeTextBox2.TextureValid = false;
                                    }
                                }
                                break;
                            }

                            unsafe
                            {
                                string text = Marshal.PtrToStringUTF8((IntPtr)e.text.text);
                                if (!string.IsNullOrEmpty(text))
                                {
                                    foreach (char c in text)
                                    {
                                        var args = new KeyPressEventArgs(c);
                                        try { DXControl.ActiveScene?.OnKeyPress(args); }
                                        catch (Exception ex) { CEnvir.SaveException(ex); }
                                    }
                                }
                            }
                            break;
                        }

                        case SDL.SDL_EVENT_WINDOW_FOCUS_LOST:
                        {
                            CEnvir.Shift = false;
                            CEnvir.Alt = false;
                            CEnvir.Ctrl = false;
                            break;
                        }
                    }
                }

                if (!running)
                    break;

                loop();
            }
        }

        private static MouseButtons MapMouseButton(byte sdlButton)
        {
            return sdlButton switch
            {
                1 => MouseButtons.Left,
                2 => MouseButtons.Middle,
                3 => MouseButtons.Right,
                4 => MouseButtons.XButton1,
                5 => MouseButtons.XButton2,
                _ => MouseButtons.None,
            };
        }

        private static Keys MapKey(int scancode)
        {
            return scancode switch
            {
                4 => Keys.A, 5 => Keys.B, 6 => Keys.C, 7 => Keys.D,
                8 => Keys.E, 9 => Keys.F, 10 => Keys.G, 11 => Keys.H,
                12 => Keys.I, 13 => Keys.J, 14 => Keys.K, 15 => Keys.L,
                16 => Keys.M, 17 => Keys.N, 18 => Keys.O, 19 => Keys.P,
                20 => Keys.Q, 21 => Keys.R, 22 => Keys.S, 23 => Keys.T,
                24 => Keys.U, 25 => Keys.V, 26 => Keys.W, 27 => Keys.X,
                28 => Keys.Y, 29 => Keys.Z,
                30 => Keys.D1, 31 => Keys.D2, 32 => Keys.D3, 33 => Keys.D4,
                34 => Keys.D5, 35 => Keys.D6, 36 => Keys.D7, 37 => Keys.D8,
                38 => Keys.D9, 39 => Keys.D0,
                40 => Keys.Return, 41 => Keys.Escape, 42 => Keys.Back, 43 => Keys.Tab,
                44 => Keys.Space,
                45 => Keys.OemMinus, 46 => Keys.Oemplus,
                47 => Keys.OemOpenBrackets, 48 => Keys.OemCloseBrackets,
                49 => Keys.OemPipe, 51 => Keys.OemSemicolon,
                52 => Keys.OemQuotes, 53 => Keys.Oemtilde,
                54 => Keys.OemComma, 55 => Keys.OemPeriod, 56 => Keys.OemQuestion,
                58 => Keys.F1, 59 => Keys.F2, 60 => Keys.F3, 61 => Keys.F4,
                62 => Keys.F5, 63 => Keys.F6, 64 => Keys.F7, 65 => Keys.F8,
                66 => Keys.F9, 67 => Keys.F10, 68 => Keys.F11, 69 => Keys.F12,
                73 => Keys.Insert, 74 => Keys.Home, 75 => Keys.PageUp,
                76 => Keys.Delete, 77 => Keys.End, 78 => Keys.PageDown,
                79 => Keys.Right, 80 => Keys.Left, 81 => Keys.Down, 82 => Keys.Up,
                224 => Keys.ControlKey, 225 => Keys.ShiftKey, 226 => Keys.Menu,
                228 => Keys.ControlKey, 229 => Keys.ShiftKey, 230 => Keys.Menu,
                _ => Keys.None,
            };
        }

        // ── Frame rendering ─────────────────────────────────────────────────

        public bool RenderFrame(Action drawScene)
        {
            if (drawScene == null)
                throw new ArgumentNullException(nameof(drawScene));

            try
            {
                _manager.SetBackBuffer();
                _renderer.SetDefaultBlend();

                Size bbSize = _manager.GetBackBufferSize();
                GL.glViewport(0, 0, bbSize.Width, bbSize.Height);
                GL.glClearColor(0f, 0f, 0f, 1f);
                GL.glClear(GL.GL_COLOR_BUFFER_BIT);

                drawScene();

                SDL3Native.SDL_GL_SwapWindow(_window);
                return true;
            }
            catch (Exception ex)
            {
                CEnvir.SaveException(ex);
                return false;
            }
        }

        // ── Window management ───────────────────────────────────────────────

        public void ToggleFullScreen()
        {
            _fullscreen = !_fullscreen;
            Config.FullScreen = _fullscreen;
            SDL3Native.SDL_SetWindowFullscreen(_window, _fullscreen);

            if (!_fullscreen)
            {
                // Restore windowed size
                SDL3Native.SDL_SetWindowSize(_window, Config.GameSize.Width, Config.GameSize.Height);
            }

            UpdateBackBufferSize();
        }

        public void SetResolution(Size size)
        {
            Config.GameSize = size;
            SDL3Native.SDL_SetWindowSize(_window, size.Width, size.Height);
            UpdateBackBufferSize();
        }

        public void SetTargetMonitor(int monitorIndex)
        {
            // SDL3 handles multiple displays via SDL_GetDisplays.
            // For now, position the window at (0,0) of the requested display.
            // Full monitor enumeration can be refined later.
        }

        public void CenterOnSelectedMonitor()
        {
            // SDL3 auto-centres windows unless positioned explicitly.
            // A proper implementation would query the display bounds.
        }

        public void ResetDevice()
        {
            UpdateBackBufferSize();
        }

        public void OnSceneChanged(bool isGameScene)
        {
            if (!isGameScene)
                return;

            if (!Config.FullScreen)
                UpdateBackBufferSize();
        }

        public IReadOnlyList<Size> GetSupportedResolutions()
        {
            return _validResolutions ?? (IReadOnlyList<Size>)Array.Empty<Size>();
        }

        // ── Text measurement (SDL3_ttf) ─────────────────────────────────────

        public Size MeasureText(string text, GameFont font)
        {
            if (string.IsNullOrEmpty(text))
                return Size.Empty;

            nint ttf = Client.Platform.SDL3.SDL3TTF.GetFont(font.Size, font.Bold, font.Italic);
            if (ttf != nint.Zero)
            {
                if (Client.Platform.SDL3.SDL3TTF.TTF_GetStringSize(ttf, text, (nuint)0, out int w, out int h))
                    return new Size(w, h);
            }

            // Fallback
            float charWidth = font.Size * 0.6f;
            if (font.Bold) charWidth *= 1.1f;
            int width = (int)MathF.Ceiling(text.Length * charWidth);
            int height = (int)MathF.Ceiling(font.Size * 1.35f);
            return new Size(width, height);
        }

        public Size MeasureText(string text, GameFont font, Size proposedSize, GameTextFormatFlags format)
        {
            if (string.IsNullOrEmpty(text))
                return Size.Empty;

            nint ttf = Client.Platform.SDL3.SDL3TTF.GetFont(font.Size, font.Bold, font.Italic);
            if (ttf != nint.Zero)
            {
                if ((format & GameTextFormatFlags.WordBreak) != 0 && proposedSize.Width > 0)
                {
                    if (Client.Platform.SDL3.SDL3TTF.TTF_GetStringSizeWrapped(ttf, text, (nuint)0, proposedSize.Width, out int ww, out int wh))
                        return new Size(Math.Min(ww, proposedSize.Width), wh);
                }
                else
                {
                    if (Client.Platform.SDL3.SDL3TTF.TTF_GetStringSize(ttf, text, (nuint)0, out int w, out int h))
                        return new Size(w, h);
                }
            }

            // Fallback
            float charWidth = font.Size * 0.6f;
            if (font.Bold) charWidth *= 1.1f;
            int lineHeight = (int)MathF.Ceiling(font.Size * 1.35f);
            int singleLineWidth = (int)MathF.Ceiling(text.Length * charWidth);

            if ((format & GameTextFormatFlags.WordBreak) != 0 && proposedSize.Width > 0 && singleLineWidth > proposedSize.Width)
            {
                int charsPerLine = Math.Max(1, (int)(proposedSize.Width / charWidth));
                int lines = (text.Length + charsPerLine - 1) / charsPerLine;
                return new Size(proposedSize.Width, lines * lineHeight);
            }

            return new Size(Math.Min(singleLineWidth, proposedSize.Width > 0 ? proposedSize.Width : singleLineWidth), lineHeight);
        }

        public float GetHorizontalDpi()
        {
            return 96f;
        }

        // ── Colour conversion ───────────────────────────────────────────────

        public Color ConvertHslToRgb(float h, float s, float l)
        {
            float r, g, b;

            if (MathF.Abs(s) < float.Epsilon)
            {
                r = g = b = l;
            }
            else
            {
                float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
                float p = 2f * l - q;
                r = HueToRgb(p, q, h + 1f / 3f);
                g = HueToRgb(p, q, h);
                b = HueToRgb(p, q, h - 1f / 3f);
            }

            return Color.FromArgb((int)(r * 255), (int)(g * 255), (int)(b * 255));
        }

        private static float HueToRgb(float p, float q, float t)
        {
            if (t < 0f) t += 1f;
            if (t > 1f) t -= 1f;
            if (t < 1f / 6f) return p + (q - p) * 6f * t;
            if (t < 1f / 2f) return q;
            if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
            return p;
        }

        // ── Blend / opacity / line width state ──────────────────────────────

        public void SetOpacity(float opacity) => _opacity = opacity;
        public float GetOpacity() => _opacity;

        public void SetBlend(bool enabled, float rate, BlendMode mode)
        {
            _blending = enabled;
            _blendRate = rate;
            _blendMode = mode;

            if (!_blending)
                _renderer?.SetDefaultBlend();
        }

        public bool IsBlending() => _blending;
        public float GetBlendRate() => _blendRate;
        public BlendMode GetBlendMode() => _blendMode;
        public float GetLineWidth() => _lineWidth;

        public void SetLineWidth(float width)
        {
            if (width <= 0f)
                return;

            _lineWidth = width;
        }

        // ── Draw operations ─────────────────────────────────────────────────

        public void DrawLine(IReadOnlyList<LinePoint> points, Color colour)
        {
            if (points == null || points.Count < 2 || _renderer == null)
                return;

            Size vp = GetCurrentViewportSize();

            for (int i = 0; i < points.Count - 1; i++)
            {
                LinePoint start = points[i];
                LinePoint end = points[i + 1];
                _renderer.DrawLine(Snap(start.X), Snap(start.Y), Snap(end.X), Snap(end.Y),
                    colour, _opacity, _lineWidth, vp);
            }
        }

        private static float Snap(float v)
        {
            return MathF.Floor(v) + 0.5f;
        }

        public void DrawTexture(RenderTexture texture, Rectangle sourceRectangle, RectangleF destinationRectangle, Color colour)
        {
            if (!texture.IsValid || _renderer == null)
                return;

            uint texId = GLManager.ResolveTextureId(texture);
            Size texSize = GLManager.ResolveTextureSize(texture);
            if (texId == 0)
                return;

            Size vp = GetCurrentViewportSize();

            if (TryDrawSpriteEffect(texId, texSize.Width, texSize.Height, destinationRectangle, sourceRectangle, colour, Matrix3x2.Identity, vp))
                return;

            if (_blending && _blendMode != BlendMode.NONE)
            {
                _renderer.Draw(texId, texSize.Width, texSize.Height,
                    destinationRectangle, sourceRectangle, colour,
                    Matrix3x2.Identity, _blendMode, _opacity, _blendRate, vp);
                return;
            }

            // Standard alpha blend draw
            _renderer.SetDefaultBlend();
            _renderer.Draw(texId, texSize.Width, texSize.Height,
                destinationRectangle, sourceRectangle, colour,
                Matrix3x2.Identity, BlendMode.NONE, _opacity, _blendRate, vp);
        }

        public void DrawTexture(RenderTexture texture, Rectangle? sourceRectangle, Matrix3x2 transform, Vector3 center, Vector3 translation, Color colour)
        {
            if (!texture.IsValid || _renderer == null)
                return;

            uint texId = GLManager.ResolveTextureId(texture);
            Size texSize = GLManager.ResolveTextureSize(texture);
            if (texId == 0)
                return;

            Matrix3x2 finalTransform = transform;

            if (center.X != 0 || center.Y != 0)
                finalTransform = Matrix3x2.CreateTranslation(-center.X, -center.Y) * finalTransform;

            finalTransform.M31 += translation.X;
            finalTransform.M32 += translation.Y;

            float drawW = sourceRectangle.HasValue ? sourceRectangle.Value.Width : texSize.Width;
            float drawH = sourceRectangle.HasValue ? sourceRectangle.Value.Height : texSize.Height;
            RectangleF geom = new RectangleF(0, 0, drawW, drawH);

            Size vp = GetCurrentViewportSize();

            if (TryDrawSpriteEffect(texId, texSize.Width, texSize.Height, geom, sourceRectangle, colour, finalTransform, vp))
                return;

            if (_blending && _blendMode != BlendMode.NONE)
            {
                _renderer.Draw(texId, texSize.Width, texSize.Height,
                    geom, sourceRectangle, colour,
                    finalTransform, _blendMode, _opacity, _blendRate, vp);
                return;
            }

            _renderer.SetDefaultBlend();
            _renderer.Draw(texId, texSize.Width, texSize.Height,
                geom, sourceRectangle, colour,
                finalTransform, BlendMode.NONE, _opacity, _blendRate, vp);
        }

        private bool TryDrawSpriteEffect(uint texId, int texWidth, int texHeight,
                                          RectangleF geometry, Rectangle? sourceRectangle,
                                          Color colour, Matrix3x2 transform, Size viewportSize)
        {
            var effect = RenderingPipelineManager.GetSpriteShaderEffect();
            if (!effect.HasValue || _renderer == null)
                return false;

            switch (effect.Value.Kind)
            {
                case RenderingPipelineManager.SpriteShaderEffectKind.Outline:
                {
                    var outline = effect.Value.Outline;
                    if (_renderer.SupportsOutlineShader)
                    {
                        _renderer.DrawOutlined(texId, texWidth, texHeight,
                            geometry, sourceRectangle, colour, transform,
                            BlendMode.NONE, 1f, 1f,
                            outline.Colour, outline.Thickness, viewportSize);
                    }
                    // Outline renders beneath; the base sprite still needs to draw, so return false.
                    return false;
                }

                case RenderingPipelineManager.SpriteShaderEffectKind.Grayscale:
                {
                    _renderer.DrawGrayscale(texId, texWidth, texHeight,
                        geometry, sourceRectangle, colour, transform,
                        _blending ? _blendMode : BlendMode.NONE,
                        _opacity, _blendRate, viewportSize);
                    return true;
                }

                case RenderingPipelineManager.SpriteShaderEffectKind.DropShadow:
                {
                    var ds = effect.Value.DropShadow;
                    RectangleF shadowBounds = ds.VisibleBounds ?? geometry;

                    _renderer.DrawDropShadow(texId, texWidth, texHeight,
                        geometry, shadowBounds, sourceRectangle,
                        colour, transform,
                        _blending ? _blendMode : BlendMode.NONE,
                        _opacity, _blendRate,
                        ds.Colour, ds.Width, ds.StartOpacity, viewportSize);
                    return false;
                }
            }

            return false;
        }

        // ── Surface management ──────────────────────────────────────────────

        public RenderSurface GetCurrentSurface() => _manager.GetCurrentSurface();

        public void SetSurface(RenderSurface surface) => _manager.SetSurface(surface);

        public RenderSurface GetScratchSurface() => _manager.GetScratchSurface();

        public RenderTexture GetScratchTexture() => _manager.GetScratchTexture();

        public void ColorFill(RenderSurface surface, Rectangle rectangle, Color color)
        {
            _manager.ColorFill(surface, rectangle, color);
        }

        public RenderTargetResource CreateRenderTarget(Size size) => _manager.CreateRenderTarget(size);

        public void ReleaseRenderTarget(RenderTargetResource renderTarget)
        {
            if (!renderTarget.IsValid)
                return;

            _manager.ReleaseRenderTarget(renderTarget);
        }

        public Size GetBackBufferSize() => _manager.GetBackBufferSize();

        public void Clear(RenderClearFlags flags, Color colour, float z, int stencil, params Rectangle[] regions)
        {
            if ((flags & RenderClearFlags.Target) == 0)
                return;

            float r = colour.R / 255f;
            float g = colour.G / 255f;
            float b = colour.B / 255f;
            float a = colour.A / 255f * _opacity;

            if (regions != null && regions.Length > 0)
            {
                GLFramebufferHandle current = _manager.GetCurrentTargetHandle();
                int fbHeight = current?.Height ?? _manager.GetBackBufferSize().Height;

                GL.glEnable(GL.GL_SCISSOR_TEST);
                foreach (Rectangle region in regions)
                {
                    int scissorY = fbHeight - region.Bottom;
                    GL.glScissor(region.X, scissorY, region.Width, region.Height);
                    GL.glClearColor(r, g, b, a);
                    GL.glClear(GL.GL_COLOR_BUFFER_BIT);
                }
                GL.glDisable(GL.GL_SCISSOR_TEST);
            }
            else
            {
                GL.glClearColor(r, g, b, a);
                GL.glClear(GL.GL_COLOR_BUFFER_BIT);
            }
        }

        public void FlushSprite()
        {
            _renderer?.FlushSprite();
        }

        // ── Texture operations ──────────────────────────────────────────────

        public RenderTexture CreateTexture(Size size, RenderTextureFormat format, RenderTextureUsage usage, RenderTexturePool pool)
        {
            return _manager.CreateTexture(size, format, usage, pool);
        }

        public void ReleaseTexture(RenderTexture texture)
        {
            _manager.ReleaseTexture(texture);
        }

        public TextureLock LockTexture(RenderTexture texture, TextureLockMode mode)
        {
            if (texture.NativeHandle is not GLTextureHandle)
                throw new InvalidOperationException("Expected a GLTextureHandle.");

            return _manager.LockTexture(texture, mode);
        }

        // ── Cache management ────────────────────────────────────────────────

        public void RegisterControlCache(ITextureCacheItem control)
        {
            if (control != null && !_controlCache.Contains(control))
                _controlCache.Add(control);
        }

        public void UnregisterControlCache(ITextureCacheItem control)
        {
            if (control != null)
                _controlCache.Remove(control);
        }

        public void RegisterTextureCache(ITextureCacheItem texture)
        {
            if (texture != null && !_textureCache.Contains(texture))
                _textureCache.Add(texture);
        }

        public void UnregisterTextureCache(ITextureCacheItem texture)
        {
            if (texture != null)
                _textureCache.Remove(texture);
        }

        public void RegisterSoundCache(ISoundCacheItem sound)
        {
            if (sound != null && !_soundCache.Contains(sound))
                _soundCache.Add(sound);
        }

        public void UnregisterSoundCache(ISoundCacheItem sound)
        {
            if (sound != null)
                _soundCache.Remove(sound);
        }

        public IReadOnlyList<ISoundCacheItem> GetRegisteredSoundCaches()
        {
            if (_soundCache.Count == 0)
                return Array.Empty<ISoundCacheItem>();

            return _soundCache.ToArray();
        }

        public void MemoryClear()
        {
            DateTime now = CEnvir.Now;

            for (int i = _controlCache.Count - 1; i >= 0; i--)
            {
                if (now < _controlCache[i].ExpireTime)
                    continue;

                _controlCache[i].DisposeTexture();
            }

            for (int i = _textureCache.Count - 1; i >= 0; i--)
            {
                if (now < _textureCache[i].ExpireTime)
                    continue;

                _textureCache[i].DisposeTexture();
            }

            for (int i = _soundCache.Count - 1; i >= 0; i--)
            {
                if (now < _soundCache[i].ExpireTime)
                    continue;

                _soundCache[i].DisposeSoundBuffer();
            }
        }

        // ── Built-in textures ───────────────────────────────────────────────

        public RenderTexture GetColourPaletteTexture() => _manager.GetColourPaletteTexture();
        public byte[] GetColourPaletteData() => _manager.GetColourPaletteData();
        public RenderTexture GetLightTexture() => _manager.GetLightTexture();
        public Size GetLightTextureSize() => new Size(GLManager.LightWidth, GLManager.LightHeight);
        public RenderTexture GetPoisonTexture() => _manager.GetPoisonTexture();
        public Size GetPoisonTextureSize() => new Size(GLManager.PoisonSize, GLManager.PoisonSize);

        // ── Texture filter ──────────────────────────────────────────────────

        public TextureFilterMode GetTextureFilter() => _manager.GetTextureFilterMode();

        public void SetTextureFilter(TextureFilterMode mode)
        {
            _manager.SetTextureFilterMode(mode);
        }

        // ── Shutdown ────────────────────────────────────────────────────────

        public void Shutdown()
        {
            for (int i = _controlCache.Count - 1; i >= 0; i--)
                _controlCache[i].DisposeTexture();
            _controlCache.Clear();

            for (int i = _textureCache.Count - 1; i >= 0; i--)
                _textureCache[i].DisposeTexture();
            _textureCache.Clear();

            for (int i = _soundCache.Count - 1; i >= 0; i--)
                _soundCache[i].DisposeSoundBuffer();
            _soundCache.Clear();

            _renderer?.Dispose();
            _renderer = null;

            _manager?.Shutdown();
            _manager = null;

            if (_glContext != IntPtr.Zero)
            {
                SDL3Native.SDL_GL_DestroyContext(_glContext);
                _glContext = IntPtr.Zero;
            }

            if (_window != IntPtr.Zero && _ownsWindow)
            {
                SDL3Native.SDL_DestroyWindow(_window);
                _window = IntPtr.Zero;
            }

            SDL3TTF.ShutdownFonts();

            if (_ownsWindow)
                SDL3Native.SDL_Quit();
        }

        // ── Private helpers ─────────────────────────────────────────────────

        private void UpdateBackBufferSize()
        {
            if (_window == IntPtr.Zero)
                return;

            SDL3Native.SDL_GetWindowSize(_window, out int w, out int h);
            _manager?.ResizeBackBuffer(new Size(w, h));
        }

        private Size GetCurrentViewportSize()
        {
            GLFramebufferHandle current = _manager?.GetCurrentTargetHandle();
            if (current != null)
                return new Size(current.Width, current.Height);

            return _manager?.GetBackBufferSize() ?? Config.GameSize;
        }

        private static List<Size> QueryDisplayModes()
        {
            var result = new List<Size>();
            Size minimum = new Size(1024, 768);

            try
            {
                IntPtr displaysPtr = SDL3Native.SDL_GetDisplays(out int displayCount);
                if (displaysPtr == IntPtr.Zero || displayCount <= 0)
                    return result;

                // Read first display ID
                uint displayId = (uint)Marshal.ReadInt32(displaysPtr);

                IntPtr modesPtr = SDL3Native.SDL_GetFullscreenDisplayModes(displayId, out int modeCount);
                if (modesPtr == IntPtr.Zero || modeCount <= 0)
                    return result;

                int structSize = Marshal.SizeOf<SDL3Native.SDL_DisplayMode>();
                for (int i = 0; i < modeCount; i++)
                {
                    IntPtr entryPtr = Marshal.ReadIntPtr(modesPtr, i * IntPtr.Size);
                    if (entryPtr == IntPtr.Zero)
                        continue;

                    SDL3Native.SDL_DisplayMode mode = Marshal.PtrToStructure<SDL3Native.SDL_DisplayMode>(entryPtr);

                    Size size = new Size(mode.w, mode.h);
                    if (size.Width < minimum.Width || size.Height < minimum.Height)
                        continue;

                    if (!result.Contains(size))
                        result.Add(size);
                }

                result.Sort((a, b) => (a.Width * a.Height).CompareTo(b.Width * b.Height));
            }
            catch
            {
                // If display enumeration fails, return an empty list.
            }

            return result;
        }
    }
}
