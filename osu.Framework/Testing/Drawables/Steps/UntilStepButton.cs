// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Text;
using osu.Framework.Graphics;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class UntilStepButton : StepButton
    {
        private bool success;

        private int invocations;

        private static readonly int max_attempt_milliseconds = Environment.GetEnvironmentVariable("OSU_TESTS_NO_TIMEOUT") == "1" ? int.MaxValue : 10000;

        public override int RequiredRepetitions => success ? 0 : int.MaxValue;

        public new Action Action;

        private string text;

        public new string Text
        {
            get => text;
            set => base.Text = text = value;
        }

        private Stopwatch elapsedTime;

        public UntilStepButton(Func<bool> waitUntilTrueDelegate, bool isSetupStep = false, Func<string> getFailureMessage = null)
            : base(isSetupStep)
        {
            updateText();
            LightColour = Color4.Sienna;

            base.Action = () =>
            {
                invocations++;

                elapsedTime ??= Stopwatch.StartNew();

                updateText();

                if (waitUntilTrueDelegate())
                {
                    elapsedTime = null;
                    success = true;
                    Success();
                }
                else if (!Debugger.IsAttached && elapsedTime.ElapsedMilliseconds >= max_attempt_milliseconds)
                {
                    StringBuilder builder = new StringBuilder();

                    builder.Append($"\"{Text}\" timed out");

                    if (getFailureMessage != null)
                        builder.Append($": {getFailureMessage()}");

                    throw new TimeoutException(builder.ToString());
                }

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

        public override string ToString() => "Until: " + base.ToString();
    }
}
