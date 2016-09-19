// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.MathUtils;
using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transformations
{
    public abstract class TransformFloat : Transform<float>
    {
        public override float CurrentValue
        {
            get
            {
                double time = Time;
                if (time < StartTime) return StartValue;
                if (time >= EndTime) return EndValue;

                return Interpolation.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
            }
        }

        public TransformFloat(IClock clock)
            : base(clock)
        {
        }
    }
}
