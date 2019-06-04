// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

namespace osu.Framework.Graphics.OpenGL
{
    /// <summary>
    /// The depth value used to draw 2D objects to the screen.
    /// Starts at -1f and increments to 1f for each <see cref="Drawable"/> which draws a hull through <see cref="DrawNode.DrawHull"/>.
    /// </summary>
    public class DepthValue
    {
        /// <summary>
        /// A safe value, such that rounding issues don't occur within 16-bit float precision.
        /// </summary>
        private const float increment = 0.001f;

        /// <summary>
        /// Calculated as (1 - (-1)) / increment - 1.
        /// -1 is subtracted since a depth of 1.0f conflicts with the default backbuffer clear value.
        /// </summary>
        private const int max_count = 1999;

        private float depth = -1;
        private int count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Increment()
        {
            depth += increment;
            count++;

            return depth;
        }

        public bool CanIncrement
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => count != max_count - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(DepthValue d) => d.depth;
    }
}
