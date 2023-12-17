// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    public class TestStopwatchClockWithRangeLimit : StopwatchClock
    {
        public double MinTime => 0;
        public double MaxTime { get; set; } = double.PositiveInfinity;

        public TestStopwatchClockWithRangeLimit()
            : base(true)
        {
        }

        public override double CurrentTime
        {
            get
            {
                double currentTime = base.CurrentTime;
                double clamped = Math.Clamp(currentTime, MinTime, MaxTime);

                if (clamped == currentTime) return clamped;

                if ((Rate > 0 && clamped == MaxTime) || (Rate < 0 && clamped == MinTime))
                    Stop();

                return clamped;
            }
        }

        public override bool Seek(double position)
        {
            double clamped = Math.Clamp(position, MinTime, MaxTime);

            if (clamped != position)
            {
                // Emulate what a bass track will do in this situation.
                if (position >= MaxTime)
                    Stop();
                Seek(clamped);
                return false;
            }

            return base.Seek(position);
        }
    }
}
