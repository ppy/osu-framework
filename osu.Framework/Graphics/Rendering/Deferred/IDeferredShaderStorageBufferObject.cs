// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Rendering.Deferred.Allocation;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal interface IDeferredShaderStorageBufferObject
    {
        /// <summary>
        /// Writes data to the buffer at the given index.
        /// </summary>
        /// <param name="index">The element index in the buffer at which to begin writing.</param>
        /// <param name="memory">The data to write.</param>
        void Write(int index, MemoryReference memory);
    }
}
