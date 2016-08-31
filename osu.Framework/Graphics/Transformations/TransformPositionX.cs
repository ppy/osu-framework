//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Timing;
using OpenTK;

namespace osu.Framework.Graphics.Transformations
{
    public class TransformPositionX : TransformFloat
    {
        public override void Apply(Drawable d)
        {
            d.Position = new Vector2(CurrentValue, d.Position.Y);
        }

        public TransformPositionX(IClock clock) : base(clock)
        {
        }
    }
}