// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Utils;
using CoreGraphics;
using Foundation;
using UIKit;
using osu.Framework.Graphics;

namespace osu.Framework.iOS
{
    internal class IOSKeyboardUtils : KeyboardUtils
    {
        public IOSKeyboardUtils()
        {
            UIKeyboard.Notifications.ObserveWillChangeFrame(keyboardWillShowChange);
            UIKeyboard.Notifications.ObserveWillShow(keyboardWillShowChange);
            UIKeyboard.Notifications.ObserveWillHide(keyboardWillHide);

            UIKeyboard.Notifications.ObserveDidChangeFrame(keyboardDidShowChange);
            UIKeyboard.Notifications.ObserveDidShow(keyboardDidShowChange);
            UIKeyboard.Notifications.ObserveDidHide(keyboardDidHide);
        }

        private Easing translateEasingEffect(UIViewAnimationCurve animationCurve)
        {
            switch (animationCurve)
            {
                case UIViewAnimationCurve.EaseInOut:
                    return Easing.InOutSine;

                case UIViewAnimationCurve.EaseIn:
                    return Easing.InSine;

                case UIViewAnimationCurve.EaseOut:
                    return Easing.OutSine;

                case UIViewAnimationCurve.Linear:
                    return Easing.None;
            }

            return Easing.None;
        }

        private void keyboardWillShowChange(object sender, UIKeyboardEventArgs args)
        {
            // Get ScreenBounds and keyboardFrame
            CGRect screenBounds = UIScreen.MainScreen.Bounds;
            CGRect keyboardFrame = ((NSValue)args.Notification.UserInfo![UIKeyboard.FrameEndUserInfoKey]!).CGRectValue;

            // Check if docked using a odd method
            // https://stackoverflow.com/questions/59871352/is-is-possible-on-ipad-os-to-detect-if-the-keyboard-is-in-floating-mode
            Docked = screenBounds.GetMaxX() == keyboardFrame.GetMaxX() &&
                screenBounds.GetMaxY() == keyboardFrame.GetMaxY() &&
                screenBounds.Width == keyboardFrame.Width;

            Visible = true;
            TrueHeight = args.FrameEnd.Height;
            Height = Docked ? args.FrameEnd.Height : 0;
            AnimationDuration = args.AnimationDuration;
            AnimationType = translateEasingEffect(args.AnimationCurve);
        }

        private void keyboardDidShowChange(object sender, UIKeyboardEventArgs args)
        {
            AnimationDuration = -1;
            AnimationType = Easing.InOutSine;
        }

        private void keyboardWillHide(object sender, UIKeyboardEventArgs args)
        {
            Docked = true;
            Visible = false;
            Height = 0;
            TrueHeight = 0;
            AnimationDuration = args.AnimationDuration;
            AnimationType = Easing.InOutSine;
        }

        private void keyboardDidHide(object sender, UIKeyboardEventArgs args)
        {
            AnimationDuration = -1;
            AnimationType = Easing.InOutSine;
        }
    }
}
