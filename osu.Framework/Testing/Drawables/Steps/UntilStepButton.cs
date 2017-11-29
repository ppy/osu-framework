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

        public override int RequiredRepetitions => success ? 0 : int.MaxValue;

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

                if (waitUntilTrueDelegate())
                {
                    success = true;
                    Success();
                }

                updateText();

                Action?.Invoke();
            };
        }

        private void updateText() => base.Text = $@"{Text} {invocations}";

        public override string ToString() => "Repeat: " + base.ToString();
    }
}
