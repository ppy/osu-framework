// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Graphics;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class UntilStepButton : StepButton
    {
        private bool success;

        private int invocations;

        private const int max_attempt_milliseconds = 10000;

        public override int RequiredRepetitions => success ? 0 : int.MaxValue;

        public new Action Action;

        private string text;

        public new string Text
        {
            get { return text; }
            set { base.Text = text = value; }
        }

        private Stopwatch elapsedTime;

        public UntilStepButton(Func<bool> waitUntilTrueDelegate)
        {
            updateText();
            LightColour = Color4.Sienna;

            base.Action = () =>
            {
                invocations++;

                if (elapsedTime == null)
                    elapsedTime = Stopwatch.StartNew();

                updateText();

                if (waitUntilTrueDelegate())
                {
                    elapsedTime = null;
                    success = true;
                    Success();
                }
                else if (elapsedTime.ElapsedMilliseconds >= max_attempt_milliseconds)
                    throw new TimeoutException();

                Action?.Invoke();
            };
        }

        public override void Reset()
        {
            base.Reset();

            invocations = 0;
            elapsedTime = null;
            success = false;
        }

        protected override void Success()
        {
            base.Success();
            Light.FadeColour(Color4.YellowGreen);
        }

        protected override void Failure()
        {
            base.Failure();
            Light.FadeColour(Color4.Red);
        }

        private void updateText() => base.Text = $@"{Text} ({invocations} tries)";

        public override string ToString() => "Repeat: " + base.ToString();
    }
}
