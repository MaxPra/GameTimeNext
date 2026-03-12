namespace GameTimeNext.Core.Framework.UserInput
{
    public class KeyInput
    {
        public enum VirtualKey : Int32
        {
            VK_NONE = 0,
            VK_NORESULT = -20,
            VK_CANCEL = 0x03,         // Control-break processing
            RESERVED_0x07 = 0x07,     // Reserved
            VK_BACK = 0x08,           // BACKSPACE key
            VK_TAB = 0x09,            // TAB key
            RESERVED_0x0A_0x0B = 0x0A, // Reserved
            VK_CLEAR = 0x0C,          // CLEAR key
            VK_RETURN = 0x0D,         // ENTER key
            RESERVED_0x0E_0x0F = 0x0E, // Unassigned
            VK_SHIFT = 0x10,          // SHIFT key
            VK_CONTROL = 0x11,        // CTRL key
            VK_MENU = 0x12,           // ALT key
            VK_PAUSE = 0x13,          // PAUSE key
            VK_CAPITAL = 0x14,        // CAPS LOCK key
            VK_KANA = 0x15,           // IME Kana mode
            VK_HANGUL = 0x15,         // IME Hangul mode
            VK_IME_ON = 0x16,         // IME On
            VK_JUNJA = 0x17,          // IME Junja mode
            VK_FINAL = 0x18,          // IME final mode
            VK_HANJA = 0x19,          // IME Hanja mode
            VK_KANJI = 0x19,          // IME Kanji mode
            VK_IME_OFF = 0x1A,        // IME Off
            VK_ESCAPE = 0x1B,         // ESC key
            VK_CONVERT = 0x1C,        // IME convert
            VK_NONCONVERT = 0x1D,     // IME nonconvert
            VK_ACCEPT = 0x1E,         // IME accept
            VK_MODECHANGE = 0x1F,     // IME mode change request
            VK_SPACE = 0x20,          // SPACEBAR
            VK_PRIOR = 0x21,          // PAGE UP key
            VK_NEXT = 0x22,           // PAGE DOWN key
            VK_END = 0x23,            // END key
            VK_HOME = 0x24,           // HOME key
            VK_LEFT = 0x25,           // LEFT ARROW key
            VK_UP = 0x26,             // UP ARROW key
            VK_RIGHT = 0x27,          // RIGHT ARROW key
            VK_DOWN = 0x28,           // DOWN ARROW key
            VK_SELECT = 0x29,         // SELECT key
            VK_PRINT = 0x2A,          // PRINT key
            VK_EXECUTE = 0x2B,        // EXECUTE key
            VK_SNAPSHOT = 0x2C,       // PRINT SCREEN key
            VK_INSERT = 0x2D,         // INS key
            VK_DELETE = 0x2E,         // DEL key
            VK_HELP = 0x2F,           // HELP key
            VK_0 = 0x30,              // 0 key
            VK_1 = 0x31,              // 1 key
            VK_2 = 0x32,              // 2 key
            VK_3 = 0x33,              // 3 key
            VK_4 = 0x34,              // 4 key
            VK_5 = 0x35,              // 5 key
            VK_6 = 0x36,              // 6 key
            VK_7 = 0x37,              // 7 key
            VK_8 = 0x38,              // 8 key
            VK_9 = 0x39,              // 9 key
            RESERVED_0x3A_0x40 = 0x3A, // Undefined
            VK_A = 0x41,              // A key
            VK_B = 0x42,              // B key
            VK_C = 0x43,              // C key
            VK_D = 0x44,              // D key
            VK_E = 0x45,              // E key
            VK_F = 0x46,              // F key
            VK_G = 0x47,              // G key
            VK_H = 0x48,              // H key
            VK_I = 0x49,              // I key
            VK_J = 0x4A,              // J key
            VK_K = 0x4B,              // K key
            VK_L = 0x4C,              // L key
            VK_M = 0x4D,              // M key
            VK_N = 0x4E,              // N key
            VK_O = 0x4F,              // O key
            VK_P = 0x50,              // P key
            VK_Q = 0x51,              // Q key
            VK_R = 0x52,              // R key
            VK_S = 0x53,              // S key
            VK_T = 0x54,              // T key
            VK_U = 0x55,              // U key
            VK_V = 0x56,              // V key
            VK_W = 0x57,              // W key
            VK_X = 0x58,              // X key
            VK_Y = 0x59,              // Y key
            VK_Z = 0x5A,              // Z key
            VK_LWIN = 0x5B,           // Left Windows key
            VK_RWIN = 0x5C,           // Right Windows key
            VK_APPS = 0x5D,           // Applications key
            RESERVED_0x5E = 0x5E,     // Reserved
            VK_SLEEP = 0x5F,          // Computer Sleep key
            VK_NUMPAD0 = 0x60,        // Numeric keypad 0 key
            VK_NUMPAD1 = 0x61,        // Numeric keypad 1 key
            VK_NUMPAD2 = 0x62,        // Numeric keypad 2 key
            VK_NUMPAD3 = 0x63,        // Numeric keypad 3 key
            VK_NUMPAD4 = 0x64,        // Numeric keypad 4 key
            VK_NUMPAD5 = 0x65,        // Numeric keypad 5 key
            VK_NUMPAD6 = 0x66,        // Numeric keypad 6 key
            VK_NUMPAD7 = 0x67,        // Numeric keypad 7 key
            VK_NUMPAD8 = 0x68,        // Numeric keypad 8 key
            VK_NUMPAD9 = 0x69,        // Numeric keypad 9 key
            VK_MULTIPLY = 0x6A,       // Multiply key
            VK_ADD = 0x6B,            // Add key
            VK_SEPARATOR = 0x6C,      // Separator key
            VK_SUBTRACT = 0x6D,       // Subtract key
            VK_DECIMAL = 0x6E,        // Decimal key
            VK_DIVIDE = 0x6F,         // Divide key
            VK_F1 = 0x70,             // F1 key
            VK_F2 = 0x71,             // F2 key
            VK_F3 = 0x72,             // F3 key
            VK_F4 = 0x73,             // F4 key
            VK_F5 = 0x74,             // F5 key
            VK_F6 = 0x75,             // F6 key
            VK_F7 = 0x76,             // F7 key
            VK_F8 = 0x77,             // F8 key
            VK_F9 = 0x78,             // F9 key
            VK_F10 = 0x79,            // F10 key
            VK_F11 = 0x7A,            // F11 key
            VK_F12 = 0x7B,            // F12 key
            VK_F13 = 0x7C,            // F13 key
            VK_F14 = 0x7D,            // F14 key
            VK_F15 = 0x7E,            // F15 key
            VK_F16 = 0x7F,            // F16 key
            VK_F17 = 0x80,            // F17 key
            VK_F18 = 0x81,            // F18 key
            VK_F19 = 0x82,            // F19 key
            VK_F20 = 0x83,            // F20 key
            VK_F21 = 0x84,            // F21 key
            VK_F22 = 0x85,            // F22 key
            VK_F23 = 0x86,            // F23 key
            VK_F24 = 0x87,            // F24 key
            RESERVED_0x88_0x8F = 0x88, // Reserved
            VK_NUMLOCK = 0x90,        // NUM LOCK key
            VK_SCROLL = 0x91,         // SCROLL LOCK key
            RESERVED_0x92_0x96 = 0x92, // OEM specific
            RESERVED_0x97_0x9F = 0x97, // Unassigned
            VK_LSHIFT = 0xA0,         // Left SHIFT key
            VK_RSHIFT = 0xA1,         // Right SHIFT key
            VK_LCONTROL = 0xA2,       // Left CONTROL key
            VK_RCONTROL = 0xA3,       // Right CONTROL key
            VK_LMENU = 0xA4,          // Left ALT key
            VK_RMENU = 0xA5,          // Right ALT key
            VK_BROWSER_BACK = 0xA6,   // Browser Back key
            VK_BROWSER_FORWARD = 0xA7, // Browser Forward key
            VK_BROWSER_REFRESH = 0xA8, // Browser Refresh key
            VK_BROWSER_STOP = 0xA9,   // Browser Stop key
            VK_BROWSER_SEARCH = 0xAA,  // Browser Search key
            VK_BROWSER_FAVORITES = 0xAB, // Browser Favorites key
            VK_BROWSER_HOME = 0xAC,    // Browser Start and Home key
            VK_VOLUME_MUTE = 0xAD,     // Volume Mute key
            VK_VOLUME_DOWN = 0xAE,     // Volume Down key
            VK_VOLUME_UP = 0xAF,       // Volume Up key
            VK_MEDIA_NEXT_TRACK = 0xB0, // Next Track key
            VK_MEDIA_PREV_TRACK = 0xB1, // Previous Track key
            VK_MEDIA_STOP = 0xB2,      // Stop Media key
            VK_MEDIA_PLAY_PAUSE = 0xB3, // Play/Pause Media key
            VK_LAUNCH_MAIL = 0xB4,     // Start Mail key
            VK_LAUNCH_MEDIA_SELECT = 0xB5, // Select Media key
            VK_LAUNCH_APP1 = 0xB6,     // Start Application 1 key
            VK_LAUNCH_APP2 = 0xB7,     // Start Application 2 key
            RESERVED_0xB8_0xB9 = 0xB8, // Reserved
            VK_OEM_1 = 0xBA,           // Miscellaneous characters
            VK_OEM_PLUS = 0xBB,        // Plus key (+)
            VK_OEM_COMMA = 0xBC,       // Comma key (,)
            VK_OEM_MINUS = 0xBD,       // Minus key (-)
            VK_OEM_PERIOD = 0xBE,      // Period key (.)
            VK_OEM_2 = 0xBF,           // Miscellaneous characters (/?)
            VK_OEM_3 = 0xC0,           // Miscellaneous characters (`~)
            RESERVED_0xC1_0xDA = 0xC1, // Reserved
            VK_OEM_4 = 0xDB,           // Miscellaneous characters ([{)
            VK_OEM_5 = 0xDC,           // Miscellaneous characters (\\|)
            VK_OEM_6 = 0xDD,           // Miscellaneous characters (]} key
            VK_OEM_7 = 0xDE,           // Miscellaneous characters ('" key)
            VK_OEM_8 = 0xDF,           // Miscellaneous characters
            RESERVED_0xE0 = 0xE0,      // Reserved
            RESERVED_0xE1 = 0xE1,      // OEM specific
            VK_OEM_102 = 0xE2,         // <> key
            RESERVED_0xE3_0xE4 = 0xE3, // OEM specific
            VK_PROCESSKEY = 0xE5,      // IME PROCESS key
            RESERVED_0xE6 = 0xE6,      // OEM specific
            VK_PACKET = 0xE7,          // Used to pass Unicode characters as keystrokes
            RESERVED_0xE8 = 0xE8,      // Unassigned
            RESERVED_0xE9_0xF5 = 0xE9, // OEM specific
            VK_ATTN = 0xF6,            // Attn key
            VK_CRSEL = 0xF7,           // CrSel key
            VK_EXSEL = 0xF8,           // ExSel key
            VK_EREOF = 0xF9,           // Erase EOF key
            VK_PLAY = 0xFA,            // Play key
            VK_ZOOM = 0xFB,            // Zoom key
            VK_NONAME = 0xFC,          // Reserved
            VK_PA1 = 0xFD,             // PA1 key
            VK_OEM_CLEAR = 0xFE        // Clear key
        }

        // Erstellen der HashMap (Dictionary)
        public static Dictionary<VirtualKey, string> virtualKeyMap = new Dictionary<VirtualKey, string>
        {

            { VirtualKey.VK_CANCEL, "Control-break processing" },
            { VirtualKey.VK_BACK, "BACKSPACE" },
            { VirtualKey.VK_TAB, "TAB" },
            { VirtualKey.VK_CLEAR, "CLEAR" },
            { VirtualKey.VK_RETURN, "ENTER" },
            { VirtualKey.VK_SHIFT, "SHIFT" },
            { VirtualKey.VK_CONTROL, "CTRL" },
            { VirtualKey.VK_MENU, "ALT" },
            { VirtualKey.VK_PAUSE, "PAUSE" },
            { VirtualKey.VK_CAPITAL, "CAPS LOCK" },
            { VirtualKey.VK_ESCAPE, "ESC" },
            { VirtualKey.VK_SPACE, "SPACEBAR" },
            { VirtualKey.VK_PRIOR, "PAGE UP" },
            { VirtualKey.VK_NEXT, "PAGE DOWN" },
            { VirtualKey.VK_END, "END" },
            { VirtualKey.VK_HOME, "HOME" },
            { VirtualKey.VK_LEFT, "LEFT ARROW" },
            { VirtualKey.VK_UP, "UP ARROW" },
            { VirtualKey.VK_RIGHT, "RIGHT ARROW" },
            { VirtualKey.VK_DOWN, "DOWN ARROW" },
            { VirtualKey.VK_SELECT, "SELECT" },
            { VirtualKey.VK_PRINT, "PRINT" },
            { VirtualKey.VK_EXECUTE, "EXECUTE" },
            { VirtualKey.VK_SNAPSHOT, "PRINT SCREEN" },
            { VirtualKey.VK_INSERT, "INS" },
            { VirtualKey.VK_DELETE, "DEL" },
            { VirtualKey.VK_HELP, "HELP" },
            { VirtualKey.VK_0, "0" },
            { VirtualKey.VK_1, "1" },
            { VirtualKey.VK_2, "2" },
            { VirtualKey.VK_3, "3" },
            { VirtualKey.VK_4, "4" },
            { VirtualKey.VK_5, "5" },
            { VirtualKey.VK_6, "6" },
            { VirtualKey.VK_7, "7" },
            { VirtualKey.VK_8, "8" },
            { VirtualKey.VK_9, "9" },
            { VirtualKey.VK_A, "A" },
            { VirtualKey.VK_B, "B" },
            { VirtualKey.VK_C, "C" },
            { VirtualKey.VK_D, "D" },
            { VirtualKey.VK_E, "E" },
            { VirtualKey.VK_F, "F" },
            { VirtualKey.VK_G, "G" },
            { VirtualKey.VK_H, "H" },
            { VirtualKey.VK_I, "I" },
            { VirtualKey.VK_J, "J" },
            { VirtualKey.VK_K, "K" },
            { VirtualKey.VK_L, "L" },
            { VirtualKey.VK_M, "M" },
            { VirtualKey.VK_N, "N" },
            { VirtualKey.VK_O, "O" },
            { VirtualKey.VK_P, "P" },
            { VirtualKey.VK_Q, "Q" },
            { VirtualKey.VK_R, "R" },
            { VirtualKey.VK_S, "S" },
            { VirtualKey.VK_T, "T" },
            { VirtualKey.VK_U, "U" },
            { VirtualKey.VK_V, "V" },
            { VirtualKey.VK_W, "W" },
            { VirtualKey.VK_X, "X" },
            { VirtualKey.VK_Y, "Y" },
            { VirtualKey.VK_Z, "Z" },
            { VirtualKey.VK_LWIN, "Left Windows" },
            { VirtualKey.VK_RWIN, "Right Windows" },
            { VirtualKey.VK_APPS, "Applications" },
            { VirtualKey.VK_SLEEP, "Sleep" },
            { VirtualKey.VK_NUMPAD0, "Numeric keypad 0" },
            { VirtualKey.VK_NUMPAD1, "Numeric keypad 1" },
            { VirtualKey.VK_NUMPAD2, "Numeric keypad 2" },
            { VirtualKey.VK_NUMPAD3, "Numeric keypad 3" },
            { VirtualKey.VK_NUMPAD4, "Numeric keypad 4" },
            { VirtualKey.VK_NUMPAD5, "Numeric keypad 5" },
            { VirtualKey.VK_NUMPAD6, "Numeric keypad 6" },
            { VirtualKey.VK_NUMPAD7, "Numeric keypad 7" },
            { VirtualKey.VK_NUMPAD8, "Numeric keypad 8" },
            { VirtualKey.VK_NUMPAD9, "Numeric keypad 9" },
            { VirtualKey.VK_MULTIPLY, "Multiply" },
            { VirtualKey.VK_ADD, "Add" },
            { VirtualKey.VK_SEPARATOR, "Separator" },
            { VirtualKey.VK_SUBTRACT, "Subtract" },
            { VirtualKey.VK_DECIMAL, "Decimal" },
            { VirtualKey.VK_DIVIDE, "Divide" },
            { VirtualKey.VK_F1, "F1" },
            { VirtualKey.VK_F2, "F2" },
            { VirtualKey.VK_F3, "F3" },
            { VirtualKey.VK_F4, "F4" },
            { VirtualKey.VK_F5, "F5" },
            { VirtualKey.VK_F6, "F6" },
            { VirtualKey.VK_F7, "F7" },
            { VirtualKey.VK_F8, "F8" },
            { VirtualKey.VK_F9, "F9" },
            { VirtualKey.VK_F10, "F10" },
            { VirtualKey.VK_F11, "F11" },
            { VirtualKey.VK_F12, "F12" },
            { VirtualKey.VK_F13, "F13" },
            { VirtualKey.VK_F14, "F14" },
            { VirtualKey.VK_F15, "F15" },
            { VirtualKey.VK_F16, "F16" },
            { VirtualKey.VK_F17, "F17" },
            { VirtualKey.VK_F18, "F18" },
            { VirtualKey.VK_F19, "F19" },
            { VirtualKey.VK_F20, "F20" },
            { VirtualKey.VK_F21, "F21" },
            { VirtualKey.VK_F22, "F22" },
            { VirtualKey.VK_F23, "F23" },
            { VirtualKey.VK_F24, "F24" },
            { VirtualKey.VK_NUMLOCK, "NUM LOCK" },
            { VirtualKey.VK_SCROLL, "SCROLL LOCK" },
            { VirtualKey.VK_LSHIFT, "Left SHIFT" },
            { VirtualKey.VK_RSHIFT, "Right SHIFT" },
            { VirtualKey.VK_LCONTROL, "Left CONTROL" },
            { VirtualKey.VK_RCONTROL, "Right CONTROL" },
            { VirtualKey.VK_LMENU, "Left ALT" },
            { VirtualKey.VK_RMENU, "Right ALT" },
            { VirtualKey.VK_BROWSER_BACK, "Browser Back" },
            { VirtualKey.VK_BROWSER_FORWARD, "Browser Forward" },
            { VirtualKey.VK_BROWSER_REFRESH, "Browser Refresh" },
            { VirtualKey.VK_BROWSER_STOP, "Browser Stop" },
            { VirtualKey.VK_BROWSER_SEARCH, "Browser Search" },
            { VirtualKey.VK_BROWSER_FAVORITES, "Browser Favorites" },
            { VirtualKey.VK_BROWSER_HOME, "Browser Start/Home" },
            { VirtualKey.VK_VOLUME_MUTE, "Volume Mute" },
            { VirtualKey.VK_VOLUME_DOWN, "Volume Down" },
            { VirtualKey.VK_VOLUME_UP, "Volume Up" },
            { VirtualKey.VK_MEDIA_NEXT_TRACK, "Next Track" },
            { VirtualKey.VK_MEDIA_PREV_TRACK, "Previous Track" },
            { VirtualKey.VK_MEDIA_STOP, "Stop Media" },
            { VirtualKey.VK_MEDIA_PLAY_PAUSE, "Play/Pause Media" },
            { VirtualKey.VK_LAUNCH_MAIL, "Start Mail" },
            { VirtualKey.VK_LAUNCH_MEDIA_SELECT, "Select Media" },
            { VirtualKey.VK_LAUNCH_APP1, "Start Application 1" },
            { VirtualKey.VK_LAUNCH_APP2, "Start Application 2" },
            { VirtualKey.VK_OEM_1, "Miscellaneous characters (US standard keyboard ;: key)" },
            { VirtualKey.VK_OEM_PLUS, "+ key" },
            { VirtualKey.VK_OEM_COMMA, ", key" },
            { VirtualKey.VK_OEM_MINUS, "- key" },
            { VirtualKey.VK_OEM_PERIOD, ". key" },
            { VirtualKey.VK_OEM_2, "/? key" },
            { VirtualKey.VK_OEM_3, "`~ key" },
            { VirtualKey.VK_OEM_4, "[{ key" },
            { VirtualKey.VK_OEM_5, "\\| key" },
            { VirtualKey.VK_OEM_6, "] key" },
            { VirtualKey.VK_OEM_7, "'\" key" },
            { VirtualKey.VK_OEM_8, "Miscellaneous characters" },
            { VirtualKey.VK_OEM_102, "<> keys (US standard keyboard)" },
            { VirtualKey.VK_PROCESSKEY, "IME PROCESS key" },
            { VirtualKey.VK_PACKET, "Used to pass Unicode characters" },
            { VirtualKey.VK_ATTN, "Attn key" },
            { VirtualKey.VK_CRSEL, "CrSel key" },
            { VirtualKey.VK_EXSEL, "ExSel key" },
            { VirtualKey.VK_EREOF, "Erase EOF key" },
            { VirtualKey.VK_PLAY, "Play key" },
            { VirtualKey.VK_ZOOM, "Zoom key" },
            { VirtualKey.VK_NONAME, "Reserved" },
            { VirtualKey.VK_PA1, "PA1 key" },
            { VirtualKey.VK_OEM_CLEAR, "Clear key" },
            { VirtualKey.VK_NONE, "(no key)" },
            { VirtualKey.VK_NORESULT, "(no key)" }


        };

    }
}
