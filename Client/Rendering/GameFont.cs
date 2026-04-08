using System;
using System.Drawing;

namespace Client.Rendering
{
    public sealed class GameFont : IDisposable
    {
        public string Family { get; }
        public float Size { get; }
        public bool Bold { get; }
        public bool Italic { get; }

        public GameFont(string family, float size, bool bold = false, bool italic = false)
        {
            Family = family;
            Size = size;
            Bold = bold;
            Italic = italic;
        }

#if WINDOWS
        private System.Drawing.Font _drawingFont;
        public System.Drawing.Font ToDrawingFont()
        {
            if (_drawingFont == null)
            {
                var style = System.Drawing.FontStyle.Regular;
                if (Bold) style |= System.Drawing.FontStyle.Bold;
                if (Italic) style |= System.Drawing.FontStyle.Italic;
                _drawingFont = new System.Drawing.Font(Family, Size, style);
            }
            return _drawingFont;
        }
#endif

        public void Dispose()
        {
#if WINDOWS
            _drawingFont?.Dispose();
            _drawingFont = null;
#endif
        }
    }
}
