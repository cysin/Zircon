// This file provides type compatibility shims for System.Windows.Forms types
// that are used throughout the game's UI code. On Windows, these come from
// the actual WinForms framework. On other platforms, we define minimal
// compatible versions here.
#if !WINDOWS
using System;
using System.Drawing;

// Provide the System.Windows.Forms types that the game UI code references.
// These are minimal implementations — just enough for compilation and basic functionality.
namespace System.Windows.Forms
{
    public class MouseEventArgs : EventArgs
    {
        public MouseButtons Button { get; }
        public Point Location { get; }
        public int X => Location.X;
        public int Y => Location.Y;
        public int Delta { get; }
        public int Clicks { get; }

        public MouseEventArgs(MouseButtons button, int clicks, int x, int y, int delta)
        {
            Button = button;
            Clicks = clicks;
            Location = new Point(x, y);
            Delta = delta;
        }
    }

    public class KeyEventArgs : EventArgs
    {
        public Keys KeyCode { get; }
        public Keys KeyData { get; }
        public bool Shift => (KeyData & Keys.Shift) != 0;
        public bool Alt => (KeyData & Keys.Alt) != 0;
        public bool Control => (KeyData & Keys.Control) != 0;
        public bool Handled { get; set; }
        public bool SuppressKeyPress { get; set; }

        public KeyEventArgs(Keys keyData)
        {
            KeyData = keyData;
            KeyCode = keyData & Keys.KeyCode;
        }
    }

    public class KeyPressEventArgs : EventArgs
    {
        public char KeyChar { get; set; }
        public bool Handled { get; set; }

        public KeyPressEventArgs(char keyChar)
        {
            KeyChar = keyChar;
        }
    }

    public class PreviewKeyDownEventArgs : EventArgs
    {
        public Keys KeyCode { get; }
        public Keys KeyData { get; }
        public bool IsInputKey { get; set; }
        public bool Shift => (KeyData & Keys.Shift) != 0;
        public bool Alt => (KeyData & Keys.Alt) != 0;
        public bool Control => (KeyData & Keys.Control) != 0;

        public PreviewKeyDownEventArgs(Keys keyCode)
        {
            KeyCode = keyCode;
            KeyData = keyCode;
        }

        public PreviewKeyDownEventArgs(Keys keyCode, Keys keyData)
        {
            KeyCode = keyCode;
            KeyData = keyData;
        }
    }

    [Flags]
    public enum MouseButtons
    {
        None = 0,
        Left = 0x100000,
        Right = 0x200000,
        Middle = 0x400000,
        XButton1 = 0x800000,
        XButton2 = 0x1000000,
    }

    [Flags]
    public enum Keys
    {
        None = 0,
        KeyCode = 0xFFFF,
        Modifiers = unchecked((int)0xFFFF0000),
        Shift = 0x10000,
        Control = 0x20000,
        Alt = 0x40000,

        Back = 8, Tab = 9, Enter = 13, Return = 13,
        ShiftKey = 16, ControlKey = 17, Menu = 18,
        Pause = 19, CapsLock = 20,
        Escape = 27, Space = 32,
        PageUp = 33, PageDown = 34, End = 35, Home = 36,
        Left = 37, Up = 38, Right = 39, Down = 40,
        PrintScreen = 44, Insert = 45, Delete = 46,

        D0 = 48, D1 = 49, D2 = 50, D3 = 51, D4 = 52,
        D5 = 53, D6 = 54, D7 = 55, D8 = 56, D9 = 57,

        A = 65, B = 66, C = 67, D = 68, E = 69, F = 70,
        G = 71, H = 72, I = 73, J = 74, K = 75, L = 76,
        M = 77, N = 78, O = 79, P = 80, Q = 81, R = 82,
        S = 83, T = 84, U = 85, V = 86, W = 87, X = 88,
        Y = 89, Z = 90,

        NumPad0 = 96, NumPad1 = 97, NumPad2 = 98, NumPad3 = 99,
        NumPad4 = 100, NumPad5 = 101, NumPad6 = 102, NumPad7 = 103,
        NumPad8 = 104, NumPad9 = 105,
        Multiply = 106, Add = 107, Subtract = 109, Decimal = 110, Divide = 111,

        F1 = 112, F2 = 113, F3 = 114, F4 = 115, F5 = 116, F6 = 117,
        F7 = 118, F8 = 119, F9 = 120, F10 = 121, F11 = 122, F12 = 123,

        NumLock = 144, Scroll = 145,

        OemSemicolon = 186, Oem1 = 186,
        Oemplus = 187, OemComma = 188, OemMinus = 189, OemPeriod = 190,
        OemQuestion = 191, Oem2 = 191,
        Oemtilde = 192, Oem3 = 192,
        OemOpenBrackets = 219, Oem4 = 219,
        OemPipe = 220, Oem5 = 220,
        OemCloseBrackets = 221, Oem6 = 221,
        OemQuotes = 222, Oem7 = 222,
        Oem8 = 223,
        OemBackslash = 226,
        LWin = 91, RWin = 92,
        Apps = 93, Sleep = 95,
        BrowserBack = 166, BrowserForward = 167, BrowserRefresh = 168,
        BrowserStop = 169, BrowserSearch = 170, BrowserFavorites = 171, BrowserHome = 172,
        VolumeMute = 173, VolumeDown = 174, VolumeUp = 175,
        MediaNextTrack = 176, MediaPreviousTrack = 177, MediaStop = 178, MediaPlayPause = 179,
        LaunchMail = 180, SelectMedia = 181, LaunchApplication1 = 182, LaunchApplication2 = 183,
        Snapshot = 44, Select = 41,
        Oemcomma = 188,
        LButton = 1, RButton = 2, Cancel = 3, MButton = 4,
        XButton1 = 5, XButton2 = 6,
        LineFeed = 10, Clear = 12,
        IMEConvert = 28, IMENonconvert = 29, IMEAccept = 30, IMEModeChange = 31,
        Prior = 33, Next = 34,
        LShiftKey = 160, RShiftKey = 161,
        LControlKey = 162, RControlKey = 163,
        LMenu = 164, RMenu = 165,
        ProcessKey = 229, Packet = 231,
        Attn = 246, Crsel = 247, Exsel = 248, EraseEof = 249,
        Play = 250, Zoom = 251, NoName = 252, Pa1 = 253, OemClear = 254,
        Print = 42, Execute = 43, Help = 47,
        Separator = 108,
        Capital = 20,
        KanaMode = 21, JunjaMode = 23, FinalMode = 24, HanjaMode = 25,
    }

    [Flags]
    public enum TextFormatFlags
    {
        Default = 0,
        Left = 0,
        Top = 0,
        HorizontalCenter = 1,
        Right = 2,
        VerticalCenter = 4,
        Bottom = 8,
        WordBreak = 0x10,
        SingleLine = 0x20,
        ExpandTabs = 0x40,
        NoClipping = 0x100,
        ExternalLeading = 0x200,
        NoPrefix = 0x800,
        Internal = 0x1000,
        TextBoxControl = 0x2000,
        EndEllipsis = 0x8000,
        NoPadding = 0x10000000,
        LeftAndRightPadding = 0x20000000,
        WordEllipsis = 0x40000,
        PathEllipsis = 0x4000,
    }

    public static class TextRenderer
    {
        public static Size MeasureText(string text, object font)
        {
            if (string.IsNullOrEmpty(text)) return Size.Empty;

            var f = font as Font;
            if (f != null)
            {
                nint ttf = Client.Platform.SDL3.SDL3TTF.GetFont(f.Size, (f.Style & FontStyle.Bold) != 0, (f.Style & FontStyle.Italic) != 0);
                if (ttf != nint.Zero)
                {
                    if (Client.Platform.SDL3.SDL3TTF.TTF_GetStringSize(ttf, text, (nuint)0, out int w, out int h))
                        return new Size(w, h);
                }
            }

            return new Size(text.Length * 7, 16);
        }

        public static Size MeasureText(string text, object font, Size proposedSize, TextFormatFlags flags)
        {
            if (string.IsNullOrEmpty(text)) return Size.Empty;

            var f = font as Font;
            if (f != null)
            {
                nint ttf = Client.Platform.SDL3.SDL3TTF.GetFont(f.Size, (f.Style & FontStyle.Bold) != 0, (f.Style & FontStyle.Italic) != 0);
                if (ttf != nint.Zero)
                {
                    if ((flags & TextFormatFlags.WordBreak) != 0 && proposedSize.Width > 0)
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
            }

            // Fallback
            int charWidth = 7;
            int lineHeight = 16;
            if ((flags & TextFormatFlags.WordBreak) != 0 && proposedSize.Width > 0)
            {
                int charsPerLine = Math.Max(1, proposedSize.Width / charWidth);
                int lines = (int)Math.Ceiling((double)text.Length / charsPerLine);
                return new Size(Math.Min(text.Length * charWidth, proposedSize.Width), lines * lineHeight);
            }
            return new Size(text.Length * charWidth, lineHeight);
        }

        public static unsafe void DrawText(object graphicsObj, string text, object fontObj, Rectangle bounds, Color color, TextFormatFlags flags)
        {
            if (string.IsNullOrEmpty(text)) return;

            var graphics = graphicsObj as Graphics;
            var font = fontObj as Font;
            if (graphics?.SourceBitmap == null || graphics.SourceBitmap.PixelBuffer == IntPtr.Zero) return;
            if (font == null) return;

            nint ttfFont = Client.Platform.SDL3.SDL3TTF.GetFont(font.Size, (font.Style & FontStyle.Bold) != 0, (font.Style & FontStyle.Italic) != 0);
            if (ttfFont == nint.Zero) return;

            var fg = new Client.Platform.SDL3.SDL3TTF.SDL_Color(color.R, color.G, color.B, color.A);

            // Render text to an SDL surface
            nint surface;
            bool wordBreak = (flags & TextFormatFlags.WordBreak) != 0 && bounds.Width > 0;
            if (wordBreak)
                surface = Client.Platform.SDL3.SDL3TTF.TTF_RenderText_Blended_Wrapped(ttfFont, text, (nuint)0, fg, bounds.Width);
            else
                surface = Client.Platform.SDL3.SDL3TTF.TTF_RenderText_Blended(ttfFont, text, (nuint)0, fg);

            if (surface == nint.Zero) return;

            try
            {
                Client.Platform.SDL3.SDL3TTF.ReadSurface(surface, out int srcW, out int srcH, out int srcPitch, out nint srcPixels);
                if (srcPixels == nint.Zero || srcW <= 0 || srcH <= 0) return;

                Bitmap bmp = graphics.SourceBitmap;
                int dstW = bmp.Width;
                int dstH = bmp.Height;
                int dstPitch = bmp.Pitch;
                byte* dst = (byte*)bmp.PixelBuffer;
                byte* src = (byte*)srcPixels;

                // Compute destination offset based on bounds and alignment flags
                int offsetX = bounds.X;
                int offsetY = bounds.Y;

                if ((flags & TextFormatFlags.HorizontalCenter) != 0)
                    offsetX += Math.Max(0, (bounds.Width - srcW) / 2);
                else if ((flags & TextFormatFlags.Right) != 0)
                    offsetX += Math.Max(0, bounds.Width - srcW);

                if ((flags & TextFormatFlags.VerticalCenter) != 0)
                    offsetY += Math.Max(0, (bounds.Height - srcH) / 2);
                else if ((flags & TextFormatFlags.Bottom) != 0)
                    offsetY += Math.Max(0, bounds.Height - srcH);

                // Copy rendered text into destination buffer with alpha blending.
                // SDL3 TTF_RenderText_Blended returns ARGB8888 which on little-endian
                // is B, G, R, A in memory - matching the BGRA layout the game expects.
                int copyH = Math.Min(srcH, dstH - offsetY);
                int copyW = Math.Min(srcW, dstW - offsetX);

                // Clamp to avoid writing outside destination
                int srcStartX = 0, srcStartY = 0;
                if (offsetX < 0) { srcStartX = -offsetX; offsetX = 0; }
                if (offsetY < 0) { srcStartY = -offsetY; offsetY = 0; }
                copyW = Math.Min(copyW - srcStartX, dstW - offsetX);
                copyH = Math.Min(copyH - srcStartY, dstH - offsetY);

                if (copyW <= 0 || copyH <= 0) return;

                for (int y = 0; y < copyH; y++)
                {
                    byte* srcRow = src + (srcStartY + y) * srcPitch + srcStartX * 4;
                    byte* dstRow = dst + (offsetY + y) * dstPitch + offsetX * 4;

                    for (int x = 0; x < copyW; x++)
                    {
                        // Source pixel: BGRA on little-endian (SDL ARGB8888)
                        byte sB = srcRow[0];
                        byte sG = srcRow[1];
                        byte sR = srcRow[2];
                        byte sA = srcRow[3];

                        if (sA == 255)
                        {
                            // Fully opaque - direct copy
                            dstRow[0] = sB;
                            dstRow[1] = sG;
                            dstRow[2] = sR;
                            dstRow[3] = sA;
                        }
                        else if (sA > 0)
                        {
                            // Alpha blend: srcOver compositing
                            int dA = dstRow[3];
                            int outA = sA + (dA * (255 - sA) + 127) / 255;
                            if (outA > 0)
                            {
                                dstRow[0] = (byte)((sB * sA + dstRow[0] * dA * (255 - sA) / 255 + outA / 2) / outA);
                                dstRow[1] = (byte)((sG * sA + dstRow[1] * dA * (255 - sA) / 255 + outA / 2) / outA);
                                dstRow[2] = (byte)((sR * sA + dstRow[2] * dA * (255 - sA) / 255 + outA / 2) / outA);
                                dstRow[3] = (byte)outA;
                            }
                        }
                        // sA == 0: transparent, skip

                        srcRow += 4;
                        dstRow += 4;
                    }
                }
            }
            finally
            {
                Client.Platform.SDL3.SDL3TTF.SDL_DestroySurface(surface);
            }
        }
    }

    public enum Cursors { Default, SizeNWSE, SizeNESW, SizeWE, SizeNS, SizeAll, Hand, IBeam }

    public class Cursor
    {
        public static object Current { get; set; }
        public static Rectangle Clip { get; set; }
    }

    public static class Clipboard
    {
        private static string _text = "";
        public static void SetText(string text) { _text = text ?? ""; }
        public static string GetText() => _text;
        public static bool ContainsText() => !string.IsNullOrEmpty(_text);
    }

    public enum DragDropEffects { None = 0, Copy = 1, Move = 2, Link = 4, All = 7 }

    public class DragEventArgs : EventArgs
    {
        public object Data { get; set; }
        public DragDropEffects Effect { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class GiveFeedbackEventArgs : EventArgs
    {
        public DragDropEffects Effect { get; set; }
        public bool UseDefaultCursors { get; set; }
    }

    public static class Application
    {
        public static void Exit() { }
        public static string StartupPath => AppDomain.CurrentDomain.BaseDirectory;
        public static string ExecutablePath => Environment.ProcessPath ?? System.Reflection.Assembly.GetEntryAssembly()?.Location ?? "";
    }

    public static class SystemInformation
    {
        public static int DoubleClickTime => 500;
        public static Size DoubleClickSize => new Size(4, 4);
        public static int MouseWheelScrollDelta => 120;
        public static int MouseWheelScrollLines => 3;
    }

    public enum BorderStyle { None, FixedSingle, Fixed3D }
    public enum RightToLeft { No, Yes }
    public enum HorizontalAlignment { Left, Right, Center }
    public enum ScrollBars { None, Horizontal, Vertical, Both }
    public enum FormBorderStyle { None, FixedSingle, Fixed3D, FixedDialog, Sizable, FixedToolWindow, SizableToolWindow }

    public class QueryContinueDragEventArgs : EventArgs
    {
        public bool EscapePressed { get; set; }
        public DragDropEffects Action { get; set; }
    }

    public class TextBox : IDisposable
    {
        private bool _disposed;

        public string Text { get; set; } = "";
        public int SelectionStart { get; set; }
        public int SelectionLength { get; set; }
        public string SelectedText { get; set; } = "";
        public int MaxLength { get; set; } = int.MaxValue;
        public int TextLength => Text?.Length ?? 0;
        public bool UseSystemPasswordChar { get; set; }
        public bool Visible { get; set; }
        public Point Location { get; set; }
        public Size Size { get; set; }
        public Size ClientSize { get; set; }
        public object Font { get; set; }
        public bool ReadOnly { get; set; }
        public object Parent { get; set; }
        public Color BackColor { get; set; }
        public Color ForeColor { get; set; }
        public object ActiveControl { get; set; }
        public nint Handle => nint.Zero;
        public BorderStyle BorderStyle { get; set; }
        public HorizontalAlignment TextAlign { get; set; }
        public bool Multiline { get; set; }
        public ScrollBars ScrollBars { get; set; }
        public bool WordWrap { get; set; }
        public bool AcceptsReturn { get; set; }
        public bool AcceptsTab { get; set; }
        public object Cursor { get; set; }
        public bool IsDisposed => _disposed;

        public void DrawToBitmap(Bitmap bitmap, Rectangle targetBounds) { }
        public void SuspendLayout() { }
        public void ResumeLayout() { }

        public void SelectAll() { SelectionStart = 0; SelectionLength = Text?.Length ?? 0; }
        public void Focus() { }
        public void Select(int start, int length) { SelectionStart = start; SelectionLength = length; }
        public void Paste(string text) { }
        public void Cut() { }
        public void Copy() { }
        public void Undo() { }
        public int GetLineFromCharIndex(int index) => 0;
        public int GetFirstCharIndexFromLine(int line) => 0;
        public void Dispose() { _disposed = true; Dispose(true); }

        protected virtual void OnMouseClick(MouseEventArgs e) { }
        protected virtual void OnMouseDown(MouseEventArgs e) { }
        protected virtual void OnMouseUp(MouseEventArgs e) { }
        protected virtual void OnMouseMove(MouseEventArgs e) { }
        protected virtual void OnPreviewKeyDown(PreviewKeyDownEventArgs e) { }
        protected virtual void OnKeyDown(KeyEventArgs e) { }
        protected virtual void OnKeyUp(KeyEventArgs e) { }
        protected virtual void OnKeyPress(KeyPressEventArgs e) { }
        protected virtual void OnTextChanged(EventArgs e) { TextChanged?.Invoke(this, e); }
        protected virtual void OnSizeChanged(EventArgs e) { }
        protected virtual void OnGotFocus(EventArgs e) { GotFocus?.Invoke(this, e); }
        protected virtual void OnLostFocus(EventArgs e) { LostFocus?.Invoke(this, e); }
        protected virtual void Dispose(bool disposing) { }

        public event EventHandler TextChanged;
        public event EventHandler GotFocus;
        public event EventHandler LostFocus;
        public event KeyEventHandler KeyDown;
        public event KeyEventHandler KeyUp;
        public event KeyPressEventHandler KeyPress;
        public event MouseEventHandler MouseDown;
        public event MouseEventHandler MouseUp;
        public event MouseEventHandler MouseMove;
        public event MouseEventHandler MouseWheel;
    }

    public delegate void KeyEventHandler(object sender, KeyEventArgs e);
    public delegate void KeyPressEventHandler(object sender, KeyPressEventArgs e);
    public delegate void MouseEventHandler(object sender, MouseEventArgs e);
}

// Provide System.Drawing types that require System.Drawing.Common on Windows
namespace System.Drawing
{
    // Font is in System.Drawing.Common, not in Primitives
    public sealed class Font : IDisposable
    {
        public string FontFamily { get; }
        public float Size { get; }
        public FontStyle Style { get; }
        public string Name => FontFamily;
        public float SizeInPoints => Size;

        public Font(string familyName, float emSize)
        {
            FontFamily = familyName;
            Size = emSize;
            Style = FontStyle.Regular;
        }

        public Font(string familyName, float emSize, FontStyle style)
        {
            FontFamily = familyName;
            Size = emSize;
            Style = style;
        }

        public void Dispose() { }
    }

    [Flags]
    public enum FontStyle
    {
        Regular = 0,
        Bold = 1,
        Italic = 2,
        Underline = 4,
        Strikeout = 8,
    }

    public sealed class Bitmap : Image
    {
        public int Width { get; }
        public int Height { get; }

        /// <summary>Raw BGRA pixel buffer pointer (non-zero when wrapping a texture lock).</summary>
        public IntPtr PixelBuffer { get; }
        /// <summary>Row stride in bytes.</summary>
        public int Pitch { get; }

        public Bitmap(int width, int height) { Width = width; Height = height; }
        public Bitmap(int width, int height, Imaging.PixelFormat format) { Width = width; Height = height; }
        public Bitmap(int width, int height, int stride, Imaging.PixelFormat format, IntPtr scan0)
        {
            Width = width;
            Height = height;
            PixelBuffer = scan0;
            Pitch = stride;
        }
        public Bitmap(string filename)
        {
            // Stub: on non-Windows, real image loading would go through a cross-platform decoder.
            Width = 0;
            Height = 0;
        }

        public Imaging.BitmapData LockBits(Rectangle rect, Imaging.ImageLockMode flags, Imaging.PixelFormat format)
        {
            return new Imaging.BitmapData { Scan0 = IntPtr.Zero, Stride = Width * 4 };
        }
        public void UnlockBits(Imaging.BitmapData data) { }
        public Color GetPixel(int x, int y) => Color.Empty;
    }

    public sealed class Graphics : IDisposable
    {
        public float DpiX => 96f;
        public float DpiY => 96f;
        public int TextContrast { get; set; }

        /// <summary>The Bitmap this Graphics was created from (if any).</summary>
        public Bitmap SourceBitmap { get; private set; }

        public static Graphics FromImage(Image image)
        {
            var g = new Graphics();
            g.SourceBitmap = image as Bitmap;
            return g;
        }
        public static Graphics FromHwnd(IntPtr hwnd) => new Graphics();

        public unsafe void Clear(Color color)
        {
            if (SourceBitmap == null || SourceBitmap.PixelBuffer == IntPtr.Zero)
                return;

            byte* dest = (byte*)SourceBitmap.PixelBuffer;
            int total = SourceBitmap.Pitch * SourceBitmap.Height;
            // BGRA byte order
            byte bB = color.B, bG = color.G, bR = color.R, bA = color.A;

            if (bB == 0 && bG == 0 && bR == 0 && bA == 0)
            {
                // Fast path: zero-fill for transparent black
                System.Runtime.CompilerServices.Unsafe.InitBlock(dest, 0, (uint)total);
            }
            else
            {
                for (int i = 0; i < total; i += 4)
                {
                    dest[i] = bB;
                    dest[i + 1] = bG;
                    dest[i + 2] = bR;
                    dest[i + 3] = bA;
                }
            }
        }

        public void Save() { }
        public void Dispose() { }
        public Region[] MeasureCharacterRanges(string text, Font font, RectangleF rect, StringFormat format)
        {
            return new Region[] { new Region() };
        }

        // Stub properties for GDI+ quality settings
        public Drawing2D.SmoothingMode SmoothingMode { get; set; }
        public Text.TextRenderingHint TextRenderingHint { get; set; }
        public Drawing2D.CompositingQuality CompositingQuality { get; set; }
        public Drawing2D.InterpolationMode InterpolationMode { get; set; }
        public Drawing2D.PixelOffsetMode PixelOffsetMode { get; set; }
    }

    public class Image : IDisposable
    {
        public void Dispose() { }
    }

    public struct CharacterRange
    {
        public int First { get; set; }
        public int Length { get; set; }
        public CharacterRange(int first, int length) { First = first; Length = length; }
    }

    public sealed class StringFormat : IDisposable
    {
        public static StringFormat GenericDefault => new StringFormat();
        public void SetMeasurableCharacterRanges(CharacterRange[] ranges) { }
        public void Dispose() { }
    }

    public sealed class Region : IDisposable
    {
        public RectangleF GetBounds(Graphics g) => RectangleF.Empty;
        public void Dispose() { }
    }

    public sealed class Icon : IDisposable
    {
        public void Dispose() { }
    }

    namespace Imaging
    {
        public enum PixelFormat
        {
            Format32bppArgb = 2498570,
            Format32bppPArgb = 925707,
        }

        public enum ImageLockMode
        {
            ReadOnly = 1,
            WriteOnly = 2,
            ReadWrite = 3,
        }

        public class BitmapData
        {
            public IntPtr Scan0 { get; set; }
            public int Stride { get; set; }
        }
    }

    namespace Drawing2D
    {
        public enum SmoothingMode { Default, HighSpeed, HighQuality, None, AntiAlias }
        public enum CompositingQuality { Default, HighSpeed, HighQuality, GammaCorrected, AssumeLinear }
        public enum InterpolationMode { Default, Low, High, Bilinear, Bicubic, NearestNeighbor, HighQualityBilinear, HighQualityBicubic }
        public enum PixelOffsetMode { Default, HighSpeed, HighQuality, None, Half }
    }

    namespace Text
    {
        public enum TextRenderingHint { SystemDefault, SingleBitPerPixelGridFit, SingleBitPerPixel, AntiAliasGridFit, AntiAlias, ClearTypeGridFit }
    }
}
#endif
