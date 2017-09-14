﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Effects
{
    /// <summary>
    /// A blur effect that wraps a drawable in a <see cref="BufferedContainer"/> which applies a blur effect to it.
    /// </summary>
    public class BlurEffect : IEffect<BufferedContainer>
    {
        /// <summary>
        /// The strength of the blur. Default is 1.
        /// </summary>
        public float Strength = 1f;

        /// <summary>
        /// The sigma of the blur. Default is (2, 2).
        /// </summary>
        public Vector2 Sigma = new Vector2(2f, 2f);

        /// <summary>
        /// The rotation of the blur. Default is 0.
        /// </summary>
        public float Rotation;

        /// <summary>
        /// The colour of the blur. Default is <see cref="Color4.White"/>.
        /// </summary>
        public ColourInfo Colour = Color4.White;

        /// <summary>
        /// The blending mode of the blur. Default is inheriting from the target drawable.
        /// </summary>
        public BlendingParameters Blending;

        /// <summary>
        /// Whether to draw the blur in front or behind the original. Default is behind.
        /// </summary>
        public EffectPlacement Placement;

        /// <summary>
        /// Whether to draw the original target in addition to its blurred version.
        /// </summary>
        public bool DrawOriginal;

        /// <summary>
        /// Whether to automatically pad by the blur extent such that no clipping occurs at the sides of the effect. Default is false.
        /// </summary>
        public bool PadExtent;

        /// <summary>
        /// Whether the resulting <see cref="BufferedContainer"/> should cache its drawn framebuffer.
        /// </summary>
        public bool CacheDrawnEffect;

        public BufferedContainer ApplyTo(Drawable drawable)
        {
            Vector2 position = drawable.Position;
            drawable.Position = Vector2.Zero;

            return new BufferedContainer
            {
                RelativeSizeAxes = drawable.RelativeSizeAxes,
                AutoSizeAxes = Axes.Both & ~drawable.RelativeSizeAxes,
                Anchor = drawable.Anchor,
                Origin = drawable.Origin,

                BlurSigma = Sigma,
                BlurRotation = Rotation,
                EffectColour = Colour.MultiplyAlpha(Strength),
                EffectBlending = Blending,
                EffectPlacement = Placement,

                DrawOriginal = DrawOriginal,

                CacheDrawnFrameBuffer = CacheDrawnEffect,

                Position = position,
                Padding = !PadExtent ? new MarginPadding() : new MarginPadding
                {
                    Horizontal = Blur.KernelSize(Sigma.X),
                    Vertical = Blur.KernelSize(Sigma.Y),
                },

                Child = drawable
            };
        }
    }
}
