// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformEdgeEffectColour : Transform<Color4, Drawable>
    {
        /// <summary>
        /// Current value of the transformed colour in linear colour space.
        /// </summary>
        public override Color4 CurrentValue
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
            IContainer c = (IContainer)d;

            EdgeEffectParameters e = c.EdgeEffect;
            e.Colour = CurrentValue;
            c.EdgeEffect = e;
        }
    }
}