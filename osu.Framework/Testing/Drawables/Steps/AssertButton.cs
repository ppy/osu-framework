// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
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
