// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformEdgeEffectColour : Transform<Color4, IContainer>
    {
        /// <summary>
        /// Current value of the transformed colour in linear colour space.
        /// </summary>
        private Color4 valueAt(double time)
        {
            if (time < StartTime) return StartValue;
            if (time >= EndTime) return EndValue;

            return Interpolation.ValueAt(time, StartValue, EndValue, StartTime, EndTime, Easing);
        }

        public override string TargetMember => "EdgeEffect.Colour";

        protected override void Apply(IContainer c, double time)
        {
            EdgeEffectParameters e = c.EdgeEffect;
            e.Colour = valueAt(time);
            c.EdgeEffect = e;
        }

        protected override void ReadIntoStartValue(IContainer c) => StartValue = c.EdgeEffect.Colour;
    }
}
