using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Client.Platform.SDL3
{
    public class SDL3GameWindow : IGameWindow, IDisposable
    {
        private nint _window;
        private nint _glContext;
        private string _title;
        private bool _disposed;

        public nint NativeHandle => _window;
        public nint GLContext => _glContext;

        public int Width
        {
            get
            {
                if (_window == nint.Zero) return 0;
                SDL3Native.SDL_GetWindowSize(_window, out int w, out _);
                return w;
            }
        }

        public int Height
        {
            get
            {
                if (_window == nint.Zero) return 0;
                SDL3Native.SDL_GetWindowSize(_window, out _, out int h);
                return h;
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                if (_window != nint.Zero)
                    SDL3Native.SDL_SetWindowTitle(_window, value);
            }
        }

        public bool Focused
        {
            get
            {
                if (_window == nint.Zero) return false;
                ulong flags = SDL3Native.SDL_GetWindowFlags(_window);
                return (flags & SDL3Native.SDL_WINDOW_INPUT_FOCUS) != 0;
            }
        }

        public Rectangle ClientBounds
        {
            get
            {
                if (_window == nint.Zero) return Rectangle.Empty;

                SDL3Native.SDL_GetWindowPosition(_window, out int x, out int y);
                SDL3Native.SDL_GetWindowSize(_window, out int w, out int h);
                return new Rectangle(x, y, w, h);
            }
        }

        public string Text
        {
            get => Title;
            set => Title = value;
        }

        public Size ClientSize
        {
            get => new Size(Width, Height);
            set
            {
                if (_window != nint.Zero)
                    SDL3Native.SDL_SetWindowSize(_window, value.Width, value.Height);
            }
        }

        public Rectangle DisplayRectangle => new Rectangle(Point.Empty, ClientSize);

        public object ActiveControl { get; set; }

        public Cursors Cursor { get; set; } = Cursors.Default;

        public void SuspendLayout() { }
        public void ResumeLayout() { }

        public void Center()
        {
            // SDL3 centres the window on its display
            if (_window != nint.Zero)
                SDL3Native.SDL_SetWindowPosition(_window, SDL3Native.SDL_WINDOWPOS_CENTERED, SDL3Native.SDL_WINDOWPOS_CENTERED);
        }

        public SDL3GameWindow(string title, int width, int height)
        {
            _title = title ?? "Zircon";

            int initResult = SDL3Native.SDL_Init(SDL3Native.SDL_INIT_VIDEO);
            if (initResult < 0)
                throw new InvalidOperationException($"SDL_Init failed: {SDL3Native.GetError()}");

            // Set OpenGL attributes before creating the window
            SDL3Native.SDL_GL_SetAttribute(SDL3Native.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL3Native.SDL_GL_SetAttribute(SDL3Native.SDL_GL_CONTEXT_MINOR_VERSION, 3);
            SDL3Native.SDL_GL_SetAttribute(SDL3Native.SDL_GL_CONTEXT_PROFILE_MASK, SDL3Native.SDL_GL_CONTEXT_PROFILE_CORE);
            SDL3Native.SDL_GL_SetAttribute(SDL3Native.SDL_GL_DOUBLEBUFFER, 1);

            ulong windowFlags = SDL3Native.SDL_WINDOW_OPENGL | SDL3Native.SDL_WINDOW_RESIZABLE;

            _window = SDL3Native.SDL_CreateWindow(_title, width, height, windowFlags);
            if (_window == nint.Zero)
                throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL3Native.GetError()}");

            _glContext = SDL3Native.SDL_GL_CreateContext(_window);
            if (_glContext == nint.Zero)
            {
                SDL3Native.SDL_DestroyWindow(_window);
                _window = nint.Zero;
                throw new InvalidOperationException($"SDL_GL_CreateContext failed: {SDL3Native.GetError()}");
            }

            SDL3Native.SDL_GL_MakeCurrent(_window, _glContext);
            SDL3Native.SDL_GL_SetSwapInterval(1); // Enable VSync
        }

        public void Show()
        {
            // Window is shown by default on creation in SDL3.
            // If the window was created hidden, SDL_ShowWindow would be needed here.
            // For now, start text input so keyboard events work.
            if (_window != nint.Zero)
                SDL3Native.SDL_StartTextInput(_window);
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_window != nint.Zero)
                SDL3Native.SDL_StopTextInput(_window);

            if (_glContext != nint.Zero)
            {
                SDL3Native.SDL_GL_DestroyContext(_glContext);
                _glContext = nint.Zero;
            }

            if (_window != nint.Zero)
            {
                SDL3Native.SDL_DestroyWindow(_window);
                _window = nint.Zero;
            }

            GC.SuppressFinalize(this);
        }

        ~SDL3GameWindow()
        {
            Dispose();
        }
    }
}
