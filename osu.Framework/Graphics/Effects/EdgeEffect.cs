// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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

        public Container ApplyTo(Drawable drawable) =>
            new Container
            {
                Masking = true,
                EdgeEffect = Parameters,
                CornerRadius = CornerRadius,
            }.Wrap(drawable);
    }
}
