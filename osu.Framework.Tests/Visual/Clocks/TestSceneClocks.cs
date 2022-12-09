// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Clocks
{
    public partial class TestSceneClocks : TestSceneClock
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("add stopwatch", () => AddClock(new StopwatchClock(true)));
            AddStep("add framed", () => AddClock(new FramedClock()));
            AddStep("add lag", () => AddClock(new LaggyFramedClock()));
            AddStep("add interpolating", () => AddClock(new InterpolatingFramedClock()));
            AddStep("add decoupled", () => AddClock(new DecoupleableInterpolatingFramedClock()));
        }
    }
}
