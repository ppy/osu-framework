// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osuTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class AssertButton : StepButton
    {
        public Func<bool> Assertion;
        public string ExtendedDescription;
        public StackTrace CallStack;

        public AssertButton(bool isSetupStep = false)
            : base(isSetupStep)
        {
            Action += checkAssert;
            LightColour = Color4.OrangeRed;
        }

        private void checkAssert()
        {
            if (Assertion())
                Success();
            else
                throw new TracedException($"{Text} {ExtendedDescription}", CallStack);
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
