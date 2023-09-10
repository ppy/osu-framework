// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Utils.RectanglePacking
{
    public interface IRectanglePacker
    {
        /// <summary>
        /// The size of the bin.
        /// </summary>
        public Vector2I BinSize { get; }

        /// <summary>
        /// Adds a new rectangle into the bin.
        /// </summary>
        /// <param name="width">Width of rectangle to be added.</param>
        /// <param name="height">Height of rectangle to be added.</param>
        /// <returns>Position of added rectangle. Null if no space available.</returns>
        public abstract Vector2I? TryAdd(int width, int height);

        /// <summary>
        /// Resets the bin.
        /// </summary>
        public abstract void Reset();
    }
}