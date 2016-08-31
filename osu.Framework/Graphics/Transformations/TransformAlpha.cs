//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transformations
{
    public class TransformAlpha : TransformFloat
    {
        public override void Apply(Drawable d)
        {
            d.Alpha = CurrentValue;
        }

        public TransformAlpha(IClock clock) : base(clock)
        {
        }
    }
}