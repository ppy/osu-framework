// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Testing;
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
            AddStep("add non-negative stopwatch", () => AddClock(new TestClockWithRangeLimit()));
            AddStep("add decoupling", () => AddClock(new DecouplingClock { AllowDecoupling = true }));

            AddStep("seek decoupling to -10000", () =>
            {
                foreach (var c in this.ChildrenOfType<VisualClock>())
                    (c.TrackingClock as DecouplingClock)?.Seek(-10000);
            });

            AddStep("seek decoupling to 10000", () =>
            {
                foreach (var c in this.ChildrenOfType<VisualClock>())
                    (c.TrackingClock as DecouplingClock)?.Seek(10000);
            });
        }

        internal class TestClockWithRangeLimit : StopwatchClock
        {
            public double MinTime => 0;
            public double MaxTime { get; set; } = double.PositiveInfinity;

            public TestClockWithRangeLimit()
                : base(true)
            {
            }

            public override bool Seek(double position)
            {
                double clamped = Math.Clamp(position, MinTime, MaxTime);

                if (clamped != position)
                {
                    // Emulate what bass will probably do in this case.
                    // TODO: confirm.
                    Stop();
                    Seek(clamped);
                    return false;
                }

                return base.Seek(position);
            }
        }
    }
}
