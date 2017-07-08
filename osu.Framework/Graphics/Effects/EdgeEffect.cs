// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
        public EdgeEffectParameters Parameters { get; set; }

        /// <summary>
        /// Determines how large a radius is masked away around the corners.
        /// </summary>
        public float CornerRadius { get; set; }

        public Container ApplyTo(Drawable drawable)
        {
            return new Container
            {
                Masking = true,
                EdgeEffect = Parameters,
                CornerRadius = CornerRadius,
                Anchor = drawable.Anchor,
                Origin = drawable.Origin,
                RelativeSizeAxes = drawable.RelativeSizeAxes,
                AutoSizeAxes = Axes.Both & ~drawable.RelativeSizeAxes,
                Child = drawable
            };
        }
    }
}
