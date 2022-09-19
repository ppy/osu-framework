// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Batches;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.OpenGL;
using PixelFormat = Veldrid.PixelFormat;
using PrimitiveTopology = Veldrid.PrimitiveTopology;
using Texture = Veldrid.Texture;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridRenderer : Renderer
    {
        public const int UNIFORM_RESOURCES_SLOT = 0;
        public const int TEXTURE_RESOURCES_SLOT = 1;

        protected internal override bool VerticalSync
        {
            get => Device.SyncToVerticalBlank;
            set => Device.SyncToVerticalBlank = value;
        }

        public override string ShaderFilenameSuffix => @"-veldrid";
        private IGraphicsSurface graphicsSurface = null!;

        public GraphicsDevice Device { get; private set; } = null!;

        public ResourceFactory Factory => Device.ResourceFactory;

        public CommandList Commands { get; private set; } = null!;

        private VeldridTextureSamplerSet defaultTextureSet = null!;
        private VeldridTextureSamplerSet boundTextureSet = null!;

        internal VeldridIndexData SharedLinearIndex { get; }
        internal VeldridIndexData SharedQuadIndex { get; }

        private ResourceLayout uniformLayout = null!;

        private DeviceBuffer? boundVertexBuffer;
        private ResourceSet? boundUniformSet;

        internal static readonly ResourceLayoutDescription UNIFORM_LAYOUT = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("m_Uniforms", ResourceKind.UniformBuffer, ShaderStages.Fragment | ShaderStages.Vertex));
        internal static readonly ResourceLayoutDescription TEXTURE_LAYOUT = new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("m_Texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
            new ResourceLayoutElementDescription("m_Sampler", ResourceKind.Sampler, ShaderStages.Fragment));

        private GraphicsPipelineDescription pipeline = new GraphicsPipelineDescription
        {
            RasterizerState = RasterizerStateDescription.CullNone,
            BlendState = BlendStateDescription.SingleOverrideBlend,
            ShaderSet = { VertexLayouts = new VertexLayoutDescription[1] }
        };

        private static readonly GlobalStatistic<int> stat_graphics_pipeline_created = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Total pipelines created");

        public VeldridRenderer()
        {
            SharedLinearIndex = new VeldridIndexData(this);
            SharedQuadIndex = new VeldridIndexData(this);
        }

        protected override void Initialise(IGraphicsSurface graphicsSurface)
        {
            // Veldrid must either be initialised on the main/"input" thread, or in a separate thread away from the draw thread at least.
            // Otherwise the window may not render anything on some platforms (macOS at least).
            Debug.Assert(!ThreadSafety.IsDrawThread, "Veldrid cannot be initialised on the draw thread.");

            this.graphicsSurface = graphicsSurface;

            var options = new GraphicsDeviceOptions
            {
                HasMainSwapchain = true,
                SwapchainDepthFormat = PixelFormat.R16_UNorm,
                // todo: we may want to use this over our shader-based toLinear/toSRGB correction functions.
                // SwapchainSrgbFormat = true,
                SyncToVerticalBlank = true,
                PreferDepthRangeZeroToOne = true,
                PreferStandardClipSpaceYDirection = true,
                ResourceBindingModel = ResourceBindingModel.Improved,
            };

            var size = graphicsSurface.GetDrawableSize();

            var swapchain = new SwapchainDescription
            {
                Width = (uint)size.Width,
                Height = (uint)size.Height,
                ColorSrgb = options.SwapchainSrgbFormat,
                DepthFormat = options.SwapchainDepthFormat,
                SyncToVerticalBlank = options.SyncToVerticalBlank,
            };

            int maxTextureSize = 0;

            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    swapchain.Source = SwapchainSource.CreateWin32(graphicsSurface.WindowHandle, IntPtr.Zero);
                    break;

                case RuntimeInfo.Platform.macOS:
                    var metalGraphics = (IMetalGraphicsSurface)graphicsSurface;
                    swapchain.Source = SwapchainSource.CreateNSView(metalGraphics.CreateMetalView());
                    break;

                case RuntimeInfo.Platform.Linux:
                    var linuxGraphics = (ILinuxGraphicsSurface)graphicsSurface;
                    swapchain.Source = linuxGraphics.IsWayland
                        ? SwapchainSource.CreateWayland(graphicsSurface.DisplayHandle, graphicsSurface.WindowHandle)
                        : SwapchainSource.CreateXlib(graphicsSurface.DisplayHandle, graphicsSurface.WindowHandle);
                    break;
            }

            switch (graphicsSurface.Type)
            {
                case GraphicsSurfaceType.OpenGL:
                    var openGLGraphics = (IOpenGLGraphicsSurface)graphicsSurface;

                    Device = GraphicsDevice.CreateOpenGL(options, new OpenGLPlatformInfo(
                        openGLContextHandle: openGLGraphics.WindowContext,
                        getProcAddress: openGLGraphics.GetProcAddress,
                        makeCurrent: openGLGraphics.MakeCurrent,
                        getCurrentContext: () => openGLGraphics.CurrentContext,
                        clearCurrentContext: openGLGraphics.ClearCurrent,
                        deleteContext: openGLGraphics.DeleteContext,
                        swapBuffers: openGLGraphics.SwapBuffers,
                        setSyncToVerticalBlank: v => openGLGraphics.VerticalSync = v), swapchain.Width, swapchain.Height);

                    Device.LogOpenGL(out maxTextureSize);
                    break;

                case GraphicsSurfaceType.Vulkan:
                    Device = GraphicsDevice.CreateVulkan(options, swapchain);
                    Device.LogVulkan(out maxTextureSize);
                    break;

                case GraphicsSurfaceType.Direct3D11:
                    Device = GraphicsDevice.CreateD3D11(options, swapchain);
                    Device.LogD3D11(out maxTextureSize);
                    break;

                case GraphicsSurfaceType.Metal:
                    Device = GraphicsDevice.CreateMetal(options, swapchain);
                    Device.LogMetal(out maxTextureSize);
                    break;
            }

            MaxTextureSize = maxTextureSize;

            Commands = Factory.CreateCommandList();

            uniformLayout = Factory.CreateResourceLayout(UNIFORM_LAYOUT);

            pipeline.ResourceLayouts = new ResourceLayout[2];
            pipeline.ResourceLayouts[UNIFORM_RESOURCES_SLOT] = uniformLayout;
            pipeline.Outputs = Device.SwapchainFramebuffer.OutputDescription;

            var defaultTexture = Factory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm_SRgb, TextureUsage.Sampled));
            Device.UpdateTexture(defaultTexture, new ReadOnlySpan<Rgba32>(new[] { new Rgba32(0, 0, 0) }), 0, 0, 0, 1, 1, 1, 0, 0);
            boundTextureSet = defaultTextureSet = new VeldridTextureSamplerSet(this, defaultTexture, Device.LinearSampler);
        }

        private Vector2 currentSize;

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            if (windowSize != currentSize)
            {
                Device.ResizeMainWindow((uint)windowSize.X, (uint)windowSize.Y);
                currentSize = windowSize;
            }

            Commands.Begin();

            base.BeginFrame(windowSize);

            boundTextureSet = defaultTextureSet;
        }

        protected internal override void FinishFrame()
        {
            base.FinishFrame();

            Commands.End();
            Device.SubmitCommands(Commands);
        }

        protected internal override void SwapBuffers() => Device.SwapBuffers();
        protected internal override void WaitUntilIdle() => Device.WaitForIdle();

        protected internal override void MakeCurrent()
        {
            if (graphicsSurface.Type == GraphicsSurfaceType.OpenGL)
            {
                var openGLGraphics = (IOpenGLGraphicsSurface)graphicsSurface;
                openGLGraphics.MakeCurrent(openGLGraphics.WindowContext);
            }
        }

        protected internal override void ClearCurrent()
        {
            if (graphicsSurface.Type == GraphicsSurfaceType.OpenGL)
            {
                var openGLGraphics = (IOpenGLGraphicsSurface)graphicsSurface;
                openGLGraphics.ClearCurrent();
            }
        }

        protected override void ClearImplementation(ClearInfo clearInfo)
        {
            Commands.ClearColorTarget(0, clearInfo.Colour.ToRgbaFloat());

            var framebuffer = ((VeldridFrameBuffer?)FrameBuffer)?.Framebuffer ?? Device.SwapchainFramebuffer;
            if (framebuffer.DepthTarget != null)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
        }

        protected override void SetScissorStateImplementation(bool enabled) => pipeline.RasterizerState.ScissorTestEnabled = enabled;

        protected override bool SetTextureImplementation(INativeTexture? texture, int unit)
        {
            var veldridTexture = (VeldridTexture?)texture;
            if (veldridTexture != null && veldridTexture.Resource == null)
                return false;

            var textureSet = veldridTexture?.Resource ?? defaultTextureSet;
            pipeline.ResourceLayouts[TEXTURE_RESOURCES_SLOT] = textureSet.Layout;
            boundTextureSet = textureSet;
            return true;
        }

        /// <summary>
        /// Updates a <see cref="Texture"/> with a <paramref name="data"/> at the specified coordinates.
        /// </summary>
        /// <param name="texture">The <see cref="Texture"/> to update.</param>
        /// <param name="x">The X coordinate of the update region.</param>
        /// <param name="y">The Y coordinate of the update region.</param>
        /// <param name="width">The width of the update region.</param>
        /// <param name="height">The height of the update region.</param>
        /// <param name="level">The texture level.</param>
        /// <param name="data">The textural data.</param>
        /// <param name="bufferRowLength">An optional length per row on the given <paramref name="data"/>.</param>
        /// <typeparam name="T">The pixel type.</typeparam>
        public unsafe void UpdateTexture<T>(Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data, int? bufferRowLength = null)
            where T : unmanaged
        {
            fixed (T* ptr = data)
            {
                if (bufferRowLength != null)
                {
                    var staging = Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, texture.Format, TextureUsage.Staging));

                    for (uint yi = 0; yi < height; yi++)
                        Device.UpdateTexture(staging, (IntPtr)(ptr + yi * bufferRowLength.Value), (uint)width, 0, yi, 0, (uint)width, 1, 1, 0, 0);

                    Commands.CopyTexture(staging, texture);
                    staging.Dispose();
                }
                else
                    Device.UpdateTexture(texture, (IntPtr)ptr, (uint)(data.Length * sizeof(T)), (uint)x, (uint)y, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            }
        }

        private static readonly Dictionary<int, ResourceLayout> texture_layouts = new Dictionary<int, ResourceLayout>();

        /// <summary>
        /// Retrieves a <see cref="ResourceLayout"/> for a texture-sampler resource set.
        /// </summary>
        /// <param name="textureCount">The number of textures in the resource layout.</param>
        /// <returns></returns>
        public ResourceLayout GetTextureResourceLayout(int textureCount)
        {
            if (texture_layouts.TryGetValue(textureCount, out var layout))
                return layout;

            var description = new ResourceLayoutDescription(new ResourceLayoutElementDescription[textureCount + 1]);
            var textureElement = TEXTURE_LAYOUT.Elements.Single(e => e.Kind == ResourceKind.TextureReadOnly);

            for (int i = 0; i < textureCount; i++)
                description.Elements[i] = new ResourceLayoutElementDescription($"{textureElement.Name}{i}", textureElement.Kind, textureElement.Stages);

            description.Elements[^1] = TEXTURE_LAYOUT.Elements.Single(e => e.Kind == ResourceKind.Sampler);
            return texture_layouts[textureCount] = Factory.CreateResourceLayout(ref description);
        }

        protected override void SetShaderImplementation(IShader shader)
        {
            var veldridShader = (VeldridShader)shader;
            pipeline.ShaderSet.Shaders = veldridShader.Shaders;
            boundUniformSet = veldridShader.UniformResourceSet;
        }

        protected override void SetBlendImplementation(BlendingParameters blendingParameters)
        {
            pipeline.BlendState.AttachmentStates[0].BlendEnabled = !blendingParameters.IsDisabled;
            pipeline.BlendState.AttachmentStates[0].SourceColorFactor = blendingParameters.Source.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].SourceAlphaFactor = blendingParameters.SourceAlpha.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].DestinationColorFactor = blendingParameters.Destination.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].DestinationAlphaFactor = blendingParameters.DestinationAlpha.ToBlendFactor();
            pipeline.BlendState.AttachmentStates[0].ColorFunction = blendingParameters.RGBEquation.ToBlendFunction();
            pipeline.BlendState.AttachmentStates[0].AlphaFunction = blendingParameters.AlphaEquation.ToBlendFunction();
        }

        protected override void SetBlendMaskImplementation(BlendingMask blendingMask)
        {
            pipeline.BlendState.AttachmentStates[0].ColorWriteMask = blendingMask.ToColorWriteMask();
        }

        protected override void SetViewportImplementation(RectangleI viewport)
        {
            Commands.SetViewport(0, new Viewport(viewport.Left, viewport.Top, viewport.Width, viewport.Height, 0, 1));
        }

        protected override void SetScissorImplementation(RectangleI scissor)
        {
            Commands.SetScissorRect(0, (uint)scissor.X, (uint)scissor.Y, (uint)scissor.Width, (uint)scissor.Height);
        }

        protected override void SetDepthInfoImplementation(DepthInfo depthInfo)
        {
            pipeline.DepthStencilState.DepthTestEnabled = depthInfo.DepthTest;
            pipeline.DepthStencilState.DepthWriteEnabled = depthInfo.WriteDepth;
            pipeline.DepthStencilState.DepthComparison = depthInfo.Function.ToComparisonKind();
        }

        protected override void SetStencilInfoImplementation(StencilInfo stencilInfo)
        {
            pipeline.DepthStencilState.StencilTestEnabled = stencilInfo.StencilTest;
            pipeline.DepthStencilState.StencilReference = (uint)stencilInfo.TestValue;
            pipeline.DepthStencilState.StencilReadMask = pipeline.DepthStencilState.StencilWriteMask = (byte)stencilInfo.Mask;
            pipeline.DepthStencilState.StencilBack.Pass = pipeline.DepthStencilState.StencilFront.Pass = stencilInfo.TestPassedOperation.ToStencilOperation();
            pipeline.DepthStencilState.StencilBack.Fail = pipeline.DepthStencilState.StencilFront.Fail = stencilInfo.StencilTestFailOperation.ToStencilOperation();
            pipeline.DepthStencilState.StencilBack.DepthFail = pipeline.DepthStencilState.StencilFront.DepthFail = stencilInfo.DepthTestFailOperation.ToStencilOperation();
            pipeline.DepthStencilState.StencilBack.Comparison = pipeline.DepthStencilState.StencilFront.Comparison = stencilInfo.TestFunction.ToComparisonKind();
        }

        protected override void SetFrameBufferImplementation(IFrameBuffer? frameBuffer)
        {
            VeldridFrameBuffer? veldridFrameBuffer = (VeldridFrameBuffer?)frameBuffer;
            Framebuffer framebuffer = veldridFrameBuffer?.Framebuffer ?? Device.SwapchainFramebuffer;

            Commands.SetFramebuffer(framebuffer);
            pipeline.Outputs = framebuffer.OutputDescription;
        }

        public void BindVertexBuffer(DeviceBuffer buffer, VertexLayoutDescription layout)
        {
            if (buffer == boundVertexBuffer)
                return;

            Commands.SetVertexBuffer(0, buffer);
            pipeline.ShaderSet.VertexLayouts[0] = layout;

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            boundVertexBuffer = buffer;
        }

        public void BindIndexBuffer(DeviceBuffer buffer, IndexFormat format) => Commands.SetIndexBuffer(buffer, format);

        public ResourceSet CreateUniformResourceSet(DeviceBuffer buffer) => Factory.CreateResourceSet(new ResourceSetDescription(uniformLayout, buffer));

        public void DrawVertices(PrimitiveTopology type, int indexStart, int indicesCount)
        {
            pipeline.PrimitiveTopology = type;

            Commands.SetPipeline(getPipelineInstance());
            Commands.SetGraphicsResourceSet(UNIFORM_RESOURCES_SLOT, boundUniformSet);
            Commands.SetGraphicsResourceSet(TEXTURE_RESOURCES_SLOT, boundTextureSet);
            Commands.DrawIndexed((uint)indicesCount, 1, (uint)indexStart, 0, 0);
        }

        /// <summary>
        /// Deletes a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to delete.</param>
        public void DeleteFrameBuffer(VeldridFrameBuffer frameBuffer)
        {
            while (FrameBuffer == frameBuffer)
                UnbindFrameBuffer(frameBuffer);

            frameBuffer.Framebuffer.Dispose();
        }

        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> pipelineCache = new Dictionary<GraphicsPipelineDescription, Pipeline>();

        private Pipeline getPipelineInstance()
        {
            if (!pipelineCache.TryGetValue(pipeline, out var instance))
            {
                pipelineCache[pipeline] = instance = Factory.CreateGraphicsPipeline(ref pipeline);
                stat_graphics_pipeline_created.Value++;
            }

            return instance;
        }

        protected override IShaderPart CreateShaderPart(ShaderManager manager, string name, byte[]? rawData, ShaderPartType partType)
            => new VeldridShaderPart(manager, rawData, partType);

        protected override IShader CreateShader(string name, params IShaderPart[] parts)
            => new VeldridShader(this, name, parts.Cast<VeldridShaderPart>().ToArray());

        public override IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new VeldridFrameBuffer(this, renderBufferFormats?.ToPixelFormats(), filteringMode.ToSamplerFilter());

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, Rendering.PrimitiveTopology primitiveType)
            => new VeldridLinearBatch<TVertex>(this, size, maxBuffers, primitiveType.ToPrimitiveTopology());

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
            => new VeldridQuadBatch<TVertex>(this, size, maxBuffers);

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Rgba32 initialisationColour = default)
            => new VeldridTexture(this, width, height, manualMipmaps, filteringMode.ToSamplerFilter(), initialisationColour);

        protected override INativeTexture CreateNativeVideoTexture(int width, int height) => new DummyNativeTexture(this);

        protected override void SetUniformImplementation<T>(IUniformWithValue<T> uniform)
        {
            var uniformOwner = (VeldridShader)uniform.Owner;

            switch (uniform)
            {
                case IUniformWithValue<Matrix3> matrix3:
                {
                    ref var value = ref matrix3.GetValueByRef();
                    Commands.UpdateBuffer(uniformOwner.UniformBuffer, (uint)(uniform.Location + 0), ref value.Row0);
                    Commands.UpdateBuffer(uniformOwner.UniformBuffer, (uint)(uniform.Location + 16), ref value.Row1);
                    Commands.UpdateBuffer(uniformOwner.UniformBuffer, (uint)(uniform.Location + 32), ref value.Row2);
                    break;
                }

                case IUniformWithValue<Matrix4> matrix4:
                {
                    ref var value = ref matrix4.GetValueByRef();
                    Commands.UpdateBuffer(uniformOwner.UniformBuffer, (uint)(uniform.Location + 0), ref value.Row0);
                    Commands.UpdateBuffer(uniformOwner.UniformBuffer, (uint)(uniform.Location + 16), ref value.Row1);
                    Commands.UpdateBuffer(uniformOwner.UniformBuffer, (uint)(uniform.Location + 32), ref value.Row2);
                    Commands.UpdateBuffer(uniformOwner.UniformBuffer, (uint)(uniform.Location + 48), ref value.Row3);
                    break;
                }

                default:
                    Commands.UpdateBuffer(uniformOwner.UniformBuffer, (uint)uniform.Location, ref uniform.GetValueByRef());
                    break;
            }
        }
    }
}
