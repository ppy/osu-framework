// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Android.Views;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Statistics;
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

        protected override bool OnKeyDown(Keycode keycode, KeyEvent e)
        {
            var key = GetKeyCodeAsKey(keycode);

            if (key != Key.Unknown)
            {
                enqueueInput(new KeyboardKeyInput(key, true));
                return true;
            }

            return false;
        }

        protected override bool OnKeyUp(Keycode keycode, KeyEvent e)
        {
            var key = GetKeyCodeAsKey(keycode);

            if (key != Key.Unknown)
            {
                enqueueInput(new KeyboardKeyInput(key, false));
                return true;
            }

            return false;
        }

        /// <summary>
        /// This method maps the <see cref="Xamarin.Android"/> <see cref="Keycode"/> to <see cref="Key"/> from opentk.
        /// </summary>
        /// <param name="keyCode">The <see cref="Keycode"/> to be converted into a <see cref="Key"/>.</param>
        /// <returns>The <see cref="Key"/> that was converted from <see cref="Keycode"/>.</returns>
        public static Key GetKeyCodeAsKey(Keycode keyCode)
        {
            // number keys
            const Keycode first_num_key = Keycode.Num0;
            const Keycode last_num_key = Keycode.Num9;
            if (keyCode >= first_num_key && keyCode <= last_num_key)
                return Key.Number0 + (keyCode - first_num_key);

            // letters
            const Keycode first_letter_key = Keycode.A;
            const Keycode last_letter_key = Keycode.Z;
            if (keyCode >= first_letter_key && keyCode <= last_letter_key)
                return Key.A + (keyCode - first_letter_key);

            // function keys
            const Keycode first_function_key = Keycode.F1;
            const Keycode last_function_key = Keycode.F12;
            if (keyCode >= first_function_key && keyCode <= last_function_key)
                return Key.F1 + (keyCode - first_function_key);

            // keypad keys
            const Keycode first_keypad_key = Keycode.Numpad0;
            const Keycode last_key_pad_key = Keycode.NumpadDot;
            if (keyCode >= first_keypad_key && keyCode <= last_key_pad_key)
                return Key.Keypad0 + (keyCode - first_keypad_key);

            // direction keys
            const Keycode first_direction_key = Keycode.DpadUp;
            const Keycode last_direction_key = Keycode.DpadRight;
            if (keyCode >= first_direction_key && keyCode <= last_direction_key)
                return Key.Up + (keyCode - first_direction_key);

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

                case Keycode.Backslash:
                case Keycode.Pound:
                    return Key.BackSlash; // english keyboard layout

                case Keycode.Del:
                    return Key.BackSpace;

                case Keycode.ForwardDel:
                    return Key.Delete;

                case Keycode.Power:
                    return Key.Sleep;

                case Keycode.MoveHome:
                    return Key.Home;

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

                case Keycode.NumpadEnter:
                    return Key.KeypadEnter;
            }

            if (Enum.TryParse(keyCode.ToString(), out Key key))
                return key;

            // this is the worst case scenario. Please note that the osu-framework keyboard handling cannot cope with Key.Unknown.
            return Key.Unknown;
        }

        private void enqueueInput(IInput input)
        {
            PendingInputs.Enqueue(input);
            FrameStatistics.Increment(StatisticsCounterType.KeyEvents);
        }
    }
}
