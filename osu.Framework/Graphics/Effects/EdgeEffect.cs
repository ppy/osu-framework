// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Effects
{
    /// <summary>
    /// An effect applied around the edge of the target drawable.
    /// </summary>
    public class EdgeEffect : IEffect<Container>
    {
        /// <summary>
        /// The parameters of the edge effect.
        /// </summary>
        public EdgeEffectParameters Parameters;

        /// <summary>
        /// Determines how large a radius is masked away around the corners. Default is 0.
        /// </summary>
        public float CornerRadius;

        public Container ApplyTo(Drawable drawable)
        {
            return new Container
            {
                Masking = true,
                EdgeEffect = Parameters,
                CornerRadius = CornerRadius,
            }.Wrap(drawable);
        }
    }
}
