// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformRelativeChildOffset : TransformVector<IContainer>
    {
        public override void Apply(IContainer d) => d.RelativeChildOffset = CurrentValue;
        public override void ReadIntoStartValue(IContainer d) => StartValue = d.RelativeChildOffset;
    }
}
