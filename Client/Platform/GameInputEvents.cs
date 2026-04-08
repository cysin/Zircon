using System;
using System.Drawing;

namespace Client.Platform
{
    [Flags]
    public enum GameKeys
    {
        None = 0x00,

        // Mouse buttons (matching WinForms Keys values)
        LButton = 0x01,
        RButton = 0x02,
        MButton = 0x04,

        // Control keys
        Back = 0x08,
        Tab = 0x09,
        Enter = 0x0D,
        Escape = 0x1B,
        Space = 0x20,
        PageUp = 0x21,
        PageDown = 0x22,
        End = 0x23,
        Home = 0x24,
        Left = 0x25,
        Up = 0x26,
        Right = 0x27,
        Down = 0x28,
        Delete = 0x2E,

        // Digits 0-9
        D0 = 0x30,
        D1 = 0x31,
        D2 = 0x32,
        D3 = 0x33,
        D4 = 0x34,
        D5 = 0x35,
        D6 = 0x36,
        D7 = 0x37,
        D8 = 0x38,
        D9 = 0x39,

        // Letters A-Z
        A = 0x41,
        B = 0x42,
        C = 0x43,
        D = 0x44,
        E = 0x45,
        F = 0x46,
        G = 0x47,
        H = 0x48,
        I = 0x49,
        J = 0x4A,
        K = 0x4B,
        L = 0x4C,
        M = 0x4D,
        N = 0x4E,
        O = 0x4F,
        P = 0x50,
        Q = 0x51,
        R = 0x52,
        S = 0x53,
        T = 0x54,
        U = 0x55,
        V = 0x56,
        W = 0x57,
        X = 0x58,
        Y = 0x59,
        Z = 0x5A,

        // Function keys F1-F12
        F1 = 0x70,
        F2 = 0x71,
        F3 = 0x72,
        F4 = 0x73,
        F5 = 0x74,
        F6 = 0x75,
        F7 = 0x76,
        F8 = 0x77,
        F9 = 0x78,
        F10 = 0x79,
        F11 = 0x7A,
        F12 = 0x7B,

        // OEM keys
        OemMinus = 0xBD,
        OemPlus = 0xBB,
        OemComma = 0xBC,
        OemPeriod = 0xBE,
        OemTilde = 0xC0,
        Oemtilde = 0xC0,
        Oem1 = 0xBA,
        Oem2 = 0xBF,
        Oem3 = 0xC0,
        Oem4 = 0xDB,
        Oem5 = 0xDC,
        Oem6 = 0xDD,
        Oem7 = 0xDE,
        Oem8 = 0xDF,

        // Modifier flags
        Shift = 0x10000,
        Control = 0x20000,
        Alt = 0x40000,
    }

    public enum GameMouseButtons
    {
        None = 0,
        Left = 1,
        Right = 2,
        Middle = 4,
    }

    public class GameMouseEventArgs : EventArgs
    {
        public GameMouseButtons Button { get; }
        public Point Location { get; }
        public int Delta { get; }
        public int Clicks { get; }

        public GameMouseEventArgs(GameMouseButtons button, Point location, int delta = 0, int clicks = 0)
        {
            Button = button;
            Location = location;
            Delta = delta;
            Clicks = clicks;
        }
    }

    public class GameKeyEventArgs : EventArgs
    {
        public GameKeys KeyCode { get; }
        public bool Shift { get; }
        public bool Alt { get; }
        public bool Control { get; }
        public bool Handled { get; set; }

        public GameKeyEventArgs(GameKeys keyCode, bool shift = false, bool alt = false, bool control = false)
        {
            KeyCode = keyCode;
            Shift = shift;
            Alt = alt;
            Control = control;
        }
    }

    public class GameKeyPressEventArgs : EventArgs
    {
        public char KeyChar { get; }
        public bool Handled { get; set; }

        public GameKeyPressEventArgs(char keyChar)
        {
            KeyChar = keyChar;
        }
    }
}
