// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Input
{
    public class SDL2DesktopWindowTextInput : ITextInputSource
    {
        private readonly SDL2DesktopWindow sdlWindow;

        public event Action<string> OnTextInput;

        public bool Active { get; private set; }

        public SDL2DesktopWindowTextInput(SDL2DesktopWindow window)
        {
            this.sdlWindow = window;
            this.sdlWindow.TextInput += HandleTextInput;
        }

        protected virtual void HandleTextInput(string text)
        {
            if (Active)
                OnTextInput?.Invoke(text);
        }

        public void Activate()
        {
            sdlWindow.StartTextInput();
            Active = true;
        }

        public void Deactivate()
        {
            sdlWindow.StopTextInput();
            Active = false;
        }

        public void EnsureActivated() => Activate();
    }
}
