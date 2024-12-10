// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input;

namespace osu.Framework.Testing.Input
{
    public class ManualTextInputSource : TextInputSource
    {
        public readonly Queue<TextInputProperties> ActivationQueue = new Queue<TextInputProperties>();
        public readonly Queue<TextInputProperties> EnsureActivatedQueue = new Queue<TextInputProperties>();
        public readonly Queue<bool> DeactivationQueue = new Queue<bool>();

        public void Text(string text) => TriggerTextInput(text);

        public new void TriggerImeComposition(string text, int start, int length)
        {
            base.TriggerImeComposition(text, start, length);
        }

        public new void TriggerImeResult(string text)
        {
            base.TriggerImeResult(text);
        }

        public override void ResetIme()
        {
            base.ResetIme();

            // this call will be somewhat delayed in a real world scenario, but let's run it immediately for simplicity.
            base.TriggerImeComposition(string.Empty, 0, 0);
        }

        protected override void ActivateTextInput(TextInputProperties properties)
        {
            base.ActivateTextInput(properties);
            ActivationQueue.Enqueue(properties);
        }

        protected override void EnsureTextInputActivated(TextInputProperties properties)
        {
            base.EnsureTextInputActivated(properties);
            EnsureActivatedQueue.Enqueue(properties);
        }

        protected override void DeactivateTextInput()
        {
            base.DeactivateTextInput();
            DeactivationQueue.Enqueue(true);
        }
    }
}
