// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osuTK;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// A drawable that can blur the background behind itself.
    /// </summary>
    public interface IBackdropBlurDrawable : IBufferedDrawable
    {
        /// <summary>
        /// Controls the amount of blurring in two orthogonal directions (X and Y if
        /// <see cref="BlurRotation"/> is zero).
        /// Blur is parametrized by a gaussian image filter. This property controls
        /// the standard deviation (sigma) of the gaussian kernel.
        /// </summary>
        public Vector2 BlurSigma { get; }

        /// <summary>
        /// Rotates the blur kernel clockwise. In degrees. Has no effect if
        /// <see cref="BlurSigma"/> has the same magnitude in both directions.
        /// </summary>
        public float BlurRotation { get; }

        /// <summary>
        /// The opacity at which the blurred backbuffer is drawn.
        /// </summary>
        public float BackdropOpacity { get; }

        /// <summary>
        /// The alpha at which the content is no longer considered opaque and the background will not be blurred behind it.
        /// </summary>
        public float MaskCutoff { get; }

        /// <summary>
        /// Controls how much the blurred backbuffer is tinted by the content.
        /// </summary>
        public float BackdropTintStrength { get; }

        public Vector2 EffectBufferScale { get; }

        public IShader BlurShader { get; }

        public IShader BlendShader { get; }

        public RectangleF LastBackBufferDrawRect { get; }
    }
}
