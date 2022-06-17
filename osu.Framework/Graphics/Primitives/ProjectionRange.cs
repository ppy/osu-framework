// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osuTK;

namespace osu.Framework.Graphics.Primitives
{
    /// <summary>
    /// A structure that tells how "far" along an axis
    /// the projection of vertices onto the axis would be.
    /// </summary>
    internal readonly struct ProjectionRange
    {
        /// <summary>
        /// The minimum projected value.
        /// </summary>
        public float Min { get; }

        /// <summary>
        /// The maximum projected value.
        /// </summary>
        public float Max { get; }

        public ProjectionRange(Vector2 axis, ReadOnlySpan<Vector2> vertices)
        {
            Min = 0;
            Max = 0;

            if (vertices.Length == 0)
                return;

            Min = Vector2.Dot(axis, vertices[0]);
            Max = Min;

            for (int i = 1; i < vertices.Length; i++)
            {
                float val = Vector2.Dot(axis, vertices[i]);
                if (val < Min)
                    Min = val;
                if (val > Max)
                    Max = val;
            }
        }

        /// <summary>
        /// Checks whether this range overlaps another range.
        /// </summary>
        /// <param name="other">The other range to test against.</param>
        /// <returns>Whether the two ranges overlap.</returns>
        public bool Overlaps(ProjectionRange other) => Min <= other.Max && Max >= other.Min;
    }
}
