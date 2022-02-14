// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Android.Views;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK.Input;

namespace osu.Framework.Android.Input
{
    public class AndroidKeyboardHandler : AndroidInputHandler
    {
        protected override IEnumerable<InputSourceType> HandledEventSources => new[] { InputSourceType.Keyboard };

        public AndroidKeyboardHandler(AndroidGameView view)
            : base(view)
        {
        }

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            Enabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                {
                    View.KeyDown += HandleKeyDown;
                    View.KeyUp += HandleKeyUp;
                }
                else
                {
                    View.KeyDown -= HandleKeyDown;
                    View.KeyUp -= HandleKeyUp;
                }
            }, true);

            return true;
        }

        public override bool IsActive => true;

        protected override void OnKeyDown(Keycode keycode, KeyEvent e)
        {
            var key = GetKeyCodeAsKey(keycode);

            if (key != Key.Unknown)
                PendingInputs.Enqueue(new KeyboardKeyInput(key, true));
        }

        protected override void OnKeyUp(Keycode keycode, KeyEvent e)
        {
            var key = GetKeyCodeAsKey(keycode);

            if (key != Key.Unknown)
                PendingInputs.Enqueue(new KeyboardKeyInput(key, false));
        }

        /// <summary>
        /// This method maps the <see cref="Xamarin.Android"/> <see cref="Keycode"/> to <see cref="Key"/> from opentk.
        /// </summary>
        /// <param name="keyCode">The <see cref="Keycode"/> to be converted into a <see cref="Key"/>.</param>
        /// <returns>The <see cref="Key"/> that was converted from <see cref="Keycode"/>.</returns>
        public static Key GetKeyCodeAsKey(Keycode keyCode)
        {
            int code = (int)keyCode;

            // number keys
            const int first_num_key = (int)Keycode.Num0;
            const int last_num_key = (int)Keycode.Num9;
            if (code >= first_num_key && code <= last_num_key)
                return Key.Number0 + code - first_num_key;

            // letters
            const int first_letter_key = (int)Keycode.A;
            const int last_letter_key = (int)Keycode.Z;
            if (code >= first_letter_key && code <= last_letter_key)
                return Key.A + code - first_letter_key;

            // function keys
            const int first_funtion_key = (int)Keycode.F1;
            const int last_function_key = (int)Keycode.F12;
            if (code >= first_funtion_key && code <= last_function_key)
                return Key.F1 + code - first_funtion_key;

            // keypad keys
            const int first_keypad_key = (int)Keycode.Numpad0;
            const int last_key_pad_key = (int)Keycode.NumpadDot;
            if (code >= first_keypad_key && code <= last_key_pad_key)
                return Key.Keypad0 + code - first_keypad_key;

            // direction keys
            const int first_direction_key = (int)Keycode.DpadUp;
            const int last_direction_key = (int)Keycode.DpadRight;
            if (code >= first_direction_key && code <= last_direction_key)
                return Key.Up + code - first_direction_key;

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

            // this is the worst case scenario. Please note that the osu-framework keyboard handling cannot cope with Key.Unknown.
            return Key.Unknown;
        }
    }
}
