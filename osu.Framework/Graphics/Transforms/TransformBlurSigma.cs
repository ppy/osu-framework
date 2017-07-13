// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformBlurSigma : TransformVector<IBufferedContainer>
    {
        public TransformBlurSigma(IBufferedContainer target) : base(target)
        {
        }

        public override void Apply(IBufferedContainer d) => d.BlurSigma = CurrentValue;
        public override void ReadIntoStartValue(IBufferedContainer d) => StartValue = d.BlurSigma;
    }
}
