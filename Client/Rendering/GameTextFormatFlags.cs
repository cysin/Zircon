using System;

namespace Client.Rendering
{
    [Flags]
    public enum GameTextFormatFlags
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
    }
}
