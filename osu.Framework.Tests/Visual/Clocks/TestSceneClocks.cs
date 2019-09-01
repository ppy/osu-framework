// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Clocks
{
    public class TestSceneClocks : TestSceneClock
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            Steps.AddStep("add stopwatch", () => AddClock(new StopwatchClock(true)));
            Steps.AddStep("add framed", () => AddClock(new FramedClock()));
            Steps.AddStep("add lag", () => AddClock(new LaggyFramedClock()));
            Steps.AddStep("add interpolating", () => AddClock(new InterpolatingFramedClock()));
            Steps.AddStep("add decoupled", () => AddClock(new DecoupleableInterpolatingFramedClock()));
        }
    }
}
