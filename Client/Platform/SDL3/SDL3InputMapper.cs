namespace Client.Platform.SDL3
{
    /// <summary>
    /// Maps SDL3 scancodes and mouse buttons to the platform-independent GameKeys and GameMouseButtons enums.
    /// SDL3 scancode values: https://wiki.libsdl.org/SDL3/SDL_Scancode
    /// </summary>
    public static class SDL3InputMapper
    {
        // SDL3 scancodes for reference
        private const int SDL_SCANCODE_A = 4;
        private const int SDL_SCANCODE_B = 5;
        private const int SDL_SCANCODE_C = 6;
        private const int SDL_SCANCODE_D = 7;
        private const int SDL_SCANCODE_E = 8;
        private const int SDL_SCANCODE_F = 9;
        private const int SDL_SCANCODE_G = 10;
        private const int SDL_SCANCODE_H = 11;
        private const int SDL_SCANCODE_I = 12;
        private const int SDL_SCANCODE_J = 13;
        private const int SDL_SCANCODE_K = 14;
        private const int SDL_SCANCODE_L = 15;
        private const int SDL_SCANCODE_M = 16;
        private const int SDL_SCANCODE_N = 17;
        private const int SDL_SCANCODE_O = 18;
        private const int SDL_SCANCODE_P = 19;
        private const int SDL_SCANCODE_Q = 20;
        private const int SDL_SCANCODE_R = 21;
        private const int SDL_SCANCODE_S = 22;
        private const int SDL_SCANCODE_T = 23;
        private const int SDL_SCANCODE_U = 24;
        private const int SDL_SCANCODE_V = 25;
        private const int SDL_SCANCODE_W = 26;
        private const int SDL_SCANCODE_X = 27;
        private const int SDL_SCANCODE_Y = 28;
        private const int SDL_SCANCODE_Z = 29;

        private const int SDL_SCANCODE_1 = 30;
        private const int SDL_SCANCODE_2 = 31;
        private const int SDL_SCANCODE_3 = 32;
        private const int SDL_SCANCODE_4 = 33;
        private const int SDL_SCANCODE_5 = 34;
        private const int SDL_SCANCODE_6 = 35;
        private const int SDL_SCANCODE_7 = 36;
        private const int SDL_SCANCODE_8 = 37;
        private const int SDL_SCANCODE_9 = 38;
        private const int SDL_SCANCODE_0 = 39;

        private const int SDL_SCANCODE_RETURN = 40;
        private const int SDL_SCANCODE_ESCAPE = 41;
        private const int SDL_SCANCODE_BACKSPACE = 42;
        private const int SDL_SCANCODE_TAB = 43;
        private const int SDL_SCANCODE_SPACE = 44;

        private const int SDL_SCANCODE_MINUS = 45;
        private const int SDL_SCANCODE_EQUALS = 46;
        private const int SDL_SCANCODE_LEFTBRACKET = 47;
        private const int SDL_SCANCODE_RIGHTBRACKET = 48;
        private const int SDL_SCANCODE_BACKSLASH = 49;
        private const int SDL_SCANCODE_SEMICOLON = 51;
        private const int SDL_SCANCODE_APOSTROPHE = 52;
        private const int SDL_SCANCODE_GRAVE = 53;
        private const int SDL_SCANCODE_COMMA = 54;
        private const int SDL_SCANCODE_PERIOD = 55;
        private const int SDL_SCANCODE_SLASH = 56;

        private const int SDL_SCANCODE_F1 = 58;
        private const int SDL_SCANCODE_F2 = 59;
        private const int SDL_SCANCODE_F3 = 60;
        private const int SDL_SCANCODE_F4 = 61;
        private const int SDL_SCANCODE_F5 = 62;
        private const int SDL_SCANCODE_F6 = 63;
        private const int SDL_SCANCODE_F7 = 64;
        private const int SDL_SCANCODE_F8 = 65;
        private const int SDL_SCANCODE_F9 = 66;
        private const int SDL_SCANCODE_F10 = 67;
        private const int SDL_SCANCODE_F11 = 68;
        private const int SDL_SCANCODE_F12 = 69;

        private const int SDL_SCANCODE_INSERT = 73;
        private const int SDL_SCANCODE_HOME = 74;
        private const int SDL_SCANCODE_PAGEUP = 75;
        private const int SDL_SCANCODE_DELETE = 76;
        private const int SDL_SCANCODE_END = 77;
        private const int SDL_SCANCODE_PAGEDOWN = 78;
        private const int SDL_SCANCODE_RIGHT = 79;
        private const int SDL_SCANCODE_LEFT = 80;
        private const int SDL_SCANCODE_DOWN = 81;
        private const int SDL_SCANCODE_UP = 82;

        private const int SDL_SCANCODE_LCTRL = 224;
        private const int SDL_SCANCODE_LSHIFT = 225;
        private const int SDL_SCANCODE_LALT = 226;
        private const int SDL_SCANCODE_RCTRL = 228;
        private const int SDL_SCANCODE_RSHIFT = 229;
        private const int SDL_SCANCODE_RALT = 230;

        /// <summary>
        /// Maps an SDL3 scancode to a GameKeys value.
        /// </summary>
        public static GameKeys MapScancode(int scancode)
        {
            return scancode switch
            {
                // Letters
                SDL_SCANCODE_A => GameKeys.A,
                SDL_SCANCODE_B => GameKeys.B,
                SDL_SCANCODE_C => GameKeys.C,
                SDL_SCANCODE_D => GameKeys.D,
                SDL_SCANCODE_E => GameKeys.E,
                SDL_SCANCODE_F => GameKeys.F,
                SDL_SCANCODE_G => GameKeys.G,
                SDL_SCANCODE_H => GameKeys.H,
                SDL_SCANCODE_I => GameKeys.I,
                SDL_SCANCODE_J => GameKeys.J,
                SDL_SCANCODE_K => GameKeys.K,
                SDL_SCANCODE_L => GameKeys.L,
                SDL_SCANCODE_M => GameKeys.M,
                SDL_SCANCODE_N => GameKeys.N,
                SDL_SCANCODE_O => GameKeys.O,
                SDL_SCANCODE_P => GameKeys.P,
                SDL_SCANCODE_Q => GameKeys.Q,
                SDL_SCANCODE_R => GameKeys.R,
                SDL_SCANCODE_S => GameKeys.S,
                SDL_SCANCODE_T => GameKeys.T,
                SDL_SCANCODE_U => GameKeys.U,
                SDL_SCANCODE_V => GameKeys.V,
                SDL_SCANCODE_W => GameKeys.W,
                SDL_SCANCODE_X => GameKeys.X,
                SDL_SCANCODE_Y => GameKeys.Y,
                SDL_SCANCODE_Z => GameKeys.Z,

                // Digits
                SDL_SCANCODE_0 => GameKeys.D0,
                SDL_SCANCODE_1 => GameKeys.D1,
                SDL_SCANCODE_2 => GameKeys.D2,
                SDL_SCANCODE_3 => GameKeys.D3,
                SDL_SCANCODE_4 => GameKeys.D4,
                SDL_SCANCODE_5 => GameKeys.D5,
                SDL_SCANCODE_6 => GameKeys.D6,
                SDL_SCANCODE_7 => GameKeys.D7,
                SDL_SCANCODE_8 => GameKeys.D8,
                SDL_SCANCODE_9 => GameKeys.D9,

                // Function keys
                SDL_SCANCODE_F1 => GameKeys.F1,
                SDL_SCANCODE_F2 => GameKeys.F2,
                SDL_SCANCODE_F3 => GameKeys.F3,
                SDL_SCANCODE_F4 => GameKeys.F4,
                SDL_SCANCODE_F5 => GameKeys.F5,
                SDL_SCANCODE_F6 => GameKeys.F6,
                SDL_SCANCODE_F7 => GameKeys.F7,
                SDL_SCANCODE_F8 => GameKeys.F8,
                SDL_SCANCODE_F9 => GameKeys.F9,
                SDL_SCANCODE_F10 => GameKeys.F10,
                SDL_SCANCODE_F11 => GameKeys.F11,
                SDL_SCANCODE_F12 => GameKeys.F12,

                // Arrow keys
                SDL_SCANCODE_LEFT => GameKeys.Left,
                SDL_SCANCODE_RIGHT => GameKeys.Right,
                SDL_SCANCODE_UP => GameKeys.Up,
                SDL_SCANCODE_DOWN => GameKeys.Down,

                // Control keys
                SDL_SCANCODE_RETURN => GameKeys.Enter,
                SDL_SCANCODE_ESCAPE => GameKeys.Escape,
                SDL_SCANCODE_BACKSPACE => GameKeys.Back,
                SDL_SCANCODE_TAB => GameKeys.Tab,
                SDL_SCANCODE_SPACE => GameKeys.Space,
                SDL_SCANCODE_DELETE => GameKeys.Delete,
                SDL_SCANCODE_HOME => GameKeys.Home,
                SDL_SCANCODE_END => GameKeys.End,
                SDL_SCANCODE_PAGEUP => GameKeys.PageUp,
                SDL_SCANCODE_PAGEDOWN => GameKeys.PageDown,

                // OEM keys
                SDL_SCANCODE_MINUS => GameKeys.OemMinus,
                SDL_SCANCODE_EQUALS => GameKeys.OemPlus,
                SDL_SCANCODE_COMMA => GameKeys.OemComma,
                SDL_SCANCODE_PERIOD => GameKeys.OemPeriod,
                SDL_SCANCODE_GRAVE => GameKeys.OemTilde,
                SDL_SCANCODE_SEMICOLON => GameKeys.Oem1,
                SDL_SCANCODE_SLASH => GameKeys.Oem2,
                SDL_SCANCODE_LEFTBRACKET => GameKeys.Oem4,
                SDL_SCANCODE_BACKSLASH => GameKeys.Oem5,
                SDL_SCANCODE_RIGHTBRACKET => GameKeys.Oem6,
                SDL_SCANCODE_APOSTROPHE => GameKeys.Oem7,

                _ => GameKeys.None,
            };
        }

        /// <summary>
        /// Maps an SDL3 mouse button index to a GameMouseButtons value.
        /// </summary>
        public static GameMouseButtons MapButton(byte button)
        {
            return button switch
            {
                SDL3Native.SDL_BUTTON_LEFT => GameMouseButtons.Left,
                SDL3Native.SDL_BUTTON_RIGHT => GameMouseButtons.Right,
                SDL3Native.SDL_BUTTON_MIDDLE => GameMouseButtons.Middle,
                _ => GameMouseButtons.None,
            };
        }
    }
}
