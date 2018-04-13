// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
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
            // Drop any keypresses if the control, alt, or windows/command key are being held.
            // This is a workaround for an issue on macOS where OpenTK will fire KeyPress events even
            // if modifier keys are held.  This can be reverted when it is fixed on OpenTK's side.
            if (RuntimeInfo.OS == RuntimeInfo.Platform.MacOsx)
            {
                var state = OpenTK.Input.Keyboard.GetState();
                if (state.IsKeyDown(OpenTK.Input.Key.LControl)
                    || state.IsKeyDown(OpenTK.Input.Key.RControl)
                    || state.IsKeyDown(OpenTK.Input.Key.LAlt)
                    || state.IsKeyDown(OpenTK.Input.Key.RAlt)
                    || state.IsKeyDown(OpenTK.Input.Key.LWin)
                    || state.IsKeyDown(OpenTK.Input.Key.RWin))
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
