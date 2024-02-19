// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Pipelines
{
    internal interface IGraphicsPipeline : IBasicPipeline
    {
        /// <summary>
        /// Clears the currently bound frame buffer.
        /// </summary>
        /// <param name="clearInfo">The clearing parameters.</param>
        void Clear(ClearInfo clearInfo);

        /// <summary>
        /// Sets the active scissor state.
        /// </summary>
        /// <param name="enabled">Whether the scissor test is enabled.</param>
        void SetScissorState(bool enabled);

        /// <summary>
        /// Sets the active shader.
        /// </summary>
        /// <param name="shader">The shader.</param>
        void SetShader(VeldridShader shader);

        /// <summary>
        /// Sets the active blending state.
        /// </summary>
        /// <param name="blendingParameters">The blending parameters.</param>
        void SetBlend(BlendingParameters blendingParameters);

        /// <summary>
        /// Sets a mask deciding which colour components are affected during blending.
        /// </summary>
        /// <param name="blendingMask">The blending mask.</param>
        void SetBlendMask(BlendingMask blendingMask);

        /// <summary>
        /// Sets the active viewport rectangle.
        /// </summary>
        /// <param name="viewport">The viewport rectangle.</param>
        void SetViewport(RectangleI viewport);

        /// <summary>
        /// Sets the active scissor rectangle.
        /// </summary>
        /// <param name="scissor">The scissor rectangle.</param>
        void SetScissor(RectangleI scissor);

        /// <summary>
        /// Sets the active depth parameters.
        /// </summary>
        /// <param name="depthInfo">The depth parameters.</param>
        void SetDepthInfo(DepthInfo depthInfo);

        /// <summary>
        /// Sets the active stencil parameters.
        /// </summary>
        /// <param name="stencilInfo">The stencil parameters.</param>
        void SetStencilInfo(StencilInfo stencilInfo);

        /// <summary>
        /// Sets the active framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer, or <c>null</c> to activate the back-buffer.</param>
        void SetFrameBuffer(IVeldridFrameBuffer? frameBuffer);

        /// <summary>
        /// Sets the active vertex buffer.
        /// </summary>
        /// <param name="buffer">The vertex buffer.</param>
        /// <param name="layout">The layout of vertices in the buffer.</param>
        void SetVertexBuffer(DeviceBuffer buffer, VertexLayoutDescription layout);

        /// <summary>
        /// Sets the active index buffer.
        /// </summary>
        /// <param name="indexBuffer">The index buffer.</param>
        void SetIndexBuffer(VeldridIndexBuffer indexBuffer);

        /// <summary>
        /// Attaches a texture to the pipeline at the given texture unit.
        /// </summary>
        /// <param name="unit">The texture unit.</param>
        /// <param name="texture">The texture.</param>
        void AttachTexture(int unit, IVeldridTexture texture);

        /// <summary>
        /// Attaches a uniform buffer to the pipeline at the given uniform block.
        /// </summary>
        /// <param name="name">The uniform block name.</param>
        /// <param name="buffer">The uniform buffer.</param>
        void AttachUniformBuffer(string name, IVeldridUniformBuffer buffer);

        /// <summary>
        /// Sets the offset of a uniform buffer.
        /// </summary>
        /// <param name="buffer">The uniform buffer.</param>
        /// <param name="bufferOffsetInBytes">The offset in the uniform buffer.</param>
        void SetUniformBufferOffset(IVeldridUniformBuffer buffer, uint bufferOffsetInBytes);

        /// <summary>
        /// Draws vertices from the active vertex buffer.
        /// </summary>
        /// <param name="topology">The vertex topology.</param>
        /// <param name="vertexStart">The vertex at which to start drawing.</param>
        /// <param name="verticesCount">The number of vertices to draw.</param>
        /// <param name="vertexIndexOffset">The base vertex value at which to start reading from.</param>
        /// <remarks>
        /// The choice of value for <paramref name="vertexStart"/> and <paramref name="vertexIndexOffset"/> depends on the specific use-case:
        /// <list type="bullet">
        ///   <item><paramref name="vertexStart"/> offsets where in the index buffer to start reading from.</item>
        ///   <item><paramref name="vertexIndexOffset"/> offsets where in the vertex buffer to start reading from.</item>
        /// </list>
        /// </remarks>
        /// <exception cref="InvalidOperationException">If no shader or index buffer is active.</exception>
        void DrawVertices(global::Veldrid.PrimitiveTopology topology, int vertexStart, int verticesCount, int vertexIndexOffset = 0);
    }
}
