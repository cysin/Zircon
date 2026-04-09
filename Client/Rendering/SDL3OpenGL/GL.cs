using System;
using System.Runtime.InteropServices;

namespace Client.Rendering.SDL3OpenGL
{
    /// <summary>
    /// Loads OpenGL function pointers via SDL3's SDL_GL_GetProcAddress and exposes them
    /// as static delegates. Call <see cref="Initialize"/> once after creating the GL context.
    /// </summary>
    internal static class GL
    {
        // ── OpenGL constants ────────────────────────────────────────────────

        public const uint GL_FALSE = 0;
        public const uint GL_TRUE = 1;
        public const uint GL_ZERO = 0;
        public const uint GL_ONE = 1;

        public const uint GL_NO_ERROR = 0;
        public const uint GL_TRIANGLES = 0x0004;
        public const uint GL_TRIANGLE_STRIP = 0x0005;
        public const uint GL_UNSIGNED_SHORT = 0x1403;
        public const uint GL_UNSIGNED_INT = 0x1405;
        public const uint GL_UNSIGNED_BYTE = 0x1401;

        public const uint GL_DEPTH_BUFFER_BIT = 0x00000100;
        public const uint GL_STENCIL_BUFFER_BIT = 0x00000400;
        public const uint GL_COLOR_BUFFER_BIT = 0x00004000;

        public const uint GL_BLEND = 0x0BE2;
        public const uint GL_SCISSOR_TEST = 0x0C11;
        public const uint GL_TEXTURE_2D = 0x0DE1;

        public const uint GL_SRC_COLOR = 0x0300;
        public const uint GL_ONE_MINUS_SRC_COLOR = 0x0301;
        public const uint GL_SRC_ALPHA = 0x0302;
        public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
        public const uint GL_DST_ALPHA = 0x0304;
        public const uint GL_ONE_MINUS_DST_ALPHA = 0x0305;
        public const uint GL_DST_COLOR = 0x0306;
        public const uint GL_ONE_MINUS_DST_COLOR = 0x0307;
        public const uint GL_CONSTANT_COLOR = 0x8001;

        public const uint GL_FUNC_ADD = 0x8006;

        public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
        public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
        public const uint GL_TEXTURE_WRAP_S = 0x2802;
        public const uint GL_TEXTURE_WRAP_T = 0x2803;
        public const uint GL_NEAREST = 0x2600;
        public const uint GL_LINEAR = 0x2601;
        public const uint GL_CLAMP_TO_EDGE = 0x812F;

        public const uint GL_RGBA = 0x1908;
        public const uint GL_BGRA = 0x80E1;
        public const uint GL_RGBA8 = 0x8058;
        public const uint GL_RED = 0x1903;
        public const uint GL_R8 = 0x8229;

        public const uint GL_FRAMEBUFFER = 0x8D40;
        public const uint GL_READ_FRAMEBUFFER = 0x8CA8;
        public const uint GL_DRAW_FRAMEBUFFER = 0x8CA9;
        public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
        public const uint GL_FRAMEBUFFER_COMPLETE = 0x8CD5;

        public const uint GL_ARRAY_BUFFER = 0x8892;
        public const uint GL_ELEMENT_ARRAY_BUFFER = 0x8893;
        public const uint GL_STATIC_DRAW = 0x88E4;
        public const uint GL_DYNAMIC_DRAW = 0x88E8;
        public const uint GL_STREAM_DRAW = 0x88E0;

        public const uint GL_FLOAT = 0x1406;

        public const uint GL_FRAGMENT_SHADER = 0x8B30;
        public const uint GL_VERTEX_SHADER = 0x8B31;
        public const uint GL_COMPILE_STATUS = 0x8B81;
        public const uint GL_LINK_STATUS = 0x8B82;
        public const uint GL_INFO_LOG_LENGTH = 0x8B84;

        public const uint GL_COMPRESSED_RGBA_S3TC_DXT1_EXT = 0x83F1;
        public const uint GL_COMPRESSED_RGBA_S3TC_DXT5_EXT = 0x83F3;

        public const uint GL_VIEWPORT = 0x0BA2;

        // ── Delegate typedefs ───────────────────────────────────────────────

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glEnable(uint cap);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDisable(uint cap);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBlendFunc(uint sfactor, uint dfactor);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBlendFuncSeparate(uint srcRGB, uint dstRGB, uint srcAlpha, uint dstAlpha);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBlendEquation(uint mode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBlendColor(float red, float green, float blue, float alpha);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glViewport(int x, int y, int width, int height);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glScissor(int x, int y, int width, int height);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glClear(uint mask);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glClearColor(float red, float green, float blue, float alpha);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate uint d_glGetError();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGetIntegerv(uint pname, out int data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGetIntegervArray(uint pname, int[] data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glPixelStorei(uint pname, int param);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glLineWidth(float width);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glReadPixels(int x, int y, int width, int height, uint format, uint type, IntPtr data);

        // Texture
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGenTextures(int n, out uint textures);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDeleteTextures(int n, ref uint textures);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBindTexture(uint target, uint texture);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glTexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glTexParameteri(uint target, uint pname, int param);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glActiveTexture(uint texture);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glCompressedTexImage2D(uint target, int level, uint internalformat, int width, int height, int border, int imageSize, IntPtr data);

        // Framebuffer
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGenFramebuffers(int n, out uint framebuffers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDeleteFramebuffers(int n, ref uint framebuffers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBindFramebuffer(uint target, uint framebuffer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glFramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate uint d_glCheckFramebufferStatus(uint target);

        // VAO / VBO / IBO
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGenVertexArrays(int n, out uint arrays);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDeleteVertexArrays(int n, ref uint arrays);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBindVertexArray(uint array);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGenBuffers(int n, out uint buffers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDeleteBuffers(int n, ref uint buffers);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBindBuffer(uint target, uint buffer);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBufferData(uint target, IntPtr size, IntPtr data, uint usage);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glBufferSubData(uint target, IntPtr offset, IntPtr size, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glEnableVertexAttribArray(uint index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDisableVertexAttribArray(uint index);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glVertexAttribPointer(uint index, int size, uint type, bool normalized, int stride, IntPtr pointer);

        // Drawing
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDrawArrays(uint mode, int first, int count);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDrawElements(uint mode, int count, uint type, IntPtr indices);

        // Shader
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate uint d_glCreateProgram();
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDeleteProgram(uint program);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glUseProgram(uint program);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate uint d_glCreateShader(uint type);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glDeleteShader(uint shader);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glShaderSource(uint shader, int count, string[] source, int[] length);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glCompileShader(uint shader);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glAttachShader(uint program, uint shader);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glLinkProgram(uint program);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGetShaderiv(uint shader, uint pname, out int param);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGetProgramiv(uint program, uint pname, out int param);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGetShaderInfoLog(uint shader, int maxLength, out int length, IntPtr infoLog);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glGetProgramInfoLog(uint program, int maxLength, out int length, IntPtr infoLog);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate int d_glGetUniformLocation(uint program, [MarshalAs(UnmanagedType.LPStr)] string name);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate int d_glGetAttribLocation(uint program, [MarshalAs(UnmanagedType.LPStr)] string name);

        // Uniforms
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glUniform1i(int location, int v0);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glUniform1f(int location, float v0);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glUniform2f(int location, float v0, float v1);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glUniform3f(int location, float v0, float v1, float v2);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public delegate void d_glUniform4f(int location, float v0, float v1, float v2, float v3);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)] public unsafe delegate void d_glUniformMatrix4fv(int location, int count, bool transpose, float* value);

        // ── Function pointers ───────────────────────────────────────────────

        public static d_glEnable glEnable;
        public static d_glDisable glDisable;
        public static d_glBlendFunc glBlendFunc;
        public static d_glBlendFuncSeparate glBlendFuncSeparate;
        public static d_glBlendEquation glBlendEquation;
        public static d_glBlendColor glBlendColor;
        public static d_glViewport glViewport;
        public static d_glScissor glScissor;
        public static d_glClear glClear;
        public static d_glClearColor glClearColor;
        public static d_glGetError glGetError;
        public static d_glGetIntegerv glGetIntegerv;
        public static d_glGetIntegervArray glGetIntegervArray;
        public static d_glPixelStorei glPixelStorei;
        public static d_glLineWidth glLineWidth;
        public static d_glReadPixels glReadPixels;

        public static d_glGenTextures glGenTextures;
        public static d_glDeleteTextures glDeleteTextures;
        public static d_glBindTexture glBindTexture;
        public static d_glTexImage2D glTexImage2D;
        public static d_glTexSubImage2D glTexSubImage2D;
        public static d_glTexParameteri glTexParameteri;
        public static d_glActiveTexture glActiveTexture;
        public static d_glCompressedTexImage2D glCompressedTexImage2D;

        public static d_glGenFramebuffers glGenFramebuffers;
        public static d_glDeleteFramebuffers glDeleteFramebuffers;
        public static d_glBindFramebuffer glBindFramebuffer;
        public static d_glFramebufferTexture2D glFramebufferTexture2D;
        public static d_glCheckFramebufferStatus glCheckFramebufferStatus;

        public static d_glGenVertexArrays glGenVertexArrays;
        public static d_glDeleteVertexArrays glDeleteVertexArrays;
        public static d_glBindVertexArray glBindVertexArray;
        public static d_glGenBuffers glGenBuffers;
        public static d_glDeleteBuffers glDeleteBuffers;
        public static d_glBindBuffer glBindBuffer;
        public static d_glBufferData glBufferData;
        public static d_glBufferSubData glBufferSubData;
        public static d_glEnableVertexAttribArray glEnableVertexAttribArray;
        public static d_glDisableVertexAttribArray glDisableVertexAttribArray;
        public static d_glVertexAttribPointer glVertexAttribPointer;

        public static d_glDrawArrays glDrawArrays;
        public static d_glDrawElements glDrawElements;

        public static d_glCreateProgram glCreateProgram;
        public static d_glDeleteProgram glDeleteProgram;
        public static d_glUseProgram glUseProgram;
        public static d_glCreateShader glCreateShader;
        public static d_glDeleteShader glDeleteShader;
        public static d_glShaderSource glShaderSource;
        public static d_glCompileShader glCompileShader;
        public static d_glAttachShader glAttachShader;
        public static d_glLinkProgram glLinkProgram;
        public static d_glGetShaderiv glGetShaderiv;
        public static d_glGetProgramiv glGetProgramiv;
        public static d_glGetShaderInfoLog glGetShaderInfoLog;
        public static d_glGetProgramInfoLog glGetProgramInfoLog;
        public static d_glGetUniformLocation glGetUniformLocation;
        public static d_glGetAttribLocation glGetAttribLocation;

        public static d_glUniform1i glUniform1i;
        public static d_glUniform1f glUniform1f;
        public static d_glUniform2f glUniform2f;
        public static d_glUniform3f glUniform3f;
        public static d_glUniform4f glUniform4f;
        public static d_glUniformMatrix4fv glUniformMatrix4fv;

        // ── Initialization ──────────────────────────────────────────────────

        private static bool _initialized;

        /// <summary>
        /// Loads all GL function pointers. Must be called after the GL context is current.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            glEnable = Load<d_glEnable>("glEnable");
            glDisable = Load<d_glDisable>("glDisable");
            glBlendFunc = Load<d_glBlendFunc>("glBlendFunc");
            glBlendFuncSeparate = Load<d_glBlendFuncSeparate>("glBlendFuncSeparate");
            glBlendEquation = Load<d_glBlendEquation>("glBlendEquation");
            glBlendColor = Load<d_glBlendColor>("glBlendColor");
            glViewport = Load<d_glViewport>("glViewport");
            glScissor = Load<d_glScissor>("glScissor");
            glClear = Load<d_glClear>("glClear");
            glClearColor = Load<d_glClearColor>("glClearColor");
            glGetError = Load<d_glGetError>("glGetError");
            glGetIntegerv = Load<d_glGetIntegerv>("glGetIntegerv");
            glGetIntegervArray = Load<d_glGetIntegervArray>("glGetIntegerv");
            glPixelStorei = Load<d_glPixelStorei>("glPixelStorei");
            glLineWidth = Load<d_glLineWidth>("glLineWidth");
            glReadPixels = Load<d_glReadPixels>("glReadPixels");

            glGenTextures = Load<d_glGenTextures>("glGenTextures");
            glDeleteTextures = Load<d_glDeleteTextures>("glDeleteTextures");
            glBindTexture = Load<d_glBindTexture>("glBindTexture");
            glTexImage2D = Load<d_glTexImage2D>("glTexImage2D");
            glTexSubImage2D = Load<d_glTexSubImage2D>("glTexSubImage2D");
            glTexParameteri = Load<d_glTexParameteri>("glTexParameteri");
            glActiveTexture = Load<d_glActiveTexture>("glActiveTexture");
            glCompressedTexImage2D = Load<d_glCompressedTexImage2D>("glCompressedTexImage2D");

            glGenFramebuffers = Load<d_glGenFramebuffers>("glGenFramebuffers");
            glDeleteFramebuffers = Load<d_glDeleteFramebuffers>("glDeleteFramebuffers");
            glBindFramebuffer = Load<d_glBindFramebuffer>("glBindFramebuffer");
            glFramebufferTexture2D = Load<d_glFramebufferTexture2D>("glFramebufferTexture2D");
            glCheckFramebufferStatus = Load<d_glCheckFramebufferStatus>("glCheckFramebufferStatus");

            glGenVertexArrays = Load<d_glGenVertexArrays>("glGenVertexArrays");
            glDeleteVertexArrays = Load<d_glDeleteVertexArrays>("glDeleteVertexArrays");
            glBindVertexArray = Load<d_glBindVertexArray>("glBindVertexArray");
            glGenBuffers = Load<d_glGenBuffers>("glGenBuffers");
            glDeleteBuffers = Load<d_glDeleteBuffers>("glDeleteBuffers");
            glBindBuffer = Load<d_glBindBuffer>("glBindBuffer");
            glBufferData = Load<d_glBufferData>("glBufferData");
            glBufferSubData = Load<d_glBufferSubData>("glBufferSubData");
            glEnableVertexAttribArray = Load<d_glEnableVertexAttribArray>("glEnableVertexAttribArray");
            glDisableVertexAttribArray = Load<d_glDisableVertexAttribArray>("glDisableVertexAttribArray");
            glVertexAttribPointer = Load<d_glVertexAttribPointer>("glVertexAttribPointer");

            glDrawArrays = Load<d_glDrawArrays>("glDrawArrays");
            glDrawElements = Load<d_glDrawElements>("glDrawElements");

            glCreateProgram = Load<d_glCreateProgram>("glCreateProgram");
            glDeleteProgram = Load<d_glDeleteProgram>("glDeleteProgram");
            glUseProgram = Load<d_glUseProgram>("glUseProgram");
            glCreateShader = Load<d_glCreateShader>("glCreateShader");
            glDeleteShader = Load<d_glDeleteShader>("glDeleteShader");
            glShaderSource = Load<d_glShaderSource>("glShaderSource");
            glCompileShader = Load<d_glCompileShader>("glCompileShader");
            glAttachShader = Load<d_glAttachShader>("glAttachShader");
            glLinkProgram = Load<d_glLinkProgram>("glLinkProgram");
            glGetShaderiv = Load<d_glGetShaderiv>("glGetShaderiv");
            glGetProgramiv = Load<d_glGetProgramiv>("glGetProgramiv");
            glGetShaderInfoLog = Load<d_glGetShaderInfoLog>("glGetShaderInfoLog");
            glGetProgramInfoLog = Load<d_glGetProgramInfoLog>("glGetProgramInfoLog");
            glGetUniformLocation = Load<d_glGetUniformLocation>("glGetUniformLocation");
            glGetAttribLocation = Load<d_glGetAttribLocation>("glGetAttribLocation");

            glUniform1i = Load<d_glUniform1i>("glUniform1i");
            glUniform1f = Load<d_glUniform1f>("glUniform1f");
            glUniform2f = Load<d_glUniform2f>("glUniform2f");
            glUniform3f = Load<d_glUniform3f>("glUniform3f");
            glUniform4f = Load<d_glUniform4f>("glUniform4f");
            glUniformMatrix4fv = Load<d_glUniformMatrix4fv>("glUniformMatrix4fv");
        }

        private static T Load<T>(string name) where T : Delegate
        {
            IntPtr ptr = SDL3Native.SDL_GL_GetProcAddress(name);

            if (ptr == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to load GL function '{name}'.");

            return Marshal.GetDelegateForFunctionPointer<T>(ptr);
        }
    }

    /// <summary>
    /// Minimal SDL3 P/Invoke surface for windowing, events and GL context management.
    /// </summary>
    internal static class SDL3Native
    {
        private const string Lib = "SDL3";

        // ── Initialization ──────────────────────────────────────────────────

        public const uint SDL_INIT_VIDEO = 0x00000020;
        public const uint SDL_INIT_EVENTS = 0x00004000;

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_Init(uint flags);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_Quit();

        // ── Window ──────────────────────────────────────────────────────────

        public const ulong SDL_WINDOW_OPENGL = 0x0000000000000002;
        public const ulong SDL_WINDOW_FULLSCREEN = 0x0000000000000001;
        public const ulong SDL_WINDOW_RESIZABLE = 0x0000000000000020;
        public const ulong SDL_WINDOW_BORDERLESS = 0x0000000000000010;
        public const ulong SDL_WINDOW_HIGH_PIXEL_DENSITY = 0x0000000000002000;

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_CreateWindow([MarshalAs(UnmanagedType.LPUTF8Str)] string title, int w, int h, ulong flags);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern void SDL_DestroyWindow(IntPtr window);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_SetWindowFullscreen(IntPtr window, bool fullscreen);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_SetWindowSize(IntPtr window, int w, int h);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_GetWindowSize(IntPtr window, out int w, out int h);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_SetWindowPosition(IntPtr window, int x, int y);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint SDL_GetWindowFlags(IntPtr window);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_SetWindowBordered(IntPtr window, bool bordered);

        // ── GL Context ──────────────────────────────────────────────────────

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_GL_SetAttribute(int attr, int value);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GL_CreateContext(IntPtr window);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_GL_MakeCurrent(IntPtr window, IntPtr context);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_GL_DestroyContext(IntPtr context);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_GL_SetSwapInterval(int interval);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_GL_SwapWindow(IntPtr window);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GL_GetProcAddress([MarshalAs(UnmanagedType.LPStr)] string proc);

        // GL attribute constants
        public const int SDL_GL_CONTEXT_MAJOR_VERSION = 17;
        public const int SDL_GL_CONTEXT_MINOR_VERSION = 18;
        public const int SDL_GL_CONTEXT_PROFILE_MASK = 21;
        public const int SDL_GL_CONTEXT_PROFILE_CORE = 0x0001;
        public const int SDL_GL_DOUBLEBUFFER = 5;

        // ── Display / Modes ─────────────────────────────────────────────────

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GetDisplays(out int count);

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GetFullscreenDisplayModes(uint displayID, out int count);

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
            public IntPtr internal_;
        }

        // ── Events ──────────────────────────────────────────────────────────

        public const uint SDL_EVENT_QUIT = 0x100;
        public const uint SDL_EVENT_WINDOW_CLOSE_REQUESTED = 0x210;
        public const uint SDL_EVENT_KEY_DOWN = 0x300;
        public const uint SDL_EVENT_KEY_UP = 0x301;
        public const uint SDL_EVENT_MOUSE_MOTION = 0x400;
        public const uint SDL_EVENT_MOUSE_BUTTON_DOWN = 0x401;
        public const uint SDL_EVENT_MOUSE_BUTTON_UP = 0x402;
        public const uint SDL_EVENT_MOUSE_WHEEL = 0x403;

        [StructLayout(LayoutKind.Explicit, Size = 128)]
        public struct SDL_Event
        {
            [FieldOffset(0)] public uint type;
        }

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool SDL_PollEvent(out SDL_Event e);

        // ── Error ───────────────────────────────────────────────────────────

        [DllImport(Lib, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SDL_GetError();

        public static string GetError()
        {
            IntPtr ptr = SDL_GetError();
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) ?? string.Empty : string.Empty;
        }
    }
}
