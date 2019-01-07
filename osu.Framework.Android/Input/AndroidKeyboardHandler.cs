// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Android.Views;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osuTK.Input;
using System;
using System.Linq;

namespace osu.Framework.Android.Input
{
    public class AndroidKeyboardHandler : InputHandler
    {
        private readonly AndroidGameView view;

        public AndroidKeyboardHandler(AndroidGameView view)
        {
            this.view = view;
        }

        public override bool IsActive
            => true;

        public override int Priority
            => 0;

        public override bool Initialize(GameHost host)
        {
            view.KeyDown += keyDown;

            return true;
        }

        private void keyDown(Keycode keycode)
        {
            PendingInputs.Enqueue(new KeyboardKeyInput(GetKeyCodeAsKey(keycode), true));
        }

        private void keyUp(Keycode keycode)
        {
            PendingInputs.Enqueue(new KeyboardKeyInput(GetKeyCodeAsKey(keycode), false));
        }

        public static Key GetKeyCodeAsKey(Keycode keycode)
        {
            string key = keycode.ToString();

            if (key.StartsWith(Keycode.Num.ToString()))
                key = "Number" + key.Last();

            switch (keycode)
            {
                case Keycode.Back:
                    return Key.Escape;
                case Keycode.Del:
                    return Key.Back;
                case Keycode.ShiftLeft:
                    return Key.ShiftLeft;
                case Keycode.ShiftRight:
                    return Key.ShiftRight;
                case Keycode.LeftBracket:
                    return Key.BracketLeft;
                case Keycode.RightBracket:
                    return Key.BracketRight;
                case Keycode.Backslash:
                    return Key.BackSlash;
                case Keycode.DpadDown:
                    return Key.Down;
                case Keycode.DpadUp:
                    return Key.Up;
                case Keycode.DpadLeft:
                    return Key.Left;
                case Keycode.DpadRight:
                    return Key.Right;
                case Keycode.CtrlLeft:
                    return Key.ControlLeft;
                case Keycode.CtrlRight:
                    return Key.ControlRight;
                default:
                    if (Enum.TryParse(key, out Key result))
                        return result;
                    break;
            }

            return Key.Unknown;
        }
    }
}
