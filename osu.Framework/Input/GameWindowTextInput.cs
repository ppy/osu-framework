// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class GameWindowTextInput : ITextInputSource
    {
        private readonly GameWindow window;

        private string pending = string.Empty;

        public GameWindowTextInput(GameWindow window)
        {
            this.window = window;
        }

        private void window_KeyPress(object sender, OpenTK.KeyPressEventArgs e)
        {
            pending += e.KeyChar;
        }

        public bool ImeActive => false;

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

        public void Deactivate(object sender)
        {
            window.KeyPress -= window_KeyPress;
        }

        public void Activate(object sender)
        {
            window.KeyPress += window_KeyPress;
        }

        private void imeCompose()
        {
            //todo: implement
            OnNewImeComposition?.Invoke(string.Empty);
        }

        private void imeResult()
        {
            //todo: implement
            OnNewImeResult?.Invoke(string.Empty);
        }

        public event Action<string> OnNewImeComposition;
        public event Action<string> OnNewImeResult;
    }
}
