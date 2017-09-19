// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class RepeatStepButton : StepButton
    {
        private readonly int count;
        private int invocations;

        public override int RequiredRepetitions => count;

        public new Action Action;

        private string text;

        public new string Text
        {
            get { return text; }
            set { base.Text = text = value; }
        }

        public RepeatStepButton(int count = 1)
        {
            this.count = count;

            updateText();

            BackgroundColour = Color4.Sienna;

            base.Action = () =>
            {
                if (invocations == count) return;

                invocations++;

                if (invocations == count)
                    Success();

                updateText();

                Action?.Invoke();
            };
        }

        private void updateText()
        {
            base.Text = $@"{Text} {invocations}/{count}";
        }

        public override string ToString() => "Repeat: " + base.ToString();
    }
}