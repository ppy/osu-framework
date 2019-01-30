// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        private void window_KeyPress(object sender, osuTK.KeyPressEventArgs e)
        {
            // Drop any keypresses if the control, alt, or windows/command key are being held.
            // This is a workaround for an issue on macOS where osuTK will fire KeyPress events even
            // if modifier keys are held.  This can be reverted when it is fixed on osuTK's side.
            if (RuntimeInfo.OS == RuntimeInfo.Platform.MacOsx)
            {
                var state = osuTK.Input.Keyboard.GetState();
                if (state.IsKeyDown(osuTK.Input.Key.LControl)
                    || state.IsKeyDown(osuTK.Input.Key.RControl)
                    || state.IsKeyDown(osuTK.Input.Key.LAlt)
                    || state.IsKeyDown(osuTK.Input.Key.RAlt)
                    || state.IsKeyDown(osuTK.Input.Key.LWin)
                    || state.IsKeyDown(osuTK.Input.Key.RWin))
                    return;
                // arbitrary choice here, but it caters for any non-printable keys on an A1243 Apple Keyboard
                if (e.KeyChar > 63000)
                    return;
            }
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
