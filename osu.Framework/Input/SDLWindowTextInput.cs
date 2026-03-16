// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Platform;
using osu.Framework.Logging;

namespace osu.Framework.Input
{
    internal class SDLWindowTextInput : TextInputSource
    {
        private readonly ISDLWindow window;

        public SDLWindowTextInput(ISDLWindow window)
        {
            this.window = window;
        }

        /// <summary>
        /// Whether a composition was active and just ended via an empty <see cref="handleTextEditing"/> event.
        /// Some IMEs (e.g. Korean) commit the final character via <c>SDL_TextInputEvent</c> with
        /// <see cref="TextInputSource.ImeActive"/> already cleared, so we track this to route that
        /// <c>SDL_TextInputEvent</c> as an IME result rather than regular text input.
        /// </summary>
        private bool pendingImeCommit;

        private void handleTextInput(string text)
        {
            // SDL sends IME results as `SDL_TextInputEvent` which we can't differentiate from regular text input
            // so we have to manually keep track and invoke the correct event.

            if (ImeActive || pendingImeCommit)
            {
                pendingImeCommit = false;
                Logger.Log($"[SDLWindowTextInput] handleTextInput → routing as ImeResult: \"{text}\"", LoggingTarget.Runtime, LogLevel.Debug);
                TriggerImeResult(text);
            }
            else
            {
                Logger.Log($"[SDLWindowTextInput] handleTextInput → ImeActive=false, routing as TextInput: \"{text}\"", LoggingTarget.Runtime, LogLevel.Debug);
                TriggerTextInput(text);
            }
        }

        private void handleTextEditing(string? text, int selectionStart, int selectionLength)
        {
            if (text == null) return;

            bool wasActive = ImeActive;
            // Some IMEs (e.g. kime) can send negative selection values; clamp to safe range.
            if (selectionStart < 0 || selectionLength < 0)
                Logger.Log($"[SDLWindowTextInput] handleTextEditing: clamping negative selection values (start={selectionStart} length={selectionLength})", LoggingTarget.Runtime, LogLevel.Debug);
            selectionStart = Math.Max(0, selectionStart);
            selectionLength = Math.Max(0, selectionLength);
            Logger.Log($"[SDLWindowTextInput] handleTextEditing: text=\"{text}\" selectionStart={selectionStart} selectionLength={selectionLength}", LoggingTarget.Runtime, LogLevel.Debug);
            TriggerImeComposition(text, selectionStart, selectionLength);

            // If a composition was active and just ended (empty text), the immediately following
            // TextInput is the committed character from the IME, not regular user text input.
            // Any subsequent TextEditing event (new composition or otherwise) clears this flag.
            pendingImeCommit = wasActive && string.IsNullOrEmpty(text);
        }

        protected override void ActivateTextInput(TextInputProperties properties)
        {
            window.TextInput += handleTextInput;
            window.TextEditing += handleTextEditing;
            window.StartTextInput(properties);
        }

        protected override void EnsureTextInputActivated(TextInputProperties properties)
        {
            window.StartTextInput(properties);
        }

        protected override void DeactivateTextInput()
        {
            window.TextInput -= handleTextInput;
            window.TextEditing -= handleTextEditing;
            window.StopTextInput();
        }

        public override void SetImeRectangle(RectangleF rectangle)
        {
            window.SetTextInputRect(rectangle);
        }

        public override void ResetIme()
        {
            Logger.Log("[SDLWindowTextInput] ResetIme() → calling base + window.ResetIme()", LoggingTarget.Runtime, LogLevel.Debug);
            base.ResetIme();
            window.ResetIme();
        }
    }
}
