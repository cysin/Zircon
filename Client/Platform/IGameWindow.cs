using System.Drawing;
using System.Windows.Forms;

namespace Client.Platform
{
    public interface IGameWindow
    {
        nint NativeHandle { get; }
        int Width { get; }
        int Height { get; }
        string Title { get; set; }
        bool Focused { get; }
        Rectangle ClientBounds { get; }

        /// <summary>Window title text (alias for Title for WinForms compat).</summary>
        string Text { get; set; }

        /// <summary>Client area size.</summary>
        Size ClientSize { get; set; }

        /// <summary>Display rectangle of the window client area.</summary>
        Rectangle DisplayRectangle { get; }

        /// <summary>Currently focused child control (WinForms compat).</summary>
        object ActiveControl { get; set; }

        /// <summary>Current mouse cursor.</summary>
        Cursors Cursor { get; set; }

        /// <summary>Suspend layout logic.</summary>
        void SuspendLayout();

        /// <summary>Resume layout logic.</summary>
        void ResumeLayout();

        /// <summary>Center the window on its current monitor.</summary>
        void Center();

        void Show();
        void Close();
    }
}
