// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Testing;
using osu.Framework.Tests.Clocks;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Clocks
{
    public partial class TestSceneClocks : TestSceneClock
    {
        [Test]
        public void TestInterpolationWithLaggyClock()
        {
            AddStep("add stopwatch", () => AddClock(new StopwatchClock(true)));
            AddStep("add lag", () => AddClock(new LaggyFramedClock(50)));
            AddStep("add interpolating", () => AddClock(new InterpolatingFramedClock()));
        }

        [Test]
        public void TestDecouplingWithRangeLimited()
        {
            AddStep("add non-negative stopwatch", () => AddClock(new TestStopwatchClockWithRangeLimit()));
            AddStep("add decoupling", () => AddClock(new DecouplingFramedClock { AllowDecoupling = true }));

            AddStep("seek decoupling to -10000", () =>
            {
                foreach (var c in this.ChildrenOfType<VisualClock>())
                    (c.TrackingClock as DecouplingFramedClock)?.Seek(-10000);
            });

            AddStep("seek decoupling to 10000", () =>
            {
                foreach (var c in this.ChildrenOfType<VisualClock>())
                    (c.TrackingClock as DecouplingFramedClock)?.Seek(10000);
            });
        }
    }
}
