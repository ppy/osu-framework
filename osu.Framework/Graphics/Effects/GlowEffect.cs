// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Effects
{
    /// <summary>
    /// Creates a glow around the drawable this effect gets applied to.
    /// </summary>
    public class GlowEffect : IEffect<BufferedContainer>
    {
        /// <summary>
        /// The strength of the glow. A higher strength means that the glow fades outward slower. Default is 1.
        /// </summary>
        public float Strength = 1f;

        /// <summary>
        /// The sigma value for the blur of the glow. This controls how spread out the glow is. Default is 5 in both X and Y.
        /// </summary>
        public Vector2 BlurSigma = new Vector2(5);

        /// <summary>
        /// The color of the outline. Default is <see cref="Color4.White"/>.
        /// </summary>
        public ColourInfo Colour = Color4.White;

        /// <summary>
        /// The blending mode of the glow. Default is additive.
        /// </summary>
        public BlendingParameters Blending = BlendingParameters.Additive;

        /// <summary>
        /// Whether to draw the glow <see cref="EffectPlacement.InFront"/> or <see cref="EffectPlacement.Behind"/> the glowing
        /// <see cref="Drawable"/>. Default is <see cref="EffectPlacement.InFront"/>.
        /// </summary>
        public EffectPlacement Placement = EffectPlacement.InFront;

        /// <summary>
        /// Whether to automatically pad by the glow extent such that no clipping occurs at the sides of the effect. Default is false.
        /// </summary>
        public bool PadExtent;

        public BufferedContainer ApplyTo(Drawable drawable) => drawable.WithEffect(new BlurEffect
        {
            Strength = Strength,
            Sigma = BlurSigma,
            Colour = Colour,
            Blending = Blending,
            Placement = Placement,
            PadExtent = PadExtent,

            DrawOriginal = true,
        });
    }
}
