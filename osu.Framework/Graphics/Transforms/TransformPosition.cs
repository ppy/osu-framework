// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Transforms
{
    public class TransformPosition : TransformVector
    {
        public override void Apply(Drawable d) => d.Position = CurrentValue;
        public override void ReadIntoStartValue(Drawable d) => StartValue = d.Position;
    }
}
