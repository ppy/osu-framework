// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformSpacing<T> : TransformVector<FillFlowContainer<T>> where T : Drawable
    {
        public TransformSpacing(FillFlowContainer<T> target) : base(target)
        {
        }

        public override void Apply(FillFlowContainer<T> d) => d.Spacing = CurrentValue;
        public override void ReadIntoStartValue(FillFlowContainer<T> d) => StartValue = d.Spacing;
    }
}
