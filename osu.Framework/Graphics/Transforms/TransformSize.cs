// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Graphics.Transforms
{
    public class TransformSize : TransformVector<Drawable>
    {
        public TransformSize(Drawable target) : base(target)
        {
        }

        public override void Apply(Drawable d) => d.Size = CurrentValue;
        public override void ReadIntoStartValue(Drawable d) => StartValue = d.Size;
    }

    public class TransformWidth : TransformFloat<Drawable>
    {
        public TransformWidth(Drawable target) : base(target)
        {
        }

        public override void Apply(Drawable d) => d.Width = CurrentValue;
        public override void ReadIntoStartValue(Drawable d) => StartValue = d.Width;
    }

    public class TransformHeight : TransformFloat<Drawable>
    {
        public TransformHeight(Drawable target) : base(target)
        {
        }

        public override void Apply(Drawable d) => d.Height = CurrentValue;
        public override void ReadIntoStartValue(Drawable d) => StartValue = d.Height;
    }
}
