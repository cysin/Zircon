using System;
using System.Drawing;

namespace Client.Rendering
{
    public interface ITextRenderer
    {
        Size MeasureText(string text, GameFont font);
        Size MeasureText(string text, GameFont font, Size proposedSize, GameTextFormatFlags format);
        void RenderText(string text, GameFont font, Color color, IntPtr buffer, int pitch,
                       int width, int height, GameTextFormatFlags format);
        float GetDpi();
    }
}
