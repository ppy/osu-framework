// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.CompilerServices;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// The depth value used to draw 2D objects to the screen.
    /// Starts at <see cref="minimum"/> and increments to 1f for each <see cref="Drawable"/> which draws a opaque interior through <see cref="DrawNode.DrawOpaqueInterior"/>.
    /// </summary>
    public class DepthValue
    {
        /// <summary>
        /// A safe value, such that rounding issues don't occur within 16-bit float precision.
        /// </summary>
        private const float increment = 0.001f;

        private readonly int minimum;
        private readonly int maxCount;

        private float depth;
        private int count;

        public DepthValue(IRenderer renderer)
        {
            minimum = renderer.DepthStartsFromNegativeOne ? -1 : 0;

            // -1 is subtracted since a depth of 1.0f may conflict with the default backbuffer clear value.
            maxCount = (int)((1 - minimum) / increment - 1);

            Reset();
        }

        /// <summary>
        /// Increments the depth value.
        /// </summary>
        /// <returns>The post-incremented depth value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal float Increment()
        {
            if (count == maxCount)
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
            depth = minimum;
            count = 0;
        }

        /// <summary>
        /// Whether the depth value can be incremented.
        /// </summary>
        internal bool CanIncrement
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => count < maxCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator float(DepthValue d) => d.depth;
    }
}
