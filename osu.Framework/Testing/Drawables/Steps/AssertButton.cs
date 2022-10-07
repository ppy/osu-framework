// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using System.Text;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class AssertButton : StepButton
    {
        public Func<bool> Assertion;
        public string ExtendedDescription;
        public StackTrace CallStack;
        private readonly Func<string> getFailureMessage;

        public AssertButton(bool isSetupStep = false, Func<string> getFailureMessage = null)
            : base(isSetupStep)
        {
            this.getFailureMessage = getFailureMessage;
            Action += checkAssert;
            LightColour = Color4.OrangeRed;
        }

        private void checkAssert()
        {
            if (Assertion())
                Success();
            else
            {
                StringBuilder builder = new StringBuilder();

                builder.Append(Text);

                if (!string.IsNullOrEmpty(ExtendedDescription))
                    builder.Append($" {ExtendedDescription}");

                if (getFailureMessage != null)
                    builder.Append($": {getFailureMessage()}");

                throw new TracedException(builder.ToString(), CallStack);
            }
        }

        public override string ToString() => "Assert: " + base.ToString();

        private class TracedException : Exception
        {
            private readonly StackTrace trace;

            public TracedException(string description, StackTrace trace)
                : base(description)
            {
                this.trace = trace;
            }

            public override string StackTrace => trace.ToString();
        }
    }
}
