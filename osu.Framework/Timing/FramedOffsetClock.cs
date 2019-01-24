// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Timing
{
    public class FramedOffsetClock : FramedClock
    {
        private double offset;

        public override double CurrentTime => base.CurrentTime + offset;

        public double Offset
        {
            get => offset;
            set
            {
                LastFrameTime += value - offset;
                offset = value;
            }
        }

        public FramedOffsetClock(IClock source)
            : base(source)
        {
        }
    }
}
