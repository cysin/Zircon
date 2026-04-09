using System;
using System.Runtime.InteropServices;

namespace Client.Platform.SDL3
{
    public static class SDL3Native
    {
        private const string LibName = "SDL3";

        // Init/Quit
        [DllImport(LibName)] public static extern int SDL_Init(uint flags);
        [DllImport(LibName)] public static extern void SDL_Quit();

        // Window
        [DllImport(LibName)] public static extern nint SDL_CreateWindow([MarshalAs(UnmanagedType.LPUTF8Str)] string title, int w, int h, ulong flags);
        [DllImport(LibName)] public static extern void SDL_DestroyWindow(nint window);
        [DllImport(LibName)] public static extern bool SDL_GetWindowSize(nint window, out int w, out int h);
        [DllImport(LibName)] public static extern bool SDL_GetWindowSizeInPixels(nint window, out int w, out int h);
        [DllImport(LibName)] public static extern bool SDL_SetWindowSize(nint window, int w, int h);
        [DllImport(LibName)] public static extern bool SDL_SetWindowFullscreen(nint window, bool fullscreen);
        [DllImport(LibName)] public static extern bool SDL_SetWindowTitle(nint window, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);
        [DllImport(LibName)] public static extern ulong SDL_GetWindowFlags(nint window);
        [DllImport(LibName)] public static extern bool SDL_SetWindowPosition(nint window, int x, int y);
        [DllImport(LibName)] public static extern nint SDL_GetWindowProperties(nint window);
        [DllImport(LibName)] public static extern bool SDL_GetWindowPosition(nint window, out int x, out int y);

        // GL
        [DllImport(LibName)] public static extern int SDL_GL_SetAttribute(int attr, int value);
        [DllImport(LibName)] public static extern nint SDL_GL_CreateContext(nint window);
        [DllImport(LibName)] public static extern int SDL_GL_MakeCurrent(nint window, nint context);
        [DllImport(LibName)] public static extern int SDL_GL_SetSwapInterval(int interval);
        [DllImport(LibName)] public static extern int SDL_GL_SwapWindow(nint window);
        [DllImport(LibName)] public static extern void SDL_GL_DestroyContext(nint context);
        [DllImport(LibName)] public static extern nint SDL_GL_GetProcAddress([MarshalAs(UnmanagedType.LPUTF8Str)] string proc);

        // Events
        [DllImport(LibName)] public static extern bool SDL_PollEvent(out SDL_Event e);

        // Display
        [DllImport(LibName)] public static extern nint SDL_GetDisplays(out int count);
        [DllImport(LibName)] public static extern float SDL_GetDisplayContentScale(uint displayId);
        [DllImport(LibName)] public static extern float SDL_GetWindowPixelDensity(nint window);
        [DllImport(LibName)] public static extern float SDL_GetWindowDisplayScale(nint window);
        [DllImport(LibName)] public static extern nint SDL_GetFullscreenDisplayModes(uint displayId, out int count);

        // Cursor
        [DllImport(LibName)] public static extern nint SDL_CreateSystemCursor(int id);
        [DllImport(LibName)] public static extern int SDL_SetCursor(nint cursor);

        // Clipboard
        [DllImport(LibName)] public static extern int SDL_SetClipboardText([MarshalAs(UnmanagedType.LPUTF8Str)] string text);
        [DllImport(LibName)] public static extern nint SDL_GetClipboardText();

        // Text input
        [DllImport(LibName)] public static extern bool SDL_StartTextInput(nint window);
        [DllImport(LibName)] public static extern bool SDL_StopTextInput(nint window);

        // Audio
        [DllImport(LibName)] public static extern int SDL_InitSubSystem(uint flags);

        // Error
        [DllImport(LibName)] public static extern nint SDL_GetError();

        // Constants
        public const uint SDL_INIT_VIDEO = 0x00000020;
        public const uint SDL_INIT_AUDIO = 0x00000010;
        public const uint SDL_INIT_EVENTS = 0x00004000;

        public const ulong SDL_WINDOW_OPENGL = 0x0000000000000002;
        public const ulong SDL_WINDOW_RESIZABLE = 0x0000000000000020;
        public const ulong SDL_WINDOW_FULLSCREEN = 0x0000000000000001;
        public const ulong SDL_WINDOW_BORDERLESS = 0x0000000000000010;
        public const ulong SDL_WINDOW_HIGH_PIXEL_DENSITY = 0x0000000000002000;
        public const ulong SDL_WINDOW_INPUT_FOCUS = 0x0000000000000200;

        // GL attributes
        public const int SDL_GL_CONTEXT_MAJOR_VERSION = 17;
        public const int SDL_GL_CONTEXT_MINOR_VERSION = 18;
        public const int SDL_GL_CONTEXT_PROFILE_MASK = 21;
        public const int SDL_GL_CONTEXT_PROFILE_CORE = 1;
        public const int SDL_GL_DOUBLEBUFFER = 5;

        // Event types (SDL3)
        public const uint SDL_EVENT_QUIT = 0x100;
        public const uint SDL_EVENT_KEY_DOWN = 0x300;
        public const uint SDL_EVENT_KEY_UP = 0x301;
        public const uint SDL_EVENT_TEXT_INPUT = 0x303;
        public const uint SDL_EVENT_MOUSE_MOTION = 0x400;
        public const uint SDL_EVENT_MOUSE_BUTTON_DOWN = 0x401;
        public const uint SDL_EVENT_MOUSE_BUTTON_UP = 0x402;
        public const uint SDL_EVENT_MOUSE_WHEEL = 0x403;
        public const uint SDL_EVENT_WINDOW_SHOWN = 0x202;
        public const uint SDL_EVENT_WINDOW_EXPOSED = 0x204;
        public const uint SDL_EVENT_WINDOW_RESIZED = 0x206;
        public const uint SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED = 0x207;
        public const uint SDL_EVENT_WINDOW_FOCUS_GAINED = 0x20E;
        public const uint SDL_EVENT_WINDOW_FOCUS_LOST = 0x20F;
        public const uint SDL_EVENT_WINDOW_CLOSE_REQUESTED = 0x210;

        // Mouse buttons
        public const byte SDL_BUTTON_LEFT = 1;
        public const byte SDL_BUTTON_MIDDLE = 2;
        public const byte SDL_BUTTON_RIGHT = 3;

        // System cursors
        public const int SDL_SYSTEM_CURSOR_DEFAULT = 0;
        public const int SDL_SYSTEM_CURSOR_TEXT = 1;
        public const int SDL_SYSTEM_CURSOR_CROSSHAIR = 3;
        public const int SDL_SYSTEM_CURSOR_NWSE_RESIZE = 6;
        public const int SDL_SYSTEM_CURSOR_NESW_RESIZE = 7;
        public const int SDL_SYSTEM_CURSOR_EW_RESIZE = 8;
        public const int SDL_SYSTEM_CURSOR_NS_RESIZE = 9;
        public const int SDL_SYSTEM_CURSOR_MOVE = 11;

        // Window position constants
        public const int SDL_WINDOWPOS_CENTERED = 0x2FFF0000;

        /// <summary>
        /// Returns the SDL error string. Marshals the native pointer to a managed string.
        /// </summary>
        public static string GetError()
        {
            nint ptr = SDL_GetError();
            return ptr == nint.Zero ? string.Empty : Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_DisplayMode
    {
        public uint displayID;
        public uint format;
        public int w;
        public int h;
        public float pixel_density;
        public float refresh_rate;
        public int refresh_rate_numerator;
        public int refresh_rate_denominator;
        public nint internal_;
    }

    // SDL_Event union - simplified for the events we need
    [StructLayout(LayoutKind.Explicit, Size = 128)]
    public struct SDL_Event
    {
        [FieldOffset(0)] public uint type;
        [FieldOffset(0)] public SDL_KeyboardEvent key;
        [FieldOffset(0)] public SDL_MouseMotionEvent motion;
        [FieldOffset(0)] public SDL_MouseButtonEvent button;
        [FieldOffset(0)] public SDL_MouseWheelEvent wheel;
        [FieldOffset(0)] public SDL_TextInputEvent text;
        [FieldOffset(0)] public SDL_WindowEvent window;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_KeyboardEvent
    {
        public uint type;
        public uint reserved;
        public ulong timestamp;
        public uint windowID;
        public uint which;
        public int scancode;
        public uint key;
        public ushort mod;
        public ushort raw;
        public byte down;
        public byte repeat;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_MouseMotionEvent
    {
        public uint type;
        public uint reserved;
        public ulong timestamp;
        public uint windowID;
        public uint which;
        public uint state;
        public float x;
        public float y;
        public float xrel;
        public float yrel;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_MouseButtonEvent
    {
        public uint type;
        public uint reserved;
        public ulong timestamp;
        public uint windowID;
        public uint which;
        public byte button;
        public byte down;
        public byte clicks;
        public byte padding;
        public float x;
        public float y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_MouseWheelEvent
    {
        public uint type;
        public uint reserved;
        public ulong timestamp;
        public uint windowID;
        public uint which;
        public float x;
        public float y;
        public int direction;
        public float mouse_x;
        public float mouse_y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_TextInputEvent
    {
        public uint type;
        public uint reserved;
        public ulong timestamp;
        public uint windowID;
        public nint text;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SDL_WindowEvent
    {
        public uint type;
        public uint reserved;
        public ulong timestamp;
        public uint windowID;
        public int data1;
        public int data2;
    }
}
