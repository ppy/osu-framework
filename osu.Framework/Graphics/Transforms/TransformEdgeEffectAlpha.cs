// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformEdgeEffectAlpha : TransformFloat
    {
        public override void Apply(Drawable d)
        {
            base.Apply(d);
            Container c = (Container)d;

            EdgeEffect e = c.EdgeEffect;
            e.Colour.Linear.A = CurrentValue;
            c.EdgeEffect = e;
        }
    }
}
