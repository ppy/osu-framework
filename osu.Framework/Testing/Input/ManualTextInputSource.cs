// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input;

namespace osu.Framework.Testing.Input
{
    public class ManualTextInputSource : TextInputSource
    {
        public readonly Queue<bool> ActivationQueue = new Queue<bool>();
        public readonly Queue<bool> EnsureActivatedQueue = new Queue<bool>();
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

        protected override void ActivateTextInput(bool allowIme)
        {
            base.ActivateTextInput(allowIme);
            ActivationQueue.Enqueue(allowIme);
        }

        protected override void EnsureTextInputActivated(bool allowIme)
        {
            base.EnsureTextInputActivated(allowIme);
            EnsureActivatedQueue.Enqueue(allowIme);
        }

        protected override void DeactivateTextInput()
        {
            base.DeactivateTextInput();
            DeactivationQueue.Enqueue(true);
        }
    }
}
