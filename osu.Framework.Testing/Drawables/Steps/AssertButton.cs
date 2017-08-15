// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using OpenTK.Graphics;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class AssertButton : StepButton
    {
        public Func<bool> Assertion;

        public string ExtendedDescription;

        public AssertButton()
        {
            BackgroundColour = Color4.OrangeRed;
            Action += checkAssert;
        }

        private void checkAssert()
        {
            if (Assertion())
            {
                Success();
                BackgroundColour = Color4.YellowGreen;
            }
            else
                throw new Exception($"{Text} {ExtendedDescription}");
        }

        public override string ToString() => "Assert: " + base.ToString();
    }
}