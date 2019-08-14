// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
            view.KeyDown += keyDown;
            view.KeyUp += keyUp;
        }

        public override bool IsActive => true;

        public override int Priority => 0;

        public override bool Initialize(GameHost host) => true;

        private void keyDown(Keycode keycode, KeyEvent e)
        {
            PendingInputs.Enqueue(new KeyboardKeyInput(GetKeyCodeAsKey(keycode), true));
        }

        private void keyUp(Keycode keycode, KeyEvent e)
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

                default:
                    if (Enum.TryParse(key, out Key result))
                        return result;
                    break;
            }

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
