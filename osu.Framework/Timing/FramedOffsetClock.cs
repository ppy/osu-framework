// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Timing
{
    public class FramedOffsetClock : FramedClock
    {
        private double offset;

        public override double CurrentTime => base.CurrentTime + offset;

        public double Offset
        {
            get { return offset; }
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
