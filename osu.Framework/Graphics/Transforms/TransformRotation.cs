// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Transforms
{
    public class TransformRotation : TransformFloat<Drawable>
    {
        public override void Apply(Drawable d) => d.Rotation = CurrentValue;
        public override void ReadIntoStartValue(Drawable d) => StartValue = d.Rotation;
    }
}
