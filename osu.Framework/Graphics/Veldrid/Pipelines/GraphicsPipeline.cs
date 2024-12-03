// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Pipelines
{
    /// <summary>
    /// A pipeline that facilitates drawing.
    /// </summary>
    internal class GraphicsPipeline : BasicPipeline
    {
        private static readonly GlobalStatistic<int> stat_graphics_pipeline_created = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Total pipelines created");

        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> pipelineCache = new Dictionary<GraphicsPipelineDescription, Pipeline>();
        private readonly Dictionary<int, VeldridTextureResources> attachedTextures = new Dictionary<int, VeldridTextureResources>();
        private readonly Dictionary<string, IVeldridUniformBuffer> attachedUniformBuffers = new Dictionary<string, IVeldridUniformBuffer>();
        private readonly Dictionary<IVeldridUniformBuffer, uint> uniformBufferOffsets = new Dictionary<IVeldridUniformBuffer, uint>();

        private GraphicsPipelineDescription pipelineDesc = new GraphicsPipelineDescription
        {
            RasterizerState = RasterizerStateDescription.CULL_NONE,
            BlendState = BlendStateDescription.SINGLE_OVERRIDE_BLEND,
            ShaderSet = { VertexLayouts = new VertexLayoutDescription[1] }
        };

        private IVeldridFrameBuffer? currentFrameBuffer;
        private VeldridShader? currentShader;
        private VeldridIndexBuffer? currentIndexBuffer;
        private DeviceBuffer? currentVertexBuffer;
        private VertexLayoutDescription currentVertexLayout;

        public GraphicsPipeline(VeldridDevice device)
            : base(device)
        {
            pipelineDesc.Outputs = Device.SwapchainFramebuffer.OutputDescription;
        }

        public override void Begin()
        {
            base.Begin();

            attachedTextures.Clear();
            attachedUniformBuffers.Clear();
            uniformBufferOffsets.Clear();
            currentFrameBuffer = null;
            currentShader = null;
            currentIndexBuffer = null;
            currentVertexBuffer = null;
        }

        /// <summary>
        /// Clears the currently bound frame buffer.
        /// </summary>
        /// <param name="clearInfo">The clearing parameters.</param>
        public void Clear(ClearInfo clearInfo)
        {
            Commands.ClearColorTarget(0, clearInfo.Colour.ToRgbaFloat());

            var framebuffer = currentFrameBuffer?.Framebuffer ?? Device.SwapchainFramebuffer;
            if (framebuffer.DepthTarget != null)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
        }

        /// <summary>
        /// Sets the active scissor state.
        /// </summary>
        /// <param name="enabled">Whether the scissor test is enabled.</param>
        public void SetScissorState(bool enabled)
            => pipelineDesc.RasterizerState.ScissorTestEnabled = enabled;

        /// <summary>
        /// Sets the active shader.
        /// </summary>
        /// <param name="shader">The shader.</param>
        public void SetShader(VeldridShader shader)
        {
            shader.EnsureShaderInitialised();

            currentShader = shader;
            pipelineDesc.ShaderSet.Shaders = shader.Shaders;
        }

        /// <summary>
        /// Sets the active blending state.
        /// </summary>
        /// <param name="blendingParameters">The blending parameters.</param>
        public void SetBlend(BlendingParameters blendingParameters)
        {
            pipelineDesc.BlendState.AttachmentStates[0].BlendEnabled = !blendingParameters.IsDisabled;
            pipelineDesc.BlendState.AttachmentStates[0].SourceColorFactor = blendingParameters.Source.ToBlendFactor();
            pipelineDesc.BlendState.AttachmentStates[0].SourceAlphaFactor = blendingParameters.SourceAlpha.ToBlendFactor();
            pipelineDesc.BlendState.AttachmentStates[0].DestinationColorFactor = blendingParameters.Destination.ToBlendFactor();
            pipelineDesc.BlendState.AttachmentStates[0].DestinationAlphaFactor = blendingParameters.DestinationAlpha.ToBlendFactor();
            pipelineDesc.BlendState.AttachmentStates[0].ColorFunction = blendingParameters.RGBEquation.ToBlendFunction();
            pipelineDesc.BlendState.AttachmentStates[0].AlphaFunction = blendingParameters.AlphaEquation.ToBlendFunction();
        }

        /// <summary>
        /// Sets a mask deciding which colour components are affected during blending.
        /// </summary>
        /// <param name="blendingMask">The blending mask.</param>
        public void SetBlendMask(BlendingMask blendingMask)
            => pipelineDesc.BlendState.AttachmentStates[0].ColorWriteMask = blendingMask.ToColorWriteMask();

        /// <summary>
        /// Sets the active viewport rectangle.
        /// </summary>
        /// <param name="viewport">The viewport rectangle.</param>
        public void SetViewport(RectangleI viewport)
            => Commands.SetViewport(0, new Viewport(viewport.Left, viewport.Top, viewport.Width, viewport.Height, 0, 1));

        /// <summary>
        /// Sets the active scissor rectangle.
        /// </summary>
        /// <param name="scissor">The scissor rectangle.</param>
        public void SetScissor(RectangleI scissor)
            => Commands.SetScissorRect(0, (uint)scissor.X, (uint)scissor.Y, (uint)scissor.Width, (uint)scissor.Height);

        /// <summary>
        /// Sets the active depth parameters.
        /// </summary>
        /// <param name="depthInfo">The depth parameters.</param>
        public void SetDepthInfo(DepthInfo depthInfo)
        {
            pipelineDesc.DepthStencilState.DepthTestEnabled = depthInfo.DepthTest;
            pipelineDesc.DepthStencilState.DepthWriteEnabled = depthInfo.WriteDepth;
            pipelineDesc.DepthStencilState.DepthComparison = depthInfo.Function.ToComparisonKind();
        }

        /// <summary>
        /// Sets the active stencil parameters.
        /// </summary>
        /// <param name="stencilInfo">The stencil parameters.</param>
        public void SetStencilInfo(StencilInfo stencilInfo)
        {
            pipelineDesc.DepthStencilState.StencilTestEnabled = stencilInfo.StencilTest;
            pipelineDesc.DepthStencilState.StencilReference = (uint)stencilInfo.TestValue;
            pipelineDesc.DepthStencilState.StencilReadMask = pipelineDesc.DepthStencilState.StencilWriteMask = (byte)stencilInfo.Mask;
            pipelineDesc.DepthStencilState.StencilBack.Pass = pipelineDesc.DepthStencilState.StencilFront.Pass = stencilInfo.TestPassedOperation.ToStencilOperation();
            pipelineDesc.DepthStencilState.StencilBack.Fail = pipelineDesc.DepthStencilState.StencilFront.Fail = stencilInfo.StencilTestFailOperation.ToStencilOperation();
            pipelineDesc.DepthStencilState.StencilBack.DepthFail = pipelineDesc.DepthStencilState.StencilFront.DepthFail = stencilInfo.DepthTestFailOperation.ToStencilOperation();
            pipelineDesc.DepthStencilState.StencilBack.Comparison = pipelineDesc.DepthStencilState.StencilFront.Comparison = stencilInfo.TestFunction.ToComparisonKind();
        }

        /// <summary>
        /// Sets the active framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer, or <c>null</c> to activate the back-buffer.</param>
        public void SetFrameBuffer(IVeldridFrameBuffer? frameBuffer)
        {
            currentFrameBuffer = frameBuffer;

            Framebuffer fb = frameBuffer?.Framebuffer ?? Device.SwapchainFramebuffer;

            Commands.SetFramebuffer(fb);
            pipelineDesc.Outputs = fb.OutputDescription;
        }

        /// <summary>
        /// Sets the active vertex buffer.
        /// </summary>
        /// <param name="buffer">The vertex buffer.</param>
        /// <param name="layout">The layout of vertices in the buffer.</param>
        public void SetVertexBuffer(DeviceBuffer buffer, VertexLayoutDescription layout)
        {
            if (buffer == currentVertexBuffer && layout.Equals(currentVertexLayout))
                return;

            Commands.SetVertexBuffer(0, buffer);
            pipelineDesc.ShaderSet.VertexLayouts[0] = layout;

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            currentVertexBuffer = buffer;
            currentVertexLayout = layout;
        }

        /// <summary>
        /// Sets the active index buffer.
        /// </summary>
        /// <param name="indexBuffer">The index buffer.</param>
        public void SetIndexBuffer(VeldridIndexBuffer indexBuffer)
        {
            if (currentIndexBuffer == indexBuffer)
                return;

            currentIndexBuffer = indexBuffer;
            Commands.SetIndexBuffer(indexBuffer.Buffer, VeldridIndexBuffer.FORMAT);
        }

        /// <summary>
        /// Attaches a texture to the pipeline at the given texture unit.
        /// </summary>
        /// <param name="unit">The texture unit.</param>
        /// <param name="texture">The texture.</param>
        public void AttachTexture(int unit, IVeldridTexture texture)
        {
            var resources = texture.GetResourceList();

            for (int i = 0; i < resources.Count; i++)
                attachedTextures[unit++] = resources[i];
        }

        /// <summary>
        /// Attaches a uniform buffer to the pipeline at the given uniform block.
        /// </summary>
        /// <param name="name">The uniform block name.</param>
        /// <param name="buffer">The uniform buffer.</param>
        public void AttachUniformBuffer(string name, IVeldridUniformBuffer buffer)
            => attachedUniformBuffers[name] = buffer;

        /// <summary>
        /// Sets the offset of a uniform buffer.
        /// </summary>
        /// <param name="buffer">The uniform buffer.</param>
        /// <param name="bufferOffsetInBytes">The offset in the uniform buffer.</param>
        public void SetUniformBufferOffset(IVeldridUniformBuffer buffer, uint bufferOffsetInBytes)
            => uniformBufferOffsets[buffer] = bufferOffsetInBytes;

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
        public void DrawVertices(global::Veldrid.PrimitiveTopology topology, int vertexStart, int verticesCount, int vertexIndexOffset = 0)
        {
            if (currentShader == null)
                throw new InvalidOperationException("No shader bound.");

            if (currentIndexBuffer == null)
                throw new InvalidOperationException("No index buffer bound.");

            pipelineDesc.PrimitiveTopology = topology;
            Array.Resize(ref pipelineDesc.ResourceLayouts, currentShader.LayoutCount);

            // Activate texture layouts.
            foreach (var (unit, _) in attachedTextures)
            {
                var layout = currentShader.GetTextureLayout(unit);
                if (layout == null)
                    continue;

                pipelineDesc.ResourceLayouts[layout.Set] = layout.Layout;
            }

            // Activate uniform buffer layouts.
            foreach (var (name, _) in attachedUniformBuffers)
            {
                var layout = currentShader.GetUniformBufferLayout(name);
                if (layout == null)
                    continue;

                pipelineDesc.ResourceLayouts[layout.Set] = layout.Layout;
            }

            // Activate the pipeline.
            Commands.SetPipeline(createPipeline());

            // Activate texture resources.
            foreach (var (unit, texture) in attachedTextures)
            {
                var layout = currentShader.GetTextureLayout(unit);
                if (layout == null)
                    continue;

                Commands.SetGraphicsResourceSet((uint)layout.Set, texture.GetResourceSet(Factory, layout.Layout));
            }

            // Activate uniform buffer resources.
            foreach (var (name, buffer) in attachedUniformBuffers)
            {
                var layout = currentShader.GetUniformBufferLayout(name);
                if (layout == null)
                    continue;

                uint bufferOffset = uniformBufferOffsets.GetValueOrDefault(buffer);
                Commands.SetGraphicsResourceSet((uint)layout.Set, buffer.GetResourceSet(layout.Layout), 1, ref bufferOffset);
            }

            int indexStart = currentIndexBuffer.TranslateToIndex(vertexStart);
            int indicesCount = currentIndexBuffer.TranslateToIndex(verticesCount);
            Commands.DrawIndexed((uint)indicesCount, 1, (uint)indexStart, vertexIndexOffset, 0);
        }

        private Pipeline createPipeline()
        {
            if (!pipelineCache.TryGetValue(pipelineDesc, out var instance))
            {
                pipelineCache[pipelineDesc.Clone()] = instance = Factory.CreateGraphicsPipeline(ref pipelineDesc);
                stat_graphics_pipeline_created.Value++;
            }

            return instance;
        }
    }
}
