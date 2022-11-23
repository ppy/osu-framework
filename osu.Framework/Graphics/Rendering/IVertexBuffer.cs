// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Internal interface for all vertex buffers for use in <see cref="Renderer"/>.
    /// </summary>
    internal interface IVertexBuffer
    {
        /// <summary>
        /// The <see cref="Renderer.ResetId"/> when this <see cref="IVertexBuffer"/> was last used.
        /// </summary>
        ulong LastUseResetId { get; }

        /// <summary>
        /// Whether this <see cref="IVertexBuffer"/> is currently in use.
        /// </summary>
        bool InUse { get; }

        /// <summary>
        /// Frees all resources allocated by this <see cref="IVertexBuffer"/>.
        /// </summary>
        void Free();
    }
}
