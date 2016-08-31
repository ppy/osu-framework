//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Timing;

namespace osu.Framework.Graphics.Transformations
{
    public class TransformRotation : TransformFloat
    {
        public override void Apply(Drawable d)
        {
            d.Rotation = CurrentValue;
        }

        public TransformRotation(IClock clock) : base(clock)
        {
        }
    }
}