// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input.Bindings
{
    /// <summary>
    /// A collection of keys, mouse and other controllers' buttons.
    /// </summary>
    public enum InputKey
    {
        /// <summary>
        /// No key pressed.
        /// </summary>
        None = 0,
        /// <summary>
        /// The shift key.
        /// </summary>
        Shift = 1,
        /// <summary>
        /// The control key.
        /// </summary>
        Control = 3,
        /// <summary>
        /// The alt key.
        /// </summary>
        Alt = 5,
        /// <summary>
        /// The win key.
        /// </summary>
        Super = 7,
        /// <summary>
        /// The menu key.
        /// </summary>
        Menu = 9,
        /// <summary>
        /// The F1 key.
        /// </summary>
        F1 = 10,
        /// <summary>
        /// The F2 key.
        /// </summary>
        F2 = 11,
        /// <summary>
        /// The F3 key.
        /// </summary>
        F3 = 12,
        /// <summary>
        /// The F4 key.
        /// </summary>
        F4 = 13,
        /// <summary>
        /// The F5 key.
        /// </summary>
        F5 = 14,
        /// <summary>
        /// The F6 key.
        /// </summary>
        F6 = 15,
        /// <summary>
        /// The F7 key.
        /// </summary>
        F7 = 16,
        /// <summary>
        /// The F8 key.
        /// </summary>
        F8 = 17,
        /// <summary>
        /// The F9 key.
        /// </summary>
        F9 = 18,
        /// <summary>
        /// The F10 key.
        /// </summary>
        F10 = 19,
        /// <summary>
        /// The F11 key.
        /// </summary>
        F11 = 20,
        /// <summary>
        /// The F12 key.
        /// </summary>
        F12 = 21,
        /// <summary>
        /// The F13 key.
        /// </summary>
        F13 = 22,
        /// <summary>
        /// The F14 key.
        /// </summary>
        F14 = 23,
        /// <summary>
        /// The F15 key.
        /// </summary>
        F15 = 24,
        /// <summary>
        /// The F16 key.
        /// </summary>
        F16 = 25,
        /// <summary>
        /// The F17 key.
        /// </summary>
        F17 = 26,
        /// <summary>
        /// The F18 key.
        /// </summary>
        F18 = 27,
        /// <summary>
        /// The F19 key.
        /// </summary>
        F19 = 28,
        /// <summary>
        /// The F20 key.
        /// </summary>
        F20 = 29,
        /// <summary>
        /// The F21 key.
        /// </summary>
        F21 = 30,
        /// <summary>
        /// The F22 key.
        /// </summary>
        F22 = 31,
        /// <summary>
        /// The F23 key.
        /// </summary>
        F23 = 32,
        /// <summary>
        /// The F24 key.
        /// </summary>
        F24 = 33,
        /// <summary>
        /// The F25 key.
        /// </summary>
        F25 = 34,
        /// <summary>
        /// The F26 key.
        /// </summary>
        F26 = 35,
        /// <summary>
        /// The F27 key.
        /// </summary>
        F27 = 36,
        /// <summary>
        /// The F28 key.
        /// </summary>
        F28 = 37,
        /// <summary>
        /// The F29 key.
        /// </summary>
        F29 = 38,
        /// <summary>
        /// The F30 key.
        /// </summary>
        F30 = 39,
        /// <summary>
        /// The F31 key.
        /// </summary>
        F31 = 40,
        /// <summary>
        /// The F32 key.
        /// </summary>
        F32 = 41,
        /// <summary>
        /// The F33 key.
        /// </summary>
        F33 = 42,
        /// <summary>
        /// The F34 key.
        /// </summary>
        F34 = 43,
        /// <summary>
        /// The F35 key.
        /// </summary>
        F35 = 44,
        /// <summary>
        /// The up arrow key.
        /// </summary>
        Up = 45,
        /// <summary>
        /// The down arrow key.
        /// </summary>
        Down = 46,
        /// <summary>
        /// The left arrow key.
        /// </summary>
        Left = 47,
        /// <summary>
        /// The right arrow key.
        /// </summary>
        Right = 48,
        /// <summary>
        /// The enter key.
        /// </summary>
        Enter = 49,
        /// <summary>
        /// The escape key.
        /// </summary>
        Escape = 50,
        /// <summary>
        /// The space key.
        /// </summary>
        Space = 51,
        /// <summary>
        /// The tab key.
        /// </summary>
        Tab = 52,
        /// <summary>
        /// The backspace key.
        /// </summary>
        BackSpace = 53,
        /// <summary>
        /// The backspace key (equivalent to BackSpace).
        /// </summary>
        Back = 53,
        /// <summary>
        /// The insert key.
        /// </summary>
        Insert = 54,
        /// <summary>
        /// The delete key.
        /// </summary>
        Delete = 55,
        /// <summary>
        /// The page up key.
        /// </summary>
        PageUp = 56,
        /// <summary>
        /// The page down key.
        /// </summary>
        PageDown = 57,
        /// <summary>
        /// The home key.
        /// </summary>
        Home = 58,
        /// <summary>
        /// The end key.
        /// </summary>
        End = 59,
        /// <summary>
        /// The caps lock key.
        /// </summary>
        CapsLock = 60,
        /// <summary>
        /// The scroll lock key.
        /// </summary>
        ScrollLock = 61,
        /// <summary>
        /// The print screen key.
        /// </summary>
        PrintScreen = 62,
        /// <summary>
        /// The pause key.
        /// </summary>
        Pause = 63,
        /// <summary>
        /// The num lock key.
        /// </summary>
        NumLock = 64,
        /// <summary>
        /// The clear key (Keypad5 with NumLock disabled, on typical keyboards).
        /// </summary>
        Clear = 65,
        /// <summary>
        /// The sleep key.
        /// </summary>
        Sleep = 66,
        /// <summary>
        /// The keypad 0 key.
        /// </summary>
        Keypad0 = 67,
        /// <summary>
        /// The keypad 1 key.
        /// </summary>
        Keypad1 = 68,
        /// <summary>
        /// The keypad 2 key.
        /// </summary>
        Keypad2 = 69,
        /// <summary>
        /// The keypad 3 key.
        /// </summary>
        Keypad3 = 70,
        /// <summary>
        /// The keypad 4 key.
        /// </summary>
        Keypad4 = 71,
        /// <summary>
        /// The keypad 5 key.
        /// </summary>
        Keypad5 = 72,
        /// <summary>
        /// The keypad 6 key.
        /// </summary>
        Keypad6 = 73,
        /// <summary>
        /// The keypad 7 key.
        /// </summary>
        Keypad7 = 74,
        /// <summary>
        /// The keypad 8 key.
        /// </summary>
        Keypad8 = 75,
        /// <summary>
        /// The keypad 9 key.
        /// </summary>
        Keypad9 = 76,
        /// <summary>
        /// The keypad divide key.
        /// </summary>
        KeypadDivide = 77,
        /// <summary>
        /// The keypad multiply key.
        /// </summary>
        KeypadMultiply = 78,
        /// <summary>
        /// The keypad subtract key.
        /// </summary>
        KeypadSubtract = 79,
        /// <summary>
        /// The keypad minus key (equivalent to KeypadSubtract).
        /// </summary>
        KeypadMinus = 79,
        /// <summary>
        /// The keypad add key.
        /// </summary>
        KeypadAdd = 80,
        /// <summary>
        /// The keypad plus key (equivalent to KeypadAdd).
        /// </summary>
        KeypadPlus = 80,
        /// <summary>
        /// The keypad decimal key.
        /// </summary>
        KeypadDecimal = 81,
        /// <summary>
        /// The keypad period key (equivalent to KeypadDecimal).
        /// </summary>
        KeypadPeriod = 81,
        /// <summary>
        /// The keypad enter key.
        /// </summary>
        KeypadEnter = 82,
        /// <summary>
        /// The A key.
        /// </summary>
        A = 83,
        /// <summary>
        /// The B key.
        /// </summary>
        B = 84,
        /// <summary>
        /// The C key.
        /// </summary>
        C = 85,
        /// <summary>
        /// The D key.
        /// </summary>
        D = 86,
        /// <summary>
        /// The E key.
        /// </summary>
        E = 87,
        /// <summary>
        /// The F key.
        /// </summary>
        F = 88,
        /// <summary>
        /// The G key.
        /// </summary>
        G = 89,
        /// <summary>
        /// The H key.
        /// </summary>
        H = 90,
        /// <summary>
        /// The I key.
        /// </summary>
        I = 91,
        /// <summary>
        /// The J key.
        /// </summary>
        J = 92,
        /// <summary>
        /// The K key.
        /// </summary>
        K = 93,
        /// <summary>
        /// The L key.
        /// </summary>
        L = 94,
        /// <summary>
        /// The M key.
        /// </summary>
        M = 95,
        /// <summary>
        /// The N key.
        /// </summary>
        N = 96,
        /// <summary>
        /// The O key.
        /// </summary>
        O = 97,
        /// <summary>
        /// The P key.
        /// </summary>
        P = 98,
        /// <summary>
        /// The Q key.
        /// </summary>
        Q = 99,
        /// <summary>
        /// The R key.
        /// </summary>
        R = 100,
        /// <summary>
        /// The S key.
        /// </summary>
        S = 101,
        /// <summary>
        /// The T key.
        /// </summary>
        T = 102,
        /// <summary>
        /// The U key.
        /// </summary>
        U = 103,
        /// <summary>
        /// The V key.
        /// </summary>
        V = 104,
        /// <summary>
        /// The W key.
        /// </summary>
        W = 105,
        /// <summary>
        /// The X key.
        /// </summary>
        X = 106,
        /// <summary>
        /// The Y key.
        /// </summary>
        Y = 107,
        /// <summary>
        /// The Z key.
        /// </summary>
        Z = 108,
        /// <summary>
        /// The number 0 key.
        /// </summary>
        Number0 = 109,
        /// <summary>
        /// The number 1 key.
        /// </summary>
        Number1 = 110,
        /// <summary>
        /// The number 2 key.
        /// </summary>
        Number2 = 111,
        /// <summary>
        /// The number 3 key.
        /// </summary>
        Number3 = 112,
        /// <summary>
        /// The number 4 key.
        /// </summary>
        Number4 = 113,
        /// <summary>
        /// The number 5 key.
        /// </summary>
        Number5 = 114,
        /// <summary>
        /// The number 6 key.
        /// </summary>
        Number6 = 115,
        /// <summary>
        /// The number 7 key.
        /// </summary>
        Number7 = 116,
        /// <summary>
        /// The number 8 key.
        /// </summary>
        Number8 = 117,
        /// <summary>
        /// The number 9 key.
        /// </summary>
        Number9 = 118,
        /// <summary>
        /// The tilde key.
        /// </summary>
        Tilde = 119,
        /// <summary>
        /// The grave key (equivaent to Tilde).
        /// </summary>
        Grave = 119,
        /// <summary>
        /// The minus key.
        /// </summary>
        Minus = 120,
        /// <summary>
        /// The plus key.
        /// </summary>
        Plus = 121,
        /// <summary>
        /// The left bracket key.
        /// </summary>
        BracketLeft = 122,
        /// <summary>
        /// The left bracket key (equivalent to BracketLeft).
        /// </summary>
        LBracket = 122,
        /// <summary>
        /// The right bracket key.
        /// </summary>
        BracketRight = 123,
        /// <summary>
        /// The right bracket key (equivalent to BracketRight).
        /// </summary>
        RBracket = 123,
        /// <summary>
        /// The semicolon key.
        /// </summary>
        Semicolon = 124,
        /// <summary>
        /// The quote key.
        /// </summary>
        Quote = 125,
        /// <summary>
        /// The comma key.
        /// </summary>
        Comma = 126,
        /// <summary>
        /// The period key.
        /// </summary>
        Period = 127,
        /// <summary>
        /// The slash key.
        /// </summary>
        Slash = 128,
        /// <summary>
        /// The backslash key.
        /// </summary>
        BackSlash = 129,
        /// <summary>
        /// The secondary backslash key.
        /// </summary>
        NonUSBackSlash = 130,
        /// <summary>
        /// Indicates the last available keyboard key.
        /// </summary>
        LastKey = 131,

        FirstMouseButton = 132,

        /// <summary>
        /// The left mouse button.
        /// </summary>
        MouseLeft = 132,
        /// <summary>
        /// The middle mouse button.
        /// </summary>
        MouseMiddle = 133,
        /// <summary>
        /// The right mouse button.
        /// </summary>
        MouseRight = 134,
        /// <summary>
        /// The first extra mouse button.
        /// </summary>
        MouseButton1 = 135,
        /// <summary>
        /// The second extra mouse button.
        /// </summary>
        MouseButton2 = 136,
        /// <summary>
        /// The third extra mouse button.
        /// </summary>
        MouseButton3 = 137,
        /// <summary>
        /// The fourth extra mouse button.
        /// </summary>
        MouseButton4 = 138,
        /// <summary>
        /// The fifth extra mouse button.
        /// </summary>
        MouseButton5 = 139,
        /// <summary>
        /// The sixth extra mouse button.
        /// </summary>
        MouseButton6 = 140,
        /// <summary>
        /// The seventh extra mouse button.
        /// </summary>
        MouseButton7 = 141,
        /// <summary>
        /// The eigth extra mouse button.
        /// </summary>
        MouseButton8 = 142,
        /// <summary>
        /// The ninth extra mouse button.
        /// </summary>
        MouseButton9 = 143,
        /// <summary>
        /// Indicates the last available mouse button.
        /// </summary>
        MouseLastButton = 144,
        /// <summary>
        /// Mouse wheel rolled up.
        /// </summary>
        MouseWheelUp = 145,
        /// <summary>
        /// Mouse wheel rolled down.
        /// </summary>
        MouseWheelDown = 146,

        /// <summary>
        /// Indicates the first available joystick button.
        /// </summary>
        FirstJoystickButton = 1024,

        /// <summary>
        /// Joystick button 1.
        /// </summary>
        Joystick1,
        /// <summary>
        /// Joystick button 2.
        /// </summary>
        Joystick2,
        /// <summary>
        /// Joystick button 3.
        /// </summary>
        Joystick3,
        /// <summary>
        /// Joystick button 4.
        /// </summary>
        Joystick4,
        /// <summary>
        /// Joystick button 5.
        /// </summary>
        Joystick5,
        /// <summary>
        /// Joystick button 6.
        /// </summary>
        Joystick6,
        /// <summary>
        /// Joystick button 7.
        /// </summary>
        Joystick7,
        /// <summary>
        /// Joystick button 8.
        /// </summary>
        Joystick8,
        /// <summary>
        /// Joystick button 9.
        /// </summary>
        Joystick9,
        /// <summary>
        /// Joystick button 10.
        /// </summary>
        Joystick10,
        /// <summary>
        /// Joystick button 11.
        /// </summary>
        Joystick11,
        /// <summary>
        /// Joystick button 12.
        /// </summary>
        Joystick12,
        /// <summary>
        /// Joystick button 13.
        /// </summary>
        Joystick13,
        /// <summary>
        /// Joystick button 14.
        /// </summary>
        Joystick14,
        /// <summary>
        /// Joystick button 15.
        /// </summary>
        Joystick15,
        /// <summary>
        /// Joystick button 16.
        /// </summary>
        Joystick16,
        /// <summary>
        /// Joystick button 17.
        /// </summary>
        Joystick17,
        /// <summary>
        /// Joystick button 18.
        /// </summary>
        Joystick18,
        /// <summary>
        /// Joystick button 19.
        /// </summary>
        Joystick19,
        /// <summary>
        /// Joystick button 20.
        /// </summary>
        Joystick20,
        /// <summary>
        /// Joystick button 21.
        /// </summary>
        Joystick21,
        /// <summary>
        /// Joystick button 22.
        /// </summary>
        Joystick22,
        /// <summary>
        /// Joystick button 23.
        /// </summary>
        Joystick23,
        /// <summary>
        /// Joystick button 24.
        /// </summary>
        Joystick24,
        /// <summary>
        /// Joystick button 25.
        /// </summary>
        Joystick25,
        /// <summary>
        /// Joystick button 26.
        /// </summary>
        Joystick26,
        /// <summary>
        /// Joystick button 27.
        /// </summary>
        Joystick27,
        /// <summary>
        /// Joystick button 28.
        /// </summary>
        Joystick28,
        /// <summary>
        /// Joystick button 29.
        /// </summary>
        Joystick29,
        /// <summary>
        /// Joystick button 30.
        /// </summary>
        Joystick30,
        /// <summary>
        /// Joystick button 31.
        /// </summary>
        Joystick31,
        /// <summary>
        /// Joystick button 32.
        /// </summary>
        Joystick32,
        /// <summary>
        /// Joystick button 33.
        /// </summary>
        Joystick33,
        /// <summary>
        /// Joystick button 34.
        /// </summary>
        Joystick34,
        /// <summary>
        /// Joystick button 35.
        /// </summary>
        Joystick35,
        /// <summary>
        /// Joystick button 36.
        /// </summary>
        Joystick36,
        /// <summary>
        /// Joystick button 37.
        /// </summary>
        Joystick37,
        /// <summary>
        /// Joystick button 38.
        /// </summary>
        Joystick38,
        /// <summary>
        /// Joystick button 39.
        /// </summary>
        Joystick39,
        /// <summary>
        /// Joystick button 40.
        /// </summary>
        Joystick40,
        /// <summary>
        /// Joystick button 41.
        /// </summary>
        Joystick41,
        /// <summary>
        /// Joystick button 42.
        /// </summary>
        Joystick42,
        /// <summary>
        /// Joystick button 43.
        /// </summary>
        Joystick43,
        /// <summary>
        /// Joystick button 44.
        /// </summary>
        Joystick44,
        /// <summary>
        /// Joystick button 45.
        /// </summary>
        Joystick45,
        /// <summary>
        /// Joystick button 46.
        /// </summary>
        Joystick46,
        /// <summary>
        /// Joystick button 47.
        /// </summary>
        Joystick47,
        /// <summary>
        /// Joystick button 48.
        /// </summary>
        Joystick48,
        /// <summary>
        /// Joystick button 49.
        /// </summary>
        Joystick49,
        /// <summary>
        /// Joystick button 50.
        /// </summary>
        Joystick50,
        /// <summary>
        /// Joystick button 51.
        /// </summary>
        Joystick51,
        /// <summary>
        /// Joystick button 52.
        /// </summary>
        Joystick52,
        /// <summary>
        /// Joystick button 53.
        /// </summary>
        Joystick53,
        /// <summary>
        /// Joystick button 54.
        /// </summary>
        Joystick54,
        /// <summary>
        /// Joystick button 55.
        /// </summary>
        Joystick55,
        /// <summary>
        /// Joystick button 56.
        /// </summary>
        Joystick56,
        /// <summary>
        /// Joystick button 57.
        /// </summary>
        Joystick57,
        /// <summary>
        /// Joystick button 58.
        /// </summary>
        Joystick58,
        /// <summary>
        /// Joystick button 59.
        /// </summary>
        Joystick59,
        /// <summary>
        /// Joystick button 60.
        /// </summary>
        Joystick60,
        /// <summary>
        /// Joystick button 61.
        /// </summary>
        Joystick61,
        /// <summary>
        /// Joystick button 62.
        /// </summary>
        Joystick62,
        /// <summary>
        /// Joystick button 63.
        /// </summary>
        Joystick63,
        /// <summary>
        /// Joystick button 64.
        /// </summary>
        Joystick64,

        /// <summary>
        /// Indicates the first available negative-axis joystick button.
        /// </summary>
        FirstJoystickAxisNegativeButton = 2048,

        /// <summary>
        /// Joystick axis 1 negative button.
        /// </summary>
        JoystickAxisNegative1,
        /// <summary>
        /// Joystick axis 2 negative button.
        /// </summary>
        JoystickAxisNegative2,
        /// <summary>
        /// Joystick axis 3 negative button.
        /// </summary>
        JoystickAxisNegative3,
        /// <summary>
        /// Joystick axis 4 negative button.
        /// </summary>
        JoystickAxisNegative4,
        /// <summary>
        /// Joystick axis 5 negative button.
        /// </summary>
        JoystickAxisNegative5,
        /// <summary>
        /// Joystick axis 6 negative button.
        /// </summary>
        JoystickAxisNegative6,
        /// <summary>
        /// Joystick axis 7 negative button.
        /// </summary>
        JoystickAxisNegative7,
        /// <summary>
        /// Joystick axis 8 negative button.
        /// </summary>
        JoystickAxisNegative8,
        /// <summary>
        /// Joystick axis 9 negative button.
        /// </summary>
        JoystickAxisNegative9,
        /// <summary>
        /// Joystick axis 10 negative button.
        /// </summary>
        JoystickAxisNegative10,
        /// <summary>
        /// Joystick axis 11 negative button.
        /// </summary>
        JoystickAxisNegative11,
        /// <summary>
        /// Joystick axis 12 negative button.
        /// </summary>
        JoystickAxisNegative12,
        /// <summary>
        /// Joystick axis 13 negative button.
        /// </summary>
        JoystickAxisNegative13,
        /// <summary>
        /// Joystick axis 14 negative button.
        /// </summary>
        JoystickAxisNegative14,
        /// <summary>
        /// Joystick axis 15 negative button.
        /// </summary>
        JoystickAxisNegative15,
        /// <summary>
        /// Joystick axis 16 negative button.
        /// </summary>
        JoystickAxisNegative16,
        /// <summary>
        /// Joystick axis 17 negative button.
        /// </summary>
        JoystickAxisNegative17,
        /// <summary>
        /// Joystick axis 18 negative button.
        /// </summary>
        JoystickAxisNegative18,
        /// <summary>
        /// Joystick axis 19 negative button.
        /// </summary>
        JoystickAxisNegative19,
        /// <summary>
        /// Joystick axis 20 negative button.
        /// </summary>
        JoystickAxisNegative20,
        /// <summary>
        /// Joystick axis 21 negative button.
        /// </summary>
        JoystickAxisNegative21,
        /// <summary>
        /// Joystick axis 22 negative button.
        /// </summary>
        JoystickAxisNegative22,
        /// <summary>
        /// Joystick axis 23 negative button.
        /// </summary>
        JoystickAxisNegative23,
        /// <summary>
        /// Joystick axis 24 negative button.
        /// </summary>
        JoystickAxisNegative24,
        /// <summary>
        /// Joystick axis 25 negative button.
        /// </summary>
        JoystickAxisNegative25,
        /// <summary>
        /// Joystick axis 26 negative button.
        /// </summary>
        JoystickAxisNegative26,
        /// <summary>
        /// Joystick axis 27 negative button.
        /// </summary>
        JoystickAxisNegative27,
        /// <summary>
        /// Joystick axis 28 negative button.
        /// </summary>
        JoystickAxisNegative28,
        /// <summary>
        /// Joystick axis 29 negative button.
        /// </summary>
        JoystickAxisNegative29,
        /// <summary>
        /// Joystick axis 30 negative button.
        /// </summary>
        JoystickAxisNegative30,
        /// <summary>
        /// Joystick axis 31 negative button.
        /// </summary>
        JoystickAxisNegative31,
        /// <summary>
        /// Joystick axis 32 negative button.
        /// </summary>
        JoystickAxisNegative32,
        /// <summary>
        /// Joystick axis 33 negative button.
        /// </summary>
        JoystickAxisNegative33,
        /// <summary>
        /// Joystick axis 34 negative button.
        /// </summary>
        JoystickAxisNegative34,
        /// <summary>
        /// Joystick axis 35 negative button.
        /// </summary>
        JoystickAxisNegative35,
        /// <summary>
        /// Joystick axis 36 negative button.
        /// </summary>
        JoystickAxisNegative36,
        /// <summary>
        /// Joystick axis 37 negative button.
        /// </summary>
        JoystickAxisNegative37,
        /// <summary>
        /// Joystick axis 38 negative button.
        /// </summary>
        JoystickAxisNegative38,
        /// <summary>
        /// Joystick axis 39 negative button.
        /// </summary>
        JoystickAxisNegative39,
        /// <summary>
        /// Joystick axis 40 negative button.
        /// </summary>
        JoystickAxisNegative40,
        /// <summary>
        /// Joystick axis 41 negative button.
        /// </summary>
        JoystickAxisNegative41,
        /// <summary>
        /// Joystick axis 42 negative button.
        /// </summary>
        JoystickAxisNegative42,
        /// <summary>
        /// Joystick axis 43 negative button.
        /// </summary>
        JoystickAxisNegative43,
        /// <summary>
        /// Joystick axis 44 negative button.
        /// </summary>
        JoystickAxisNegative44,
        /// <summary>
        /// Joystick axis 45 negative button.
        /// </summary>
        JoystickAxisNegative45,
        /// <summary>
        /// Joystick axis 46 negative button.
        /// </summary>
        JoystickAxisNegative46,
        /// <summary>
        /// Joystick axis 47 negative button.
        /// </summary>
        JoystickAxisNegative47,
        /// <summary>
        /// Joystick axis 48 negative button.
        /// </summary>
        JoystickAxisNegative48,
        /// <summary>
        /// Joystick axis 49 negative button.
        /// </summary>
        JoystickAxisNegative49,
        /// <summary>
        /// Joystick axis 50 negative button.
        /// </summary>
        JoystickAxisNegative50,
        /// <summary>
        /// Joystick axis 51 negative button.
        /// </summary>
        JoystickAxisNegative51,
        /// <summary>
        /// Joystick axis 52 negative button.
        /// </summary>
        JoystickAxisNegative52,
        /// <summary>
        /// Joystick axis 53 negative button.
        /// </summary>
        JoystickAxisNegative53,
        /// <summary>
        /// Joystick axis 54 negative button.
        /// </summary>
        JoystickAxisNegative54,
        /// <summary>
        /// Joystick axis 55 negative button.
        /// </summary>
        JoystickAxisNegative55,
        /// <summary>
        /// Joystick axis 56 negative button.
        /// </summary>
        JoystickAxisNegative56,
        /// <summary>
        /// Joystick axis 57 negative button.
        /// </summary>
        JoystickAxisNegative57,
        /// <summary>
        /// Joystick axis 58 negative button.
        /// </summary>
        JoystickAxisNegative58,
        /// <summary>
        /// Joystick axis 59 negative button.
        /// </summary>
        JoystickAxisNegative59,
        /// <summary>
        /// Joystick axis 60 negative button.
        /// </summary>
        JoystickAxisNegative60,
        /// <summary>
        /// Joystick axis 61 negative button.
        /// </summary>
        JoystickAxisNegative61,
        /// <summary>
        /// Joystick axis 62 negative button.
        /// </summary>
        JoystickAxisNegative62,
        /// <summary>
        /// Joystick axis 63 negative button.
        /// </summary>
        JoystickAxisNegative63,
        /// <summary>
        /// Joystick axis 64 negative button.
        /// </summary>
        JoystickAxisNegative64,

        /// <summary>
        /// Indicates the first available positive-axis joystick button.
        /// </summary>
        FirstJoystickAxisPositiveButton = 3072,

        /// <summary>
        /// Joystick axis 1 positive button.
        /// </summary>
        JoystickAxisPositive1,
        /// <summary>
        /// Joystick axis 2 positive button.
        /// </summary>
        JoystickAxisPositive2,
        /// <summary>
        /// Joystick axis 3 positive button.
        /// </summary>
        JoystickAxisPositive3,
        /// <summary>
        /// Joystick axis 4 positive button.
        /// </summary>
        JoystickAxisPositive4,
        /// <summary>
        /// Joystick axis 5 positive button.
        /// </summary>
        JoystickAxisPositive5,
        /// <summary>
        /// Joystick axis 6 positive button.
        /// </summary>
        JoystickAxisPositive6,
        /// <summary>
        /// Joystick axis 7 positive button.
        /// </summary>
        JoystickAxisPositive7,
        /// <summary>
        /// Joystick axis 8 positive button.
        /// </summary>
        JoystickAxisPositive8,
        /// <summary>
        /// Joystick axis 9 positive button.
        /// </summary>
        JoystickAxisPositive9,
        /// <summary>
        /// Joystick axis 10 positive button.
        /// </summary>
        JoystickAxisPositive10,
        /// <summary>
        /// Joystick axis 11 positive button.
        /// </summary>
        JoystickAxisPositive11,
        /// <summary>
        /// Joystick axis 12 positive button.
        /// </summary>
        JoystickAxisPositive12,
        /// <summary>
        /// Joystick axis 13 positive button.
        /// </summary>
        JoystickAxisPositive13,
        /// <summary>
        /// Joystick axis 14 positive button.
        /// </summary>
        JoystickAxisPositive14,
        /// <summary>
        /// Joystick axis 15 positive button.
        /// </summary>
        JoystickAxisPositive15,
        /// <summary>
        /// Joystick axis 16 positive button.
        /// </summary>
        JoystickAxisPositive16,
        /// <summary>
        /// Joystick axis 17 positive button.
        /// </summary>
        JoystickAxisPositive17,
        /// <summary>
        /// Joystick axis 18 positive button.
        /// </summary>
        JoystickAxisPositive18,
        /// <summary>
        /// Joystick axis 19 positive button.
        /// </summary>
        JoystickAxisPositive19,
        /// <summary>
        /// Joystick axis 20 positive button.
        /// </summary>
        JoystickAxisPositive20,
        /// <summary>
        /// Joystick axis 21 positive button.
        /// </summary>
        JoystickAxisPositive21,
        /// <summary>
        /// Joystick axis 22 positive button.
        /// </summary>
        JoystickAxisPositive22,
        /// <summary>
        /// Joystick axis 23 positive button.
        /// </summary>
        JoystickAxisPositive23,
        /// <summary>
        /// Joystick axis 24 positive button.
        /// </summary>
        JoystickAxisPositive24,
        /// <summary>
        /// Joystick axis 25 positive button.
        /// </summary>
        JoystickAxisPositive25,
        /// <summary>
        /// Joystick axis 26 positive button.
        /// </summary>
        JoystickAxisPositive26,
        /// <summary>
        /// Joystick axis 27 positive button.
        /// </summary>
        JoystickAxisPositive27,
        /// <summary>
        /// Joystick axis 28 positive button.
        /// </summary>
        JoystickAxisPositive28,
        /// <summary>
        /// Joystick axis 29 positive button.
        /// </summary>
        JoystickAxisPositive29,
        /// <summary>
        /// Joystick axis 30 positive button.
        /// </summary>
        JoystickAxisPositive30,
        /// <summary>
        /// Joystick axis 31 positive button.
        /// </summary>
        JoystickAxisPositive31,
        /// <summary>
        /// Joystick axis 32 positive button.
        /// </summary>
        JoystickAxisPositive32,
        /// <summary>
        /// Joystick axis 33 positive button.
        /// </summary>
        JoystickAxisPositive33,
        /// <summary>
        /// Joystick axis 34 positive button.
        /// </summary>
        JoystickAxisPositive34,
        /// <summary>
        /// Joystick axis 35 positive button.
        /// </summary>
        JoystickAxisPositive35,
        /// <summary>
        /// Joystick axis 36 positive button.
        /// </summary>
        JoystickAxisPositive36,
        /// <summary>
        /// Joystick axis 37 positive button.
        /// </summary>
        JoystickAxisPositive37,
        /// <summary>
        /// Joystick axis 38 positive button.
        /// </summary>
        JoystickAxisPositive38,
        /// <summary>
        /// Joystick axis 39 positive button.
        /// </summary>
        JoystickAxisPositive39,
        /// <summary>
        /// Joystick axis 40 positive button.
        /// </summary>
        JoystickAxisPositive40,
        /// <summary>
        /// Joystick axis 41 positive button.
        /// </summary>
        JoystickAxisPositive41,
        /// <summary>
        /// Joystick axis 42 positive button.
        /// </summary>
        JoystickAxisPositive42,
        /// <summary>
        /// Joystick axis 43 positive button.
        /// </summary>
        JoystickAxisPositive43,
        /// <summary>
        /// Joystick axis 44 positive button.
        /// </summary>
        JoystickAxisPositive44,
        /// <summary>
        /// Joystick axis 45 positive button.
        /// </summary>
        JoystickAxisPositive45,
        /// <summary>
        /// Joystick axis 46 positive button.
        /// </summary>
        JoystickAxisPositive46,
        /// <summary>
        /// Joystick axis 47 positive button.
        /// </summary>
        JoystickAxisPositive47,
        /// <summary>
        /// Joystick axis 48 positive button.
        /// </summary>
        JoystickAxisPositive48,
        /// <summary>
        /// Joystick axis 49 positive button.
        /// </summary>
        JoystickAxisPositive49,
        /// <summary>
        /// Joystick axis 50 positive button.
        /// </summary>
        JoystickAxisPositive50,
        /// <summary>
        /// Joystick axis 51 positive button.
        /// </summary>
        JoystickAxisPositive51,
        /// <summary>
        /// Joystick axis 52 positive button.
        /// </summary>
        JoystickAxisPositive52,
        /// <summary>
        /// Joystick axis 53 positive button.
        /// </summary>
        JoystickAxisPositive53,
        /// <summary>
        /// Joystick axis 54 positive button.
        /// </summary>
        JoystickAxisPositive54,
        /// <summary>
        /// Joystick axis 55 positive button.
        /// </summary>
        JoystickAxisPositive55,
        /// <summary>
        /// Joystick axis 56 positive button.
        /// </summary>
        JoystickAxisPositive56,
        /// <summary>
        /// Joystick axis 57 positive button.
        /// </summary>
        JoystickAxisPositive57,
        /// <summary>
        /// Joystick axis 58 positive button.
        /// </summary>
        JoystickAxisPositive58,
        /// <summary>
        /// Joystick axis 59 positive button.
        /// </summary>
        JoystickAxisPositive59,
        /// <summary>
        /// Joystick axis 60 positive button.
        /// </summary>
        JoystickAxisPositive60,
        /// <summary>
        /// Joystick axis 61 positive button.
        /// </summary>
        JoystickAxisPositive61,
        /// <summary>
        /// Joystick axis 62 positive button.
        /// </summary>
        JoystickAxisPositive62,
        /// <summary>
        /// Joystick axis 63 positive button.
        /// </summary>
        JoystickAxisPositive63,
        /// <summary>
        /// Joystick axis 64 positive button.
        /// </summary>
        JoystickAxisPositive64,

        /// <summary>
        /// Indicates the first available joystick hat up button.
        /// </summary>
        FirstJoystickHatUpButton = 4096,

        /// <summary>
        /// Joystick hat 1 up button.
        /// </summary>
        JoystickHatUp1,
        /// <summary>
        /// Joystick hat 2 up button.
        /// </summary>
        JoystickHatUp2,
        /// <summary>
        /// Joystick hat 3 up button.
        /// </summary>
        JoystickHatUp3,
        /// <summary>
        /// Joystick hat 4 up button.
        /// </summary>
        JoystickHatUp4,

        /// <summary>
        /// Indicates the first available joystick hat down button.
        /// </summary>
        FirstJoystickHatDownButton = 5120,

        /// <summary>
        /// Joystick hat 1 down button.
        /// </summary>
        JoystickHatDown1,
        /// <summary>
        /// Joystick hat 2 down button.
        /// </summary>
        JoystickHatDown2,
        /// <summary>
        /// Joystick hat 3 down button.
        /// </summary>
        JoystickHatDown3,
        /// <summary>
        /// Joystick hat 4 down button.
        /// </summary>
        JoystickHatDown4,

        /// <summary>
        /// Indicates the first available joystick hat left button.
        /// </summary>
        FirstJoystickHatLeftButton = 6144,

        /// <summary>
        /// Joystick hat 1 left button.
        /// </summary>
        JoystickHatLeft1,
        /// <summary>
        /// Joystick hat 2 left button.
        /// </summary>
        JoystickHatLeft2,
        /// <summary>
        /// Joystick hat 3 left button.
        /// </summary>
        JoystickHatLeft3,
        /// <summary>
        /// Joystick hat 4 left button.
        /// </summary>
        JoystickHatLeft4,

        /// <summary>
        /// Indicates the first available joystick hat right button.
        /// </summary>
        FirstJoystickHatRightButton = 7168,

        /// <summary>
        /// Joystick hat 1 right button.
        /// </summary>
        JoystickHatRight1,
        /// <summary>
        /// Joystick hat 2 right button.
        /// </summary>
        JoystickHatRight2,
        /// <summary>
        /// Joystick hat 3 right button.
        /// </summary>
        JoystickHatRight3,
        /// <summary>
        /// Joystick hat 4 right button.
        /// </summary>
        JoystickHatRight4,
    }
}
