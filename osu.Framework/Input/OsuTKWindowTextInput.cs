// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class OsuTKWindowTextInput : ITextInputSource
    {
        private readonly OsuTKWindow window;

        public event Action<string> OnTextInput;

        public bool Active { get; private set; }

        public OsuTKWindowTextInput(OsuTKWindow window)
        {
            this.window = window;
        }

        protected virtual void HandleKeyPress(object sender, osuTK.KeyPressEventArgs e)
        {
            if (Active)
                OnTextInput?.Invoke(e.KeyChar.ToString());
        }

        public void Activate()
        {
            if (Active)
                return;

            window.KeyPress += HandleKeyPress;
            Active = true;
        }

        public void Deactivate()
        {
            if (!Active)
                return;

            window.KeyPress -= HandleKeyPress;
            Active = false;
        }

        public void EnsureActivated() => Activate();
    }
}
