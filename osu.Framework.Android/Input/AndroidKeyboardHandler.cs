// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Views;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK.Input;
using System;

namespace osu.Framework.Android.Input
{
    public class AndroidKeyboardHandler : InputHandler
    {
        private readonly AndroidGameView view;

        public AndroidKeyboardHandler(AndroidGameView view)
        {
            this.view = view;
            view.KeyDown += keyDown;
            view.KeyUp += keyUp;
        }

        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host) => true;

        private void keyDown(Keycode keycode, KeyEvent e)
        {
            var key = GetKeyCodeAsKey(keycode);

            if (key != Key.Unknown)
                PendingInputs.Enqueue(new KeyboardKeyInput(key, true));
        }

        private void keyUp(Keycode keycode, KeyEvent e)
        {
            var key = GetKeyCodeAsKey(keycode);

            if (key != Key.Unknown)
                PendingInputs.Enqueue(new KeyboardKeyInput(key, false));
        }

        /// <summary>
        /// This method maps the <see cref="Xamarin.Android"/> <see cref="Keycode"/> to <see cref="Key"/> from opentk.
        /// </summary>
        /// <param name="code">The <see cref="Keycode"/> to be converted into a <see cref="Key"/>.</param>
        /// <returns>The <see cref="Key"/> that was converted from <see cref="Keycode"/>.</returns>
        public static Key GetKeyCodeAsKey(Keycode keyCode)
        {
            int code = (int)keyCode;

            // number keys
            int firstNumKey = (int)Keycode.Num0;
            int lastNumKey = (int)Keycode.Num9;
            if (code >= firstNumKey && code <= lastNumKey)
                return Key.Number0 + code - firstNumKey;

            // letters
            int firstLetterKey = (int)Keycode.A;
            int lastLetterKey = (int)Keycode.Z;
            if (code >= firstLetterKey && code <= lastLetterKey)
                return Key.A + code - firstLetterKey;

            // function keys
            int firstFuntionKey = (int)Keycode.F1;
            int lastFunctionKey = (int)Keycode.F12;
            if (code >= firstFuntionKey && code <= lastFunctionKey)
                return Key.F1 + code - firstFuntionKey;

            // keypad keys
            int firstKeypadKey = (int)Keycode.Numpad0;
            int lastKeyPadKey = (int)Keycode.NumpadDot;
            if (code >= firstKeypadKey && code <= lastKeyPadKey)
                return Key.Keypad0 + code - firstKeypadKey;

            // direction keys
            int firstDirectionKey = (int)Keycode.DpadUp;
            int lastDirectionKey = (int)Keycode.DpadRight;
            if (code >= firstDirectionKey && code <= lastDirectionKey)
                return Key.Up + code - firstDirectionKey;

            // one to one mappings
            switch (keyCode)
            {
                case Keycode.Back:
                    return Key.Escape;
                case Keycode.MediaPlayPause:
                    return Key.PlayPause;
                case Keycode.SoftLeft:
                    return Key.Left;
                case Keycode.SoftRight:
                    return Key.Right;
                case Keycode.Star:
                    return Key.KeypadMultiply;
                case Keycode.Pound:
                    return Key.BackSlash; // english keyboard layout
                case Keycode.Del:
                    return Key.BackSpace;
                case Keycode.ForwardDel:
                    return Key.Delete;
                case Keycode.Power:
                    return Key.Sleep;
                case Keycode.MoveEnd:
                    return Key.End;
                case Keycode.MediaPause:
                    return Key.Pause;
                case Keycode.MediaClose:
                    return Key.Stop;
                case Keycode.LeftBracket:
                    return Key.BracketLeft;
                case Keycode.RightBracket:
                    return Key.BracketRight;
                case Keycode.MediaPrevious:
                    return Key.TrackPrevious;
                case Keycode.MediaNext:
                    return Key.TrackNext;
                case Keycode.CtrlLeft:
                    return Key.ControlLeft;
                case Keycode.CtrlRight:
                    return Key.ControlRight;
                case Keycode.MetaLeft:
                    return Key.WinLeft;
                case Keycode.MetaRight:
                    return Key.WinRight;
                case Keycode.Equals:
                    return Key.Plus;
                case Keycode.At:
                case Keycode.Apostrophe:
                    return Key.Quote;
            }

            if (Enum.TryParse(keyCode.ToString(), out Key key))
                return key;

            // this is the worst case senario. Please note that the osu-framework keyboard handling cannot cope with Key.Unknown.
            return Key.Unknown;
        }

        protected override void Dispose(bool disposing)
        {
            view.KeyDown -= keyDown;
            view.KeyUp -= keyUp;
            base.Dispose(disposing);
        }
    }
}
