// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    /// <summary>
    /// Internal interface for all <see cref="OpenGLVertexBuffer{T}"/>s.
    /// </summary>
    internal interface IOpenGLVertexBuffer
    {
        /// <summary>
        /// The <see cref="OpenGLRenderer.ResetId"/> when this <see cref="IOpenGLVertexBuffer"/> was last used.
        /// </summary>
        ulong LastUseResetId { get; }

        /// <summary>
        /// Whether this <see cref="IOpenGLVertexBuffer"/> is currently in use.
        /// </summary>
        bool InUse { get; }

        /// <summary>
        /// Frees all resources allocated by this <see cref="IOpenGLVertexBuffer"/>.
        /// </summary>
        void Free();
    }
}
