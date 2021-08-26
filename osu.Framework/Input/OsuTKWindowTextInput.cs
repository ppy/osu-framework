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

        private bool active = false;

        public bool Active => active;

        public OsuTKWindowTextInput(OsuTKWindow window)
        {
            this.window = window;
            this.window.KeyPress += HandleKeyPress;
        }

        protected virtual void HandleKeyPress(object sender, osuTK.KeyPressEventArgs e)
        {
            if (Active)
                OnTextInput.Invoke(e.KeyChar.ToString());
        }

        public void Deactivate() => active = true;

        public void Activate() => active = false;

        public void EnsureActivated()
        { }
    }
}
