// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// The depth value used to draw 2D objects to the screen.
    /// Starts at -1f and increments to 1f for each <see cref="Drawable"/> which draws a opaque interior through <see cref="DrawNode.DrawOpaqueInterior"/>.
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

        private float depth;
        private int count;

        public DepthValue()
        {
            Reset();
        }

        /// <summary>
        /// Increments the depth value.
        /// </summary>
        /// <returns>The post-incremented depth value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal float Increment()
        {
            if (count == max_count)
                return depth;

            depth += increment;
            count++;

            return depth;
        }

        /// <summary>
        /// Reset to a pristine state.
        /// </summary>
        internal void Reset()
        {
            depth = -1;
            count = 0;
        }

        /// <summary>
        /// Whether the depth value can be incremented.
        /// </summary>
        internal bool CanIncrement
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => count < max_count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(DepthValue d) => d.depth;
    }
}
