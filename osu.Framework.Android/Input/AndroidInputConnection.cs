// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Views;
using Android.Views.InputMethods;
using Java.Lang;

namespace osu.Framework.Android.Input
{
    internal class AndroidInputConnection : BaseInputConnection
    {
        private readonly AndroidGameView targetView;

        public AndroidInputConnection(AndroidGameView targetView, bool fullEditor)
            : base(targetView, fullEditor)
        {
            this.targetView = targetView;
        }

        public override bool CommitText(ICharSequence? text, int newCursorPosition)
        {
            if (text?.Length() > 0)
            {
                targetView.OnCommitText(text.ToString());
                return true;
            }

            return base.CommitText(text, newCursorPosition);
        }

        public override bool SendKeyEvent(KeyEvent? e)
        {
            if (e == null)
                return base.SendKeyEvent(e);

            switch (e.Action)
            {
                case KeyEventActions.Down:
                    targetView.OnKeyDown(e.KeyCode, e);
                    return true;

                case KeyEventActions.Up:
                    targetView.OnKeyUp(e.KeyCode, e);
                    return true;

                case KeyEventActions.Multiple:
                    targetView.OnKeyDown(e.KeyCode, e);
                    targetView.OnKeyUp(e.KeyCode, e);
                    return true;
            }

            return base.SendKeyEvent(e);
        }

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            for (int i = 0; i < beforeLength; i++)
            {
                KeyEvent ed = new KeyEvent(KeyEventActions.Multiple, Keycode.Del);
                SendKeyEvent(ed);
            }

            return true;
        }
    }
}
