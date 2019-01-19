// Copyright (c) 2007-2019 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseFTB : TestCase
    {
        public TestCaseFTB()
        {
            for (int i = 0; i < 1000; i++)
            {
                Add(new Box { RelativeSizeAxes = Axes.Both });
            }
        }
    }
}
