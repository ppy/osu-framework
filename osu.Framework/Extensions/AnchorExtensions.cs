// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="Anchor"/> enumeration type.
    /// </summary>
    public static class AnchorExtensions
    {
        /// <summary>
        /// Returns the <see cref="Anchor"/> exactly opposite the given <paramref name="anchor"/>.
        /// </summary>
        public static Anchor Opposite(this Anchor anchor)
        {
            if (anchor == Anchor.Custom)
                throw new ArgumentException($"{nameof(Anchor.Custom)} is not supported.", nameof(anchor));

            if (anchor.HasFlagFast(Anchor.x0) || anchor.HasFlagFast(Anchor.x2))
                anchor ^= Anchor.x0 | Anchor.x2;
            if (anchor.HasFlagFast(Anchor.y0) || anchor.HasFlagFast(Anchor.y2))
                anchor ^= Anchor.y0 | Anchor.y2;

            return anchor;
        }

        /// <summary>
        /// Returns the position of the given <see cref="Anchor"/> on the given <see cref="Quad"/>.
        /// </summary>
        public static Vector2 PositionOnQuad(this Anchor anchor, Quad quad)
        {
            if (anchor == Anchor.Custom)
                throw new ArgumentException($"{nameof(Anchor.Custom)} is not supported.", nameof(anchor));

            Vector2 position = new Vector2();

            if (anchor.HasFlagFast(Anchor.x0))
                position.X = quad.TopLeft.X;
            else if (anchor.HasFlagFast(Anchor.x1))
                position.X = quad.Centre.X;
            else if (anchor.HasFlagFast(Anchor.x2))
                position.X = quad.BottomRight.X;

            if (anchor.HasFlagFast(Anchor.y0))
                position.Y = quad.TopLeft.Y;
            else if (anchor.HasFlagFast(Anchor.y1))
                position.Y = quad.Centre.Y;
            else if (anchor.HasFlagFast(Anchor.y2))
                position.Y = quad.BottomRight.Y;

            return position;
        }
    }
}
