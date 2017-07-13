// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using osu.Framework.Graphics.Containers;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformEdgeEffectAlpha : TransformFloat<IContainer>
    {
        public TransformEdgeEffectAlpha(IContainer target) : base(target)
        {
        }

        public override void Apply(IContainer c)
        {
            EdgeEffectParameters e = c.EdgeEffect;
            e.Colour.Linear.A = CurrentValue;
            c.EdgeEffect = e;
        }

        public override void ReadIntoStartValue(IContainer d) => StartValue = d.EdgeEffect.Colour.Linear.A;
    }
}
