// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Transforms
{
    public class TransformBlurSigma<T> : TransformVector<BufferedContainer<T>> where T : Drawable
    {
        public TransformBlurSigma(BufferedContainer<T> target) : base(target)
        {
        }

        public override void Apply(BufferedContainer<T> d) => d.BlurSigma = CurrentValue;
        public override void ReadIntoStartValue(BufferedContainer<T> d) => StartValue = d.BlurSigma;
    }
}
