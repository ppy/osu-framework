// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class AssertButton : StepButton
    {
        public Func<bool> Assertion;
        public string ExtendedDescription;
        public StackTrace CallStack;

        public AssertButton()
        {
            BackgroundColour = Color4.OrangeRed;
            Action += checkAssert;
        }

        private void checkAssert()
        {
            if (Assertion())
                Success();
            else
                throw new TracedException($"{Text} {ExtendedDescription}", CallStack);
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
