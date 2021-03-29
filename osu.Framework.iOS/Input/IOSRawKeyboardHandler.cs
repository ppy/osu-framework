// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK.Input;
using UIKit;

namespace osu.Framework.iOS.Input
{
    public class IOSRawKeyboardHandler : InputHandler
    {
        internal bool KeyboardActive = true;
        public override bool IsActive => KeyboardActive;

        public override bool Initialize(GameHost host)
        {
            if (!(UIApplication.SharedApplication is GameUIApplication game))
                return false;

            game.KeyEvent += (keyCode, isDown) =>
            {
                if (IsActive && keyMap.ContainsKey(keyCode))
                    PendingInputs.Enqueue(new KeyboardKeyInput(keyMap[keyCode], isDown));
            };

            return true;
        }

        private readonly Dictionary<int, Key> keyMap = new Dictionary<int, Key>
        {
            { 4, Key.A },
            { 5, Key.B },
            { 6, Key.C },
            { 7, Key.D },
            { 8, Key.E },
            { 9, Key.F },
            { 10, Key.G },
            { 11, Key.H },
            { 12, Key.I },
            { 13, Key.J },
            { 14, Key.K },
            { 15, Key.L },
            { 16, Key.M },
            { 17, Key.N },
            { 18, Key.O },
            { 19, Key.P },
            { 20, Key.Q },
            { 21, Key.R },
            { 22, Key.S },
            { 23, Key.T },
            { 24, Key.U },
            { 25, Key.V },
            { 26, Key.W },
            { 27, Key.X },
            { 28, Key.Y },
            { 29, Key.Z },
            { 30, Key.Number1 },
            { 31, Key.Number2 },
            { 32, Key.Number3 },
            { 33, Key.Number4 },
            { 34, Key.Number5 },
            { 35, Key.Number6 },
            { 36, Key.Number7 },
            { 37, Key.Number8 },
            { 38, Key.Number9 },
            { 39, Key.Number0 },
            { 40, Key.Enter },
            { 41, Key.Escape },
            { 42, Key.BackSpace },
            { 43, Key.Tab },
            { 44, Key.Space },
            { 45, Key.Minus },
            { 46, Key.Plus },
            { 47, Key.BracketLeft },
            { 48, Key.BracketRight },
            { 49, Key.BackSlash },
            { 51, Key.Semicolon },
            { 52, Key.Quote },
            { 53, Key.Grave },
            { 54, Key.Comma },
            { 55, Key.Period },
            { 56, Key.Slash },
            { 57, Key.CapsLock },
            { 58, Key.F1 },
            { 59, Key.F2 },
            { 60, Key.F3 },
            { 61, Key.F4 },
            { 62, Key.F5 },
            { 63, Key.F6 },
            { 64, Key.F7 },
            { 65, Key.F8 },
            { 66, Key.F9 },
            { 67, Key.F10 },
            { 68, Key.F11 },
            { 69, Key.F12 },
            { 74, Key.Home },
            { 75, Key.PageUp },
            { 76, Key.Delete },
            { 77, Key.End },
            { 78, Key.PageDown },
            { 79, Key.Right },
            { 80, Key.Left },
            { 81, Key.Down },
            { 82, Key.Up },
            { 83, Key.NumLock },
            { 84, Key.KeypadDivide },
            { 85, Key.KeypadMultiply },
            { 86, Key.KeypadMinus },
            { 87, Key.KeypadPlus },
            { 89, Key.Keypad1 },
            { 90, Key.Keypad2 },
            { 91, Key.Keypad3 },
            { 92, Key.Keypad4 },
            { 93, Key.Keypad5 },
            { 94, Key.Keypad6 },
            { 95, Key.Keypad7 },
            { 96, Key.Keypad8 },
            { 97, Key.Keypad9 },
            { 98, Key.Keypad0 },
            { 99, Key.KeypadDecimal },
            { 101, Key.Menu },
            { 104, Key.PrintScreen },
            { 105, Key.ScrollLock },
            { 106, Key.Pause },
            { 117, Key.Insert },
            { 224, Key.ControlLeft },
            { 225, Key.ShiftLeft },
            { 226, Key.AltLeft },
            { 227, Key.WinLeft },
            { 228, Key.ControlRight },
            { 229, Key.ShiftRight },
            { 230, Key.AltRight },
            { 231, Key.WinRight }
        };
    }
}
