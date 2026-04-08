using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Client.Platform.SDL3
{
    /// <summary>
    /// P/Invoke bindings for SDL3_ttf and font management utilities.
    /// </summary>
    public static class SDL3TTF
    {
        private const string LibName = "SDL3_ttf";
        private const string SDLLibName = "SDL3";

        // ── SDL3_ttf core ──────────────────────────────────────────────────

        [DllImport(LibName)]
        public static extern bool TTF_Init();

        [DllImport(LibName)]
        public static extern void TTF_Quit();

        [DllImport(LibName)]
        public static extern bool TTF_WasInit();

        [DllImport(LibName)]
        public static extern nint TTF_OpenFont([MarshalAs(UnmanagedType.LPUTF8Str)] string file, float ptsize);

        [DllImport(LibName)]
        public static extern void TTF_CloseFont(nint font);

        [DllImport(LibName)]
        public static extern void TTF_SetFontStyle(nint font, uint style);

        [DllImport(LibName)]
        public static extern uint TTF_GetFontStyle(nint font);

        [DllImport(LibName)]
        public static extern int TTF_GetFontHeight(nint font);

        [DllImport(LibName)]
        public static extern int TTF_GetFontAscent(nint font);

        [DllImport(LibName)]
        public static extern int TTF_GetFontDescent(nint font);

        [DllImport(LibName)]
        public static extern int TTF_GetFontLineSkip(nint font);

        [DllImport(LibName)]
        public static extern bool TTF_GetStringSize(nint font,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
            nuint length, out int w, out int h);

        [DllImport(LibName)]
        public static extern bool TTF_GetStringSizeWrapped(nint font,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
            nuint length, int wrapWidth, out int w, out int h);

        // SDL_Color is a 4-byte struct { r, g, b, a } passed by value.
        // On x86-64 SysV ABI a 4-byte struct is passed in an integer register,
        // so we marshal it as a uint32 in RGBA byte order (r=low byte on LE).
        [StructLayout(LayoutKind.Sequential, Size = 4)]
        public struct SDL_Color
        {
            public byte r, g, b, a;

            public SDL_Color(byte r, byte g, byte b, byte a)
            {
                this.r = r; this.g = g; this.b = b; this.a = a;
            }
        }

        [DllImport(LibName)]
        public static extern nint TTF_RenderText_Blended(nint font,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
            nuint length, SDL_Color fg);

        [DllImport(LibName)]
        public static extern nint TTF_RenderText_Blended_Wrapped(nint font,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string text,
            nuint length, SDL_Color fg, int wrapWidth);

        // ── SDL3 surface management ────────────────────────────────────────

        [DllImport(SDLLibName)]
        public static extern void SDL_DestroySurface(nint surface);

        // SDL3 SDL_Surface struct layout (from SDL_surface.h):
        //   uint32 flags;        // offset  0, size 4
        //   int32  format;       // offset  4, size 4  (SDL_PixelFormat enum)
        //   int32  w;            // offset  8, size 4
        //   int32  h;            // offset 12, size 4
        //   int32  pitch;        // offset 16, size 4
        //   void*  pixels;       // offset 20 (32-bit) or 24 (64-bit, with padding)

        // On 64-bit, there's 4 bytes of padding between pitch (offset 16) and
        // pixels pointer (offset 24) due to pointer alignment.

        /// <summary>
        /// Reads width, height, pitch, and pixel pointer from an SDL_Surface*.
        /// </summary>
        public static unsafe void ReadSurface(nint surface, out int w, out int h, out int pitch, out nint pixels)
        {
            byte* ptr = (byte*)surface;
            w = *(int*)(ptr + 8);
            h = *(int*)(ptr + 12);
            pitch = *(int*)(ptr + 16);
            // On 64-bit, pixels pointer is at offset 24 (after 4 bytes padding)
            pixels = *(nint*)(ptr + 24);
        }

        // ── Font style constants ───────────────────────────────────────────

        public const uint TTF_STYLE_NORMAL = 0x00;
        public const uint TTF_STYLE_BOLD = 0x01;
        public const uint TTF_STYLE_ITALIC = 0x02;
        public const uint TTF_STYLE_UNDERLINE = 0x04;
        public const uint TTF_STYLE_STRIKETHROUGH = 0x08;

        // ── Initialization tracking ────────────────────────────────────────

        private static bool _initialized;
        private static readonly object _initLock = new();

        public static bool EnsureInitialized()
        {
            if (_initialized) return true;
            lock (_initLock)
            {
                if (_initialized) return true;
                if (!TTF_Init())
                {
                    Console.WriteLine("[SDL3TTF] TTF_Init failed.");
                    return false;
                }
                _initialized = true;
                return true;
            }
        }

        // ── Font cache ─────────────────────────────────────────────────────

        private struct FontCacheKey : IEquatable<FontCacheKey>
        {
            public float Size;
            public uint Style;

            public bool Equals(FontCacheKey other) =>
                MathF.Abs(Size - other.Size) < 0.01f && Style == other.Style;

            public override int GetHashCode() =>
                HashCode.Combine((int)(Size * 100), Style);

            public override bool Equals(object obj) =>
                obj is FontCacheKey k && Equals(k);
        }

        private static readonly ConcurrentDictionary<FontCacheKey, nint> _fontCache = new();
        private static string _resolvedFontPath;

        /// <summary>
        /// Resolves a TTF font file path, caching the result.
        /// Searches for Liberation Sans, Noto Sans, DejaVu Sans, or any sans-serif font.
        /// </summary>
        public static string ResolveFontPath()
        {
            if (_resolvedFontPath != null)
                return _resolvedFontPath;

            // Preferred font paths in order of preference
            string[] candidates = new[]
            {
                "/usr/share/fonts/liberation-sans-fonts/LiberationSans-Regular.ttf",
                "/usr/share/fonts/liberation-sans/LiberationSans-Regular.ttf",
                "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
                "/usr/share/fonts/google-noto-vf/NotoSans[wght].ttf",
                "/usr/share/fonts/noto/NotoSans-Regular.ttf",
                "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
                "/usr/share/fonts/dejavu-sans-fonts/DejaVuSans.ttf",
                "/usr/share/fonts/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path))
                {
                    _resolvedFontPath = path;
                    Console.WriteLine($"[SDL3TTF] Using font: {path}");
                    return _resolvedFontPath;
                }
            }

            // Last resort: try fc-match
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("fc-match", "sans-serif -f %{file}")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd().Trim();
                    proc.WaitForExit(2000);
                    if (!string.IsNullOrEmpty(output) && File.Exists(output))
                    {
                        _resolvedFontPath = output;
                        Console.WriteLine($"[SDL3TTF] Using font (fc-match): {output}");
                        return _resolvedFontPath;
                    }
                }
            }
            catch
            {
                // Ignore fc-match failures
            }

            Console.WriteLine("[SDL3TTF] WARNING: No suitable font file found.");
            _resolvedFontPath = "";
            return _resolvedFontPath;
        }

        /// <summary>
        /// Gets a cached TTF font handle for the given size and style.
        /// </summary>
        public static nint GetFont(float size, bool bold, bool italic)
        {
            if (!EnsureInitialized()) return nint.Zero;

            uint style = TTF_STYLE_NORMAL;
            if (bold) style |= TTF_STYLE_BOLD;
            if (italic) style |= TTF_STYLE_ITALIC;

            var key = new FontCacheKey { Size = size, Style = style };

            if (_fontCache.TryGetValue(key, out nint cached))
                return cached;

            string fontPath = ResolveFontPath();
            if (string.IsNullOrEmpty(fontPath))
                return nint.Zero;

            // Scale font size up to match Windows "MS Sans Serif" visual size.
            // MS Sans Serif is a bitmap font that renders ~1.4x larger than standard TTF at same pt size.
            float scaledSize = size * 1.4f;
            nint font = TTF_OpenFont(fontPath, scaledSize);
            if (font == nint.Zero)
            {
                Console.WriteLine($"[SDL3TTF] TTF_OpenFont failed for size {size}");
                return nint.Zero;
            }

            if (style != TTF_STYLE_NORMAL)
                TTF_SetFontStyle(font, style);

            _fontCache.TryAdd(key, font);
            return font;
        }

        /// <summary>
        /// Cleans up all cached fonts and shuts down SDL3_ttf.
        /// </summary>
        public static void ShutdownFonts()
        {
            foreach (var kvp in _fontCache)
            {
                if (kvp.Value != nint.Zero)
                    TTF_CloseFont(kvp.Value);
            }
            _fontCache.Clear();

            if (_initialized)
            {
                TTF_Quit();
                _initialized = false;
            }
        }
    }
}
