// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Android.Content;
using Android.Views;
using Android.Views.InputMethods;
using osu.Framework.Input;
using osuTK.Input;
using System;

namespace osu.Framework.Android.Input
{
    public class AndroidTextInput : ITextInputSource
    {
        private readonly AndroidGameView view;
        private readonly InputMethodManager inputMethodManager;
        private string pending = string.Empty;

        public AndroidTextInput(AndroidGameView view)
        {
            this.view = view;

            inputMethodManager = view.Context.GetSystemService(Context.InputMethodService) as InputMethodManager;
        }

        private void keyDown(Keycode arg)
        {
            Key key = AndroidKeyboardHandler.GetKeyCodeAsKey(arg);

            pending += key.ToString();
        }

        public bool ImeActive
            => false;

        public event Action<string> OnNewImeComposition;
        public event Action<string> OnNewImeResult;

        public void Activate(object sender)
        {
            inputMethodManager.ToggleSoftInput(ShowFlags.Forced, HideSoftInputFlags.None);
            view.KeyDown += keyDown;
        }

        public void Deactivate(object sender)
        {
            inputMethodManager.HideSoftInputFromWindow(view.WindowToken, HideSoftInputFlags.None);
            view.KeyDown -= keyDown;
        }

        public string GetPendingText()
        {
            try
            {
                return pending;
            }
            finally
            {
                pending = string.Empty;
            }
        }
    }
}
