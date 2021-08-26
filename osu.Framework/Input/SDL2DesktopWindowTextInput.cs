// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class SDL2DesktopWindowTextInput : ITextInputSource
    {
        private readonly SDL2DesktopWindow window;

        public event Action<string> OnTextInput;

        private bool active = false;

        public bool Active => active;

        public SDL2DesktopWindowTextInput(SDL2DesktopWindow window)
        {
            this.window = window;
            this.window.TextInput += HandleTextInput;
        }

        protected virtual void HandleTextInput(string text)
        {
            if (Active)
                OnTextInput?.Invoke(text);
        }

        public void Activate()
        {
            window.StartTextInput();
            active = true;
        }

        public void Deactivate()
        {
            window.StopTextInput();
            active = false;
        }

        public void EnsureActivated()
        { }
    }
}
