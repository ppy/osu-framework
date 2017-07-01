// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
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
        public float Strength { get; set; } = 1f;
        /// <summary>
        /// The sigma of the blur. Default is (2, 2).
        /// </summary>
        public Vector2 Sigma { get; set; } = new Vector2(2f, 2f);
        /// <summary>
        /// The rotation of the blur. Default is 0.
        /// </summary>
        public float BlurRotation { get; set; }

        public BufferedContainer ApplyTo(Drawable drawable)
        {
            return new BufferedContainer
            {
                RelativeSizeAxes = drawable.RelativeSizeAxes,
                AutoSizeAxes = Axes.Both & ~drawable.RelativeSizeAxes,
                BlurSigma = Sigma,
                Anchor = drawable.Anchor,
                Origin = drawable.Origin,
                BlurRotation = BlurRotation,
                Padding = new MarginPadding
                {
                    Horizontal = Blur.KernelSize(Sigma.X),
                    Vertical = Blur.KernelSize(Sigma.Y)
                },
                Alpha = Strength,
                Child = drawable
            };
        }
    }
}
