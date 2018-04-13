// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class RepeatStepButton : StepButton
    {
        private readonly int count;
        private int invocations;

        public override int RequiredRepetitions => count;

        private string text;

        public new string Text
        {
            get { return text; }
            set { base.Text = text = value; }
        }

        public RepeatStepButton(Action action, int count = 1)
        {
            this.count = count;
            Action = action;

            updateText();
        }

        public override void PerformStep(bool userTriggered = false)
        {
            if (invocations == count && !userTriggered) throw new InvalidOperationException("Repeat step was invoked too many times");

            invocations++;

            base.PerformStep(userTriggered);

            if (invocations >= count) // Allows for manual execution beyond the invocation limit.
                Success();

            updateText();
        }

        public override void Reset()
        {
            base.Reset();

            invocations = 0;
            updateText();
        }

        private void updateText() => base.Text = $@"{Text} {invocations}/{count}";

        public override string ToString() => "Repeat: " + base.ToString();
    }
}
