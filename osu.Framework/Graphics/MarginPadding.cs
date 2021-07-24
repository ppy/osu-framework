// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using System;
using osu.Framework.Graphics.Transforms;
using osu.Framework.Utils;

namespace osu.Framework.Graphics
{
    /// <summary>
    /// Holds data about the margin or padding of a <see cref="Drawable"/>.
    /// The margin describes the size of an empty area around its <see cref="Drawable"/>, while the padding describes the size of an empty area inside its container.
    /// </summary>
    public struct MarginPadding : IInterpolable<MarginPadding>, IEquatable<MarginPadding>
    {
        /// <summary>
        /// The absolute size of the space that should be left empty above the <see cref="Drawable"/> if used as margin, or
        /// the absolute size of the space that should be left empty from the top of the container if used as padding.
        /// </summary>
        public float Top;

        /// <summary>
        /// The absolute size of the space that should be left empty to the left of the <see cref="Drawable"/> if used as margin, or
        /// the absolute size of the space that should be left empty from the left of the container if used as padding.
        /// </summary>
        public float Left;

        /// <summary>
        /// The absolute size of the space that should be left empty below the <see cref="Drawable"/> if used as margin, or
        /// the absolute size of the space that should be left empty from the bottom of the container if used as padding.
        /// </summary>
        public float Bottom;

        /// <summary>
        /// The absolute size of the space that should be left empty to the right of the <see cref="Drawable"/> if used as margin, or
        /// the absolute size of the space that should be left empty from the right of the container if used as padding.
        /// </summary>
        public float Right;

        /// <summary>
        /// Gets the total absolute size of the empty space horizontally around the <see cref="Drawable"/> if used as margin, or
        /// the absolute size of the space left empty from the right and left of the container if used as padding.
        /// Effectively <see cref="Right"/> + <see cref="Left"/>.
        /// </summary>
        public readonly float TotalHorizontal => Left + Right;

        /// <summary>
        /// Sets the values of both <see cref="Left"/> and <see cref="Right"/> to the assigned value.
        /// </summary>
        public float Horizontal
        {
            set => Left = Right = value;
        }

        /// <summary>
        /// Gets the total absolute size of the empty space vertically around the <see cref="Drawable"/> or
        /// the absolute size of the space left empty from the top and bottom of the container if used as padding.
        /// Effectively <see cref="Top"/> + <see cref="Bottom"/>.
        /// </summary>
        public readonly float TotalVertical => Top + Bottom;

        /// <summary>
        /// Sets the values of both <see cref="Top"/> and <see cref="Bottom"/> to the assigned value.
        /// </summary>
        public float Vertical
        {
            set => Top = Bottom = value;
        }

        /// <summary>
        /// Gets the total absolute size of the empty space horizontally (x coordinate) and vertically (y coordinate) around the <see cref="Drawable"/> or inside the container if used as padding.
        /// </summary>
        public readonly Vector2 Total => new Vector2(TotalHorizontal, TotalVertical);

        /// <summary>
        /// Initializes all four sides (<see cref="Left"/>, <see cref="Right"/>, <see cref="Top"/> and <see cref="Bottom"/>) to the given value.
        /// </summary>
        /// <param name="allSides">The absolute size of the space that should be left around every side of the <see cref="Drawable"/>.</param>
        public MarginPadding(float allSides)
        {
            Top = Left = Bottom = Right = allSides;
        }

        public readonly bool Equals(MarginPadding other) => Top == other.Top && Left == other.Left && Bottom == other.Bottom && Right == other.Right;

        public override readonly string ToString() => $@"({Top}, {Left}, {Bottom}, {Right})";

        public static MarginPadding operator -(MarginPadding mp) =>
            new MarginPadding
            {
                Left = -mp.Left,
                Top = -mp.Top,
                Right = -mp.Right,
                Bottom = -mp.Bottom,
            };

        public MarginPadding ValueAt<TEasing>(double time, MarginPadding startValue, MarginPadding endValue, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => new MarginPadding
            {
                Left = Interpolation.ValueAt(time, startValue.Left, endValue.Left, startTime, endTime, easing),
                Top = Interpolation.ValueAt(time, startValue.Top, endValue.Top, startTime, endTime, easing),
                Right = Interpolation.ValueAt(time, startValue.Right, endValue.Right, startTime, endTime, easing),
                Bottom = Interpolation.ValueAt(time, startValue.Bottom, endValue.Bottom, startTime, endTime, easing),
            };
    }
}
