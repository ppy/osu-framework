// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Platform;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    /// <summary>
    /// Interface for all Veldrid-based renderers that want to use the following objects:
    /// <list type="bullet">
    /// <item><see cref="VeldridShader"/></item>
    /// <item><see cref="VeldridShaderPart"/></item>
    /// <item><see cref="VeldridTexture"/></item>
    /// <item><see cref="VeldridVideoTexture"/></item>
    /// </list>
    /// </summary>
    internal interface IVeldridRenderer : IRenderer
    {
        /// <summary>
        /// The platform graphics device.
        /// </summary>
        GraphicsDevice Device { get; }

        /// <summary>
        /// The platform graphics resource factory.
        /// </summary>
        ResourceFactory Factory { get; }

        /// <summary>
        /// The graphics surface type.
        /// </summary>
        GraphicsSurfaceType SurfaceType { get; }

        /// <summary>
        /// Whether to use shader storage structured buffers if possible.
        /// </summary>
        bool UseStructuredBuffers { get; }

        /// <summary>
        /// Binds the given shader.
        /// </summary>
        /// <param name="shader">The shader.</param>
        void BindShader(VeldridShader shader);

        /// <summary>
        /// Unbinds the given shader.
        /// </summary>
        /// <param name="shader">The shader.</param>
        void UnbindShader(VeldridShader shader);

        /// <summary>
        /// Binds the given uniform buffer.
        /// </summary>
        /// <param name="blockName">The block to which the uniform buffer should be bound.</param>
        /// <param name="buffer">The uniform buffer.</param>
        void BindUniformBuffer(string blockName, IUniformBuffer buffer);

        /// <summary>
        /// Updates a <see cref="global::Veldrid.Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="global::Veldrid.Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The texture data.</param>
        /// <typeparam name="T">The pixel type.</typeparam>
        void UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data) where T : unmanaged;

        /// <summary>
        /// Updates a <see cref="global::Veldrid.Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="global::Veldrid.Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The texture data.</param>
        /// <param name="rowLengthInBytes">The number of bytes per row of the image to read from <paramref name="data"/>.</param>
        void UpdateTexture(Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes);

        /// <summary>
        /// Enqueues as texture to be uploaded.
        /// </summary>
        /// <param name="texture">The texture.</param>
        void EnqueueTextureUpload(VeldridTexture texture);

        /// <summary>
        /// Generates mipmaps for the given texture.
        /// </summary>
        /// <param name="texture">The texture.</param>
        void GenerateMipmaps(VeldridTexture texture);
    }
}
