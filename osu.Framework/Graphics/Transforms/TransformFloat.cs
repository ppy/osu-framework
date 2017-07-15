// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Transforms
{
    public abstract class TransformFloat<T> : Transform<float, T>
    {
        public virtual float CurrentValue
        {
            get
            {
                double time = Time?.Current ?? 0;
                if (time < StartTime) return StartValue;
                if (time >= EndTime) return EndValue;

                return (float)Interpolation.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
            }
        }
    }
}
