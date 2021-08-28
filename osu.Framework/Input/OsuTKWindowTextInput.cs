// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class OsuTKWindowTextInput : ITextInputSource
    {
        public event Action<string> OnTextInput;

        public bool Active { get; private set; }

        public OsuTKWindowTextInput(OsuTKWindow window)
        {
            window.KeyPress += HandleKeyPress;
        }

        protected virtual void HandleKeyPress(object sender, osuTK.KeyPressEventArgs e)
        {
            if (Active)
                OnTextInput?.Invoke(e.KeyChar.ToString());
        }

        public void Activate() => Active = true;

        public void Deactivate() => Active = false;

        public void EnsureActivated()
        {
        }
    }
}
