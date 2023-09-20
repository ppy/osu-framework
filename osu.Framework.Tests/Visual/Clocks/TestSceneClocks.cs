// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Testing;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Visual.Clocks
{
    public partial class TestSceneClocks : TestSceneClock
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("add stopwatch", () => AddClock(new StopwatchClock(true)));
            AddStep("add non-negative stopwatch", () => AddClock(new TestClockWithRangeLimit()));
            AddStep("add framed", () => AddClock(new FramedClock()));
            AddStep("add lag", () => AddClock(new LaggyFramedClock()));
            AddStep("add interpolating", () => AddClock(new InterpolatingFramedClock()));
            AddStep("add decoupling(true)", () => AddClock(new DecouplingClock { AllowDecoupling = true }));
            AddStep("add decoupling(false)", () => AddClock(new DecouplingClock { AllowDecoupling = false }));

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
