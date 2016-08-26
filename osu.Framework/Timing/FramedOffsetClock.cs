//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT License - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Text;

namespace osu.Framework.Timing
{
    public class FramedOffsetClock : FramedClock
    {
        public double Offset;

        public double CurrentTime => base.CurrentTime + Offset;

        public FramedOffsetClock(IClock source)
            : base(source)
        {
        }
    }
}
