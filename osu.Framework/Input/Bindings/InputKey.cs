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
        JoystickAxis1Negative,
        /// <summary>
        /// Joystick axis 2 negative button.
        /// </summary>
        JoystickAxis2Negative,
        /// <summary>
        /// Joystick axis 3 negative button.
        /// </summary>
        JoystickAxis3Negative,
        /// <summary>
        /// Joystick axis 4 negative button.
        /// </summary>
        JoystickAxis4Negative,
        /// <summary>
        /// Joystick axis 5 negative button.
        /// </summary>
        JoystickAxis5Negative,
        /// <summary>
        /// Joystick axis 6 negative button.
        /// </summary>
        JoystickAxis6Negative,
        /// <summary>
        /// Joystick axis 7 negative button.
        /// </summary>
        JoystickAxis7Negative,
        /// <summary>
        /// Joystick axis 8 negative button.
        /// </summary>
        JoystickAxis8Negative,
        /// <summary>
        /// Joystick axis 9 negative button.
        /// </summary>
        JoystickAxis9Negative,
        /// <summary>
        /// Joystick axis 10 negative button.
        /// </summary>
        JoystickAxis10Negative,
        /// <summary>
        /// Joystick axis 11 negative button.
        /// </summary>
        JoystickAxis11Negative,
        /// <summary>
        /// Joystick axis 12 negative button.
        /// </summary>
        JoystickAxis12Negative,
        /// <summary>
        /// Joystick axis 13 negative button.
        /// </summary>
        JoystickAxis13Negative,
        /// <summary>
        /// Joystick axis 14 negative button.
        /// </summary>
        JoystickAxis14Negative,
        /// <summary>
        /// Joystick axis 15 negative button.
        /// </summary>
        JoystickAxis15Negative,
        /// <summary>
        /// Joystick axis 16 negative button.
        /// </summary>
        JoystickAxis16Negative,
        /// <summary>
        /// Joystick axis 17 negative button.
        /// </summary>
        JoystickAxis17Negative,
        /// <summary>
        /// Joystick axis 18 negative button.
        /// </summary>
        JoystickAxis18Negative,
        /// <summary>
        /// Joystick axis 19 negative button.
        /// </summary>
        JoystickAxis19Negative,
        /// <summary>
        /// Joystick axis 20 negative button.
        /// </summary>
        JoystickAxis20Negative,
        /// <summary>
        /// Joystick axis 21 negative button.
        /// </summary>
        JoystickAxis21Negative,
        /// <summary>
        /// Joystick axis 22 negative button.
        /// </summary>
        JoystickAxis22Negative,
        /// <summary>
        /// Joystick axis 23 negative button.
        /// </summary>
        JoystickAxis23Negative,
        /// <summary>
        /// Joystick axis 24 negative button.
        /// </summary>
        JoystickAxis24Negative,
        /// <summary>
        /// Joystick axis 25 negative button.
        /// </summary>
        JoystickAxis25Negative,
        /// <summary>
        /// Joystick axis 26 negative button.
        /// </summary>
        JoystickAxis26Negative,
        /// <summary>
        /// Joystick axis 27 negative button.
        /// </summary>
        JoystickAxis27Negative,
        /// <summary>
        /// Joystick axis 28 negative button.
        /// </summary>
        JoystickAxis28Negative,
        /// <summary>
        /// Joystick axis 29 negative button.
        /// </summary>
        JoystickAxis29Negative,
        /// <summary>
        /// Joystick axis 30 negative button.
        /// </summary>
        JoystickAxis30Negative,
        /// <summary>
        /// Joystick axis 31 negative button.
        /// </summary>
        JoystickAxis31Negative,
        /// <summary>
        /// Joystick axis 32 negative button.
        /// </summary>
        JoystickAxis32Negative,
        /// <summary>
        /// Joystick axis 33 negative button.
        /// </summary>
        JoystickAxis33Negative,
        /// <summary>
        /// Joystick axis 34 negative button.
        /// </summary>
        JoystickAxis34Negative,
        /// <summary>
        /// Joystick axis 35 negative button.
        /// </summary>
        JoystickAxis35Negative,
        /// <summary>
        /// Joystick axis 36 negative button.
        /// </summary>
        JoystickAxis36Negative,
        /// <summary>
        /// Joystick axis 37 negative button.
        /// </summary>
        JoystickAxis37Negative,
        /// <summary>
        /// Joystick axis 38 negative button.
        /// </summary>
        JoystickAxis38Negative,
        /// <summary>
        /// Joystick axis 39 negative button.
        /// </summary>
        JoystickAxis39Negative,
        /// <summary>
        /// Joystick axis 40 negative button.
        /// </summary>
        JoystickAxis40Negative,
        /// <summary>
        /// Joystick axis 41 negative button.
        /// </summary>
        JoystickAxis41Negative,
        /// <summary>
        /// Joystick axis 42 negative button.
        /// </summary>
        JoystickAxis42Negative,
        /// <summary>
        /// Joystick axis 43 negative button.
        /// </summary>
        JoystickAxis43Negative,
        /// <summary>
        /// Joystick axis 44 negative button.
        /// </summary>
        JoystickAxis44Negative,
        /// <summary>
        /// Joystick axis 45 negative button.
        /// </summary>
        JoystickAxis45Negative,
        /// <summary>
        /// Joystick axis 46 negative button.
        /// </summary>
        JoystickAxis46Negative,
        /// <summary>
        /// Joystick axis 47 negative button.
        /// </summary>
        JoystickAxis47Negative,
        /// <summary>
        /// Joystick axis 48 negative button.
        /// </summary>
        JoystickAxis48Negative,
        /// <summary>
        /// Joystick axis 49 negative button.
        /// </summary>
        JoystickAxis49Negative,
        /// <summary>
        /// Joystick axis 50 negative button.
        /// </summary>
        JoystickAxis50Negative,
        /// <summary>
        /// Joystick axis 51 negative button.
        /// </summary>
        JoystickAxis51Negative,
        /// <summary>
        /// Joystick axis 52 negative button.
        /// </summary>
        JoystickAxis52Negative,
        /// <summary>
        /// Joystick axis 53 negative button.
        /// </summary>
        JoystickAxis53Negative,
        /// <summary>
        /// Joystick axis 54 negative button.
        /// </summary>
        JoystickAxis54Negative,
        /// <summary>
        /// Joystick axis 55 negative button.
        /// </summary>
        JoystickAxis55Negative,
        /// <summary>
        /// Joystick axis 56 negative button.
        /// </summary>
        JoystickAxis56Negative,
        /// <summary>
        /// Joystick axis 57 negative button.
        /// </summary>
        JoystickAxis57Negative,
        /// <summary>
        /// Joystick axis 58 negative button.
        /// </summary>
        JoystickAxis58Negative,
        /// <summary>
        /// Joystick axis 59 negative button.
        /// </summary>
        JoystickAxis59Negative,
        /// <summary>
        /// Joystick axis 60 negative button.
        /// </summary>
        JoystickAxis60Negative,
        /// <summary>
        /// Joystick axis 61 negative button.
        /// </summary>
        JoystickAxis61Negative,
        /// <summary>
        /// Joystick axis 62 negative button.
        /// </summary>
        JoystickAxis62Negative,
        /// <summary>
        /// Joystick axis 63 negative button.
        /// </summary>
        JoystickAxis63Negative,
        /// <summary>
        /// Joystick axis 64 negative button.
        /// </summary>
        JoystickAxis64Negative,

        /// <summary>
        /// Indicates the first available positive-axis joystick button.
        /// </summary>
        FirstJoystickAxisPositiveButton = 3072,

        /// <summary>
        /// Joystick axis 1 positive button.
        /// </summary>
        JoystickAxis1Positive,
        /// <summary>
        /// Joystick axis 2 positive button.
        /// </summary>
        JoystickAxis2Positive,
        /// <summary>
        /// Joystick axis 3 positive button.
        /// </summary>
        JoystickAxis3Positive,
        /// <summary>
        /// Joystick axis 4 positive button.
        /// </summary>
        JoystickAxis4Positive,
        /// <summary>
        /// Joystick axis 5 positive button.
        /// </summary>
        JoystickAxis5Positive,
        /// <summary>
        /// Joystick axis 6 positive button.
        /// </summary>
        JoystickAxis6Positive,
        /// <summary>
        /// Joystick axis 7 positive button.
        /// </summary>
        JoystickAxis7Positive,
        /// <summary>
        /// Joystick axis 8 positive button.
        /// </summary>
        JoystickAxis8Positive,
        /// <summary>
        /// Joystick axis 9 positive button.
        /// </summary>
        JoystickAxis9Positive,
        /// <summary>
        /// Joystick axis 10 positive button.
        /// </summary>
        JoystickAxis10Positive,
        /// <summary>
        /// Joystick axis 11 positive button.
        /// </summary>
        JoystickAxis11Positive,
        /// <summary>
        /// Joystick axis 12 positive button.
        /// </summary>
        JoystickAxis12Positive,
        /// <summary>
        /// Joystick axis 13 positive button.
        /// </summary>
        JoystickAxis13Positive,
        /// <summary>
        /// Joystick axis 14 positive button.
        /// </summary>
        JoystickAxis14Positive,
        /// <summary>
        /// Joystick axis 15 positive button.
        /// </summary>
        JoystickAxis15Positive,
        /// <summary>
        /// Joystick axis 16 positive button.
        /// </summary>
        JoystickAxis16Positive,
        /// <summary>
        /// Joystick axis 17 positive button.
        /// </summary>
        JoystickAxis17Positive,
        /// <summary>
        /// Joystick axis 18 positive button.
        /// </summary>
        JoystickAxis18Positive,
        /// <summary>
        /// Joystick axis 19 positive button.
        /// </summary>
        JoystickAxis19Positive,
        /// <summary>
        /// Joystick axis 20 positive button.
        /// </summary>
        JoystickAxis20Positive,
        /// <summary>
        /// Joystick axis 21 positive button.
        /// </summary>
        JoystickAxis21Positive,
        /// <summary>
        /// Joystick axis 22 positive button.
        /// </summary>
        JoystickAxis22Positive,
        /// <summary>
        /// Joystick axis 23 positive button.
        /// </summary>
        JoystickAxis23Positive,
        /// <summary>
        /// Joystick axis 24 positive button.
        /// </summary>
        JoystickAxis24Positive,
        /// <summary>
        /// Joystick axis 25 positive button.
        /// </summary>
        JoystickAxis25Positive,
        /// <summary>
        /// Joystick axis 26 positive button.
        /// </summary>
        JoystickAxis26Positive,
        /// <summary>
        /// Joystick axis 27 positive button.
        /// </summary>
        JoystickAxis27Positive,
        /// <summary>
        /// Joystick axis 28 positive button.
        /// </summary>
        JoystickAxis28Positive,
        /// <summary>
        /// Joystick axis 29 positive button.
        /// </summary>
        JoystickAxis29Positive,
        /// <summary>
        /// Joystick axis 30 positive button.
        /// </summary>
        JoystickAxis30Positive,
        /// <summary>
        /// Joystick axis 31 positive button.
        /// </summary>
        JoystickAxis31Positive,
        /// <summary>
        /// Joystick axis 32 positive button.
        /// </summary>
        JoystickAxis32Positive,
        /// <summary>
        /// Joystick axis 33 positive button.
        /// </summary>
        JoystickAxis33Positive,
        /// <summary>
        /// Joystick axis 34 positive button.
        /// </summary>
        JoystickAxis34Positive,
        /// <summary>
        /// Joystick axis 35 positive button.
        /// </summary>
        JoystickAxis35Positive,
        /// <summary>
        /// Joystick axis 36 positive button.
        /// </summary>
        JoystickAxis36Positive,
        /// <summary>
        /// Joystick axis 37 positive button.
        /// </summary>
        JoystickAxis37Positive,
        /// <summary>
        /// Joystick axis 38 positive button.
        /// </summary>
        JoystickAxis38Positive,
        /// <summary>
        /// Joystick axis 39 positive button.
        /// </summary>
        JoystickAxis39Positive,
        /// <summary>
        /// Joystick axis 40 positive button.
        /// </summary>
        JoystickAxis40Positive,
        /// <summary>
        /// Joystick axis 41 positive button.
        /// </summary>
        JoystickAxis41Positive,
        /// <summary>
        /// Joystick axis 42 positive button.
        /// </summary>
        JoystickAxis42Positive,
        /// <summary>
        /// Joystick axis 43 positive button.
        /// </summary>
        JoystickAxis43Positive,
        /// <summary>
        /// Joystick axis 44 positive button.
        /// </summary>
        JoystickAxis44Positive,
        /// <summary>
        /// Joystick axis 45 positive button.
        /// </summary>
        JoystickAxis45Positive,
        /// <summary>
        /// Joystick axis 46 positive button.
        /// </summary>
        JoystickAxis46Positive,
        /// <summary>
        /// Joystick axis 47 positive button.
        /// </summary>
        JoystickAxis47Positive,
        /// <summary>
        /// Joystick axis 48 positive button.
        /// </summary>
        JoystickAxis48Positive,
        /// <summary>
        /// Joystick axis 49 positive button.
        /// </summary>
        JoystickAxis49Positive,
        /// <summary>
        /// Joystick axis 50 positive button.
        /// </summary>
        JoystickAxis50Positive,
        /// <summary>
        /// Joystick axis 51 positive button.
        /// </summary>
        JoystickAxis51Positive,
        /// <summary>
        /// Joystick axis 52 positive button.
        /// </summary>
        JoystickAxis52Positive,
        /// <summary>
        /// Joystick axis 53 positive button.
        /// </summary>
        JoystickAxis53Positive,
        /// <summary>
        /// Joystick axis 54 positive button.
        /// </summary>
        JoystickAxis54Positive,
        /// <summary>
        /// Joystick axis 55 positive button.
        /// </summary>
        JoystickAxis55Positive,
        /// <summary>
        /// Joystick axis 56 positive button.
        /// </summary>
        JoystickAxis56Positive,
        /// <summary>
        /// Joystick axis 57 positive button.
        /// </summary>
        JoystickAxis57Positive,
        /// <summary>
        /// Joystick axis 58 positive button.
        /// </summary>
        JoystickAxis58Positive,
        /// <summary>
        /// Joystick axis 59 positive button.
        /// </summary>
        JoystickAxis59Positive,
        /// <summary>
        /// Joystick axis 60 positive button.
        /// </summary>
        JoystickAxis60Positive,
        /// <summary>
        /// Joystick axis 61 positive button.
        /// </summary>
        JoystickAxis61Positive,
        /// <summary>
        /// Joystick axis 62 positive button.
        /// </summary>
        JoystickAxis62Positive,
        /// <summary>
        /// Joystick axis 63 positive button.
        /// </summary>
        JoystickAxis63Positive,
        /// <summary>
        /// Joystick axis 64 positive button.
        /// </summary>
        JoystickAxis64Positive,

        /// <summary>
        /// Indicates the first available joystick hat up button.
        /// </summary>
        FirstJoystickHatUpButton = 4096,

        /// <summary>
        /// Joystick hat 1 up button.
        /// </summary>
        JoystickHat1Up,
        /// <summary>
        /// Joystick hat 2 up button.
        /// </summary>
        JoystickHat2Up,
        /// <summary>
        /// Joystick hat 3 up button.
        /// </summary>
        JoystickHat3Up,
        /// <summary>
        /// Joystick hat 4 up button.
        /// </summary>
        JoystickHat4Up,

        /// <summary>
        /// Indicates the first available joystick hat down button.
        /// </summary>
        FirstJoystickHatDownButton = 5120,

        /// <summary>
        /// Joystick hat 1 down button.
        /// </summary>
        JoystickHat1Down,
        /// <summary>
        /// Joystick hat 2 down button.
        /// </summary>
        JoystickHat2Down,
        /// <summary>
        /// Joystick hat 3 down button.
        /// </summary>
        JoystickHat3Down,
        /// <summary>
        /// Joystick hat 4 down button.
        /// </summary>
        JoystickHat4Down,

        /// <summary>
        /// Indicates the first available joystick hat left button.
        /// </summary>
        FirstJoystickHatLeftButton = 6144,

        /// <summary>
        /// Joystick hat 1 left button.
        /// </summary>
        JoystickHat1Left,
        /// <summary>
        /// Joystick hat 2 left button.
        /// </summary>
        JoystickHat2Left,
        /// <summary>
        /// Joystick hat 3 left button.
        /// </summary>
        JoystickHat3Left,
        /// <summary>
        /// Joystick hat 4 left button.
        /// </summary>
        JoystickHat4Left,

        /// <summary>
        /// Indicates the first available joystick hat right button.
        /// </summary>
        FirstJoystickHatRightButton = 7168,

        /// <summary>
        /// Joystick hat 1 right button.
        /// </summary>
        JoystickHat1Right,
        /// <summary>
        /// Joystick hat 2 right button.
        /// </summary>
        JoystickHat2Right,
        /// <summary>
        /// Joystick hat 3 right button.
        /// </summary>
        JoystickHat3Right,
        /// <summary>
        /// Joystick hat 4 right button.
        /// </summary>
        JoystickHat4Right,
    }
}
