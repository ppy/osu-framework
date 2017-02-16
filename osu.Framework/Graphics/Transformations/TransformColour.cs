// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.MathUtils;
using OpenTK.Graphics;
using osu.Framework.Graphics.Colour;

namespace osu.Framework.Graphics.Transformations
{
    /// <summary>
    /// Transforms colour value in linear colour space.
    /// </summary>
    public class TransformColour : Transform<Color4>
    {
        /// <summary>
        /// Current value of the transformed colour in linear colour space.
        /// </summary>
        protected override Color4 CurrentValue
        {
            get
            {
                double time = Time?.Current ?? 0;
                if (time < StartTime) return StartValue;
                if (time >= EndTime) return EndValue;

                return Interpolation.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
            }
        }

        public override void Apply(Drawable d)
        {
            base.Apply(d);
            d.Colour = new SRGBColour { Linear = CurrentValue };
        }
    }
}
