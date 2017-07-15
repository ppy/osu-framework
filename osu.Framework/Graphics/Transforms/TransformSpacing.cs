// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformSpacing : TransformVector<IFillFlowContainer>
    {
        public override void Apply(IFillFlowContainer d) => d.Spacing = CurrentValue;
        public override void ReadIntoStartValue(IFillFlowContainer d) => StartValue = d.Spacing;
    }
}
