// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Testing.Drawables.Steps
{
    public partial class RepeatStepButton : StepButton
    {
        public int Count { get; init; } = 1;

        public override int RequiredRepetitions => Count;

        private readonly string text = string.Empty;
        private int invocations;

        public RepeatStepButton()
        {
            updateText();
        }

        public new string Text
        {
            get => text;
            init => base.Text = text = value;
        }

        public override void PerformStep(bool userTriggered = false)
        {
            if (invocations == Count && !userTriggered) throw new InvalidOperationException("Repeat step was invoked too many times");

            invocations++;

            base.PerformStep(userTriggered);

            if (invocations >= Count) // Allows for manual execution beyond the invocation limit.
                Success();

            updateText();
        }

        public override void Reset()
        {
            base.Reset();

            invocations = 0;
            updateText();
        }

        private void updateText() => base.Text = $@"{Text} {invocations}/{Count}";

        public override string ToString() => "Repeat: " + base.ToString();
    }
}
