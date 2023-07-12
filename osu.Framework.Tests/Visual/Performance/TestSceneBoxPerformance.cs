// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Tests.Visual.Performance
{
    public sealed partial class TestSceneBoxPerformance : RepeatedDrawablePerformanceTestScene
    {
        protected override Drawable CreateDrawable() => new Box();
    }
}
