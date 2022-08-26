// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    /// <summary>
    /// Internal interface for all <see cref="GLVertexBuffer{T}"/>s.
    /// </summary>
    internal interface IGLVertexBuffer
    {
        /// <summary>
        /// The <see cref="GLRenderer.ResetId"/> when this <see cref="IGLVertexBuffer"/> was last used.
        /// </summary>
        ulong LastUseResetId { get; }

        /// <summary>
        /// Whether this <see cref="IGLVertexBuffer"/> is currently in use.
        /// </summary>
        bool InUse { get; }

        /// <summary>
        /// Frees all resources allocated by this <see cref="IGLVertexBuffer"/>.
        /// </summary>
        void Free();
    }
}
