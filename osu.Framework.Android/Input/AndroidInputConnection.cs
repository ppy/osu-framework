// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Views;
using Android.Views.InputMethods;
using Java.Lang;

namespace osu.Framework.Android.Input
{
    internal class AndroidInputConnection : BaseInputConnection
    {
        public AndroidGameView TargetView { get; set; }

        public AndroidInputConnection(AndroidGameView targetView, bool fullEditor)
            : base(targetView, fullEditor)
        {
            TargetView = targetView;
        }

        public override bool CommitText(ICharSequence text, int newCursorPosition)
        {
            if (text.Length() != 0)
            {
                TargetView.OnCommitText(text.ToString());
                return true;
            }

            return base.CommitText(text, newCursorPosition);
        }

        public override bool SendKeyEvent(KeyEvent e)
        {
            switch (e.Action)
            {
                case KeyEventActions.Down:
                    TargetView?.OnKeyDown(e.KeyCode, e);
                    return true;

                case KeyEventActions.Up:
                    TargetView?.OnKeyUp(e.KeyCode, e);
                    return true;

                case KeyEventActions.Multiple:
                    TargetView?.OnKeyDown(e.KeyCode, e);
                    TargetView?.OnKeyUp(e.KeyCode, e);
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
