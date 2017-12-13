// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class AssertButton : StepButton
    {
        public Func<bool> Assertion;
        public string ExtendedDescription;
        public StackTrace CallStack;

        private bool interactive;

        public AssertButton()
        {
            BackgroundColour = Color4.OrangeRed;
            Action += checkAssert;
        }

        [BackgroundDependencyLoader]
        private void load(GameHost host)
        {
            interactive = !(host is HeadlessGameHost);
        }

        private void checkAssert()
        {
            if (Assertion())
            {
                Success();
                BackgroundColour = Color4.YellowGreen;
            }
            else
            {
                if (interactive)
                    BackgroundColour = Color4.Red;
                else
                    throw new TracedException($"{Text} {ExtendedDescription}", CallStack);
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
