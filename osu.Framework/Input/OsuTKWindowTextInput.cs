// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class OsuTKWindowTextInput : ITextInputSource
    {
        private readonly OsuTKWindow window;
        private string pending = string.Empty;

        public OsuTKWindowTextInput(OsuTKWindow window)
        {
            this.window = window;
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

        protected virtual void HandleKeyPress(object sender, osuTK.KeyPressEventArgs e) => pending += e.KeyChar;

        public void Activate()
        {
            window.KeyPress += HandleKeyPress;
        }

        public void EnsureActivated()
        {
        }

        public void Deactivate()
        {
            window.KeyPress -= HandleKeyPress;
        }

        public event Action<string> OnNewImeComposition { add { } remove { } }
        public event Action<string> OnNewImeResult { add { } remove { } }
    }
}
