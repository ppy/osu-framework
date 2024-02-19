// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal interface IDeferredVertexBatch
    {
        /// <summary>
        /// Writes a primitive to the buffer.
        /// </summary>
        /// <param name="primitive">The primitive to write. This should be exactly the full size of a primitive (triangle or quad).</param>
        void Write(in MemoryReference primitive);

        /// <summary>
        /// Draws a number of vertices from this batch.
        /// </summary>
        /// <param name="count">The number of vertices to draw.</param>
        void Draw(int count);
    }
}
