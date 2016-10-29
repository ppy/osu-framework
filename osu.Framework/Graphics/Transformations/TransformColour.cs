// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.MathUtils;
using osu.Framework.Timing;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Transformations
{
    public class TransformColour : Transform<Color4>
    {
        protected override Color4 CurrentValue
        {
            get
            {
                double time = CurrentTime ?? 0;
                if (time < StartTime) return StartValue;
                if (time >= EndTime) return EndValue;

                return Interpolation.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
            }
        }

        public override void Apply(Drawable d)
        {
            base.Apply(d);
            d.Colour = CurrentValue;
        }
    }
}
