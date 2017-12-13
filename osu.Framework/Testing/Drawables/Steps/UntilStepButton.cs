// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class UntilStepButton : StepButton
    {
        private bool success;

        private int invocations;

        private const int max_tries = 50;

        public override int RequiredRepetitions => success ? 0 : max_tries;

        public new Action Action;

        private string text;

        public new string Text
        {
            get { return text; }
            set { base.Text = text = value; }
        }

        public UntilStepButton(Func<bool> waitUntilTrueDelegate)
        {
            updateText();
            BackgroundColour = Color4.Sienna;

            base.Action = () =>
            {
                invocations++;

                updateText();

                if (waitUntilTrueDelegate())
                {
                    success = true;
                    Success();
                }
                else if (invocations == max_tries)
                    throw new TimeoutException();

                Action?.Invoke();
            };
        }

        protected override void Success()
        {
            base.Success();
            BackgroundColour = Color4.YellowGreen;
        }

        protected override void Failure()
        {
            base.Failure();
            BackgroundColour = Color4.Red;
        }

        private void updateText() => base.Text = $@"{Text} ({invocations} tries)";

        public override string ToString() => "Repeat: " + base.ToString();
    }
}
