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
    internal class GraphicsPipeline : BasicPipeline, IGraphicsPipeline
    {
        private static readonly GlobalStatistic<int> stat_graphics_pipeline_created = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Total pipelines created");

        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> pipelineCache = new Dictionary<GraphicsPipelineDescription, Pipeline>();
        private readonly Dictionary<int, VeldridTextureResources> attachedTextures = new Dictionary<int, VeldridTextureResources>();
        private readonly Dictionary<string, IVeldridUniformBuffer> attachedUniformBuffers = new Dictionary<string, IVeldridUniformBuffer>();
        private readonly Dictionary<IVeldridUniformBuffer, uint> uniformBufferOffsets = new Dictionary<IVeldridUniformBuffer, uint>();

        private GraphicsPipelineDescription pipelineDesc = new GraphicsPipelineDescription
        {
            RasterizerState = RasterizerStateDescription.CullNone,
            BlendState = BlendStateDescription.SingleOverrideBlend,
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

        public void Clear(ClearInfo clearInfo)
        {
            Commands.ClearColorTarget(0, clearInfo.Colour.ToRgbaFloat());

            var framebuffer = currentFrameBuffer?.Framebuffer ?? Device.SwapchainFramebuffer;
            if (framebuffer.DepthTarget != null)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
        }

        public void SetScissorState(bool enabled)
            => pipelineDesc.RasterizerState.ScissorTestEnabled = enabled;

        public void SetShader(VeldridShader shader)
        {
            shader.EnsureShaderInitialised();

            currentShader = shader;
            pipelineDesc.ShaderSet.Shaders = shader.Shaders;
        }

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

        public void SetBlendMask(BlendingMask blendingMask)
            => pipelineDesc.BlendState.AttachmentStates[0].ColorWriteMask = blendingMask.ToColorWriteMask();

        public void SetViewport(RectangleI viewport)
            => Commands.SetViewport(0, new Viewport(viewport.Left, viewport.Top, viewport.Width, viewport.Height, 0, 1));

        public void SetScissor(RectangleI scissor)
            => Commands.SetScissorRect(0, (uint)scissor.X, (uint)scissor.Y, (uint)scissor.Width, (uint)scissor.Height);

        public void SetDepthInfo(DepthInfo depthInfo)
        {
            pipelineDesc.DepthStencilState.DepthTestEnabled = depthInfo.DepthTest;
            pipelineDesc.DepthStencilState.DepthWriteEnabled = depthInfo.WriteDepth;
            pipelineDesc.DepthStencilState.DepthComparison = depthInfo.Function.ToComparisonKind();
        }

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

        public void SetFrameBuffer(IVeldridFrameBuffer? frameBuffer)
        {
            currentFrameBuffer = frameBuffer;

            Framebuffer fb = frameBuffer?.Framebuffer ?? Device.SwapchainFramebuffer;

            Commands.SetFramebuffer(fb);
            pipelineDesc.Outputs = fb.OutputDescription;
        }

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

        public void SetIndexBuffer(VeldridIndexBuffer indexBuffer)
        {
            if (currentIndexBuffer == indexBuffer)
                return;

            currentIndexBuffer = indexBuffer;
            Commands.SetIndexBuffer(indexBuffer.Buffer, VeldridIndexBuffer.FORMAT);
        }

        public void AttachTexture(int unit, IVeldridTexture texture)
        {
            var resources = texture.GetResourceList();

            for (int i = 0; i < resources.Count; i++)
                attachedTextures[unit++] = resources[i];
        }

        public void AttachUniformBuffer(string name, IVeldridUniformBuffer buffer)
            => attachedUniformBuffers[name] = buffer;

        public void SetUniformBufferOffset(IVeldridUniformBuffer buffer, uint bufferOffsetInBytes)
            => uniformBufferOffsets[buffer] = bufferOffsetInBytes;

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
