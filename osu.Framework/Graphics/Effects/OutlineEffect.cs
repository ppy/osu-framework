// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Effects
{
    /// <summary>
    /// Creates an outline around the drawable this effect gets applied to.
    /// </summary>
    public class OutlineEffect : IEffect<BufferedContainer>
    {
        /// <summary>
        /// The strength of the outline. A higher strength means that the blur effect used to draw the outline fades slower.
        /// Default is 1.
        /// </summary>
        public float Strength = 1f;

        /// <summary>
        /// The sigma value for the blur effect used to draw the outline. This controls over how many pixels the outline gets spread.
        /// Default is <see cref="Vector2.One"/>.
        /// </summary>
        public Vector2 BlurSigma = Vector2.One;

        /// <summary>
        /// The color of the outline. Default is <see cref="Color4.Black"/>.
        /// </summary>
        public ColourInfo Colour = Color4.Black;

        /// <summary>
        /// Whether to automatically pad by the blur extent such that no clipping occurs at the sides of the effect. Default is false.
        /// </summary>
        public bool PadExtent;

        public BufferedContainer ApplyTo(Drawable drawable) => drawable.WithEffect(new BlurEffect
        {
            Strength = Strength,
            Sigma = BlurSigma,
            Colour = Colour,
            PadExtent = PadExtent,

            DrawOriginal = true,
        });
    }
}
