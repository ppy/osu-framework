// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Effects
{
    /// <summary>
    /// Creates an outline around the drawable this effect gets applied to.
    /// </summary>
    public class OutlineEffect : IEffect<BufferedContainer>
    {
        /// <summary>
        /// The color of the outline.
        /// </summary>
        public ColourInfo OutlineColour { get; set; } = Color4.Black;

        /// <summary>
        /// The sigma value for the blur effect used to draw the outline. This controls over how many pixels the outline gets spread.
        /// </summary>
        public Vector2 BlurSigma { get; set; } = Vector2.One;

        /// <summary>
        /// The strength of the outline. A higher strength means that the blur effect used to draw the outline fades slower.
        /// </summary>
        public float Strength { get; set; } = 1f;

        /// <summary>
        /// True if the effect should be cached. This is an optimization, but can cause issues if the drawable changes the way it looks without changing its size. Turned off by default.
        /// </summary>
        public bool CacheDrawnEffect { get; set; }

        public BufferedContainer ApplyTo(Drawable drawable)
        {
            return new BufferedContainer
            {
                CacheDrawnFrameBuffer = CacheDrawnEffect,

                Padding = new MarginPadding
                {
                    Horizontal = Blur.KernelSize(BlurSigma.X),
                    Vertical = Blur.KernelSize(BlurSigma.Y)
                },

                RelativeSizeAxes = drawable.RelativeSizeAxes,
                AutoSizeAxes = Axes.Both & ~drawable.RelativeSizeAxes,
                Anchor = drawable.Anchor,
                Origin = drawable.Origin,

                DrawOriginal = true,
                EffectColour = OutlineColour.MultiplyAlpha(Strength),
                BlurSigma = BlurSigma,
                Child = drawable
            };
        }
    }
}
