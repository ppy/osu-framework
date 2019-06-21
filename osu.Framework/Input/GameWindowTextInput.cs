// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class GameWindowTextInput : ITextInputSource
    {
        private readonly IWindow window;

        private string pending = string.Empty;

        public GameWindowTextInput(IWindow window)
        {
            this.window = window;
        }

        protected virtual void HandleKeyPress(object sender, osuTK.KeyPressEventArgs e) => pending += e.KeyChar;

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
            window.KeyPress -= HandleKeyPress;
        }

        public void Activate(object sender)
        {
            window.KeyPress += HandleKeyPress;
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
