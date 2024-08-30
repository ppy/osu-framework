// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Visual.Performance
{
    public sealed partial class TestSceneFastCircleBoxAlternatePerformance : RepeatedDrawablePerformanceTestScene
    {
        private int index;

        protected override Drawable CreateDrawable()
        {
            index++;
            if (index % 2 == 0)
                return new FastCircle();

            return new Box();
        }
    }
}
