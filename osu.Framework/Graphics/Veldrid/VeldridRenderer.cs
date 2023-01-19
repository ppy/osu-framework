// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Development;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Batches;
using osu.Framework.Platform;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp.PixelFormats;
using Veldrid;
using Veldrid.OpenGL;
using PixelFormat = Veldrid.PixelFormat;
using PrimitiveTopology = Veldrid.PrimitiveTopology;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridRenderer : Renderer
    {
        protected internal override bool VerticalSync
        {
            get => Device.SyncToVerticalBlank;
            set => Device.SyncToVerticalBlank = value;
        }

        public GraphicsDevice Device { get; private set; } = null!;

        public ResourceFactory Factory => Device.ResourceFactory;

        public CommandList Commands { get; private set; } = null!;

        public VeldridIndexData SharedLinearIndex { get; }
        public VeldridIndexData SharedQuadIndex { get; }

        private IGraphicsSurface graphicsSurface = null!;

        private DeviceBuffer? boundVertexBuffer;

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

            pipeline.ResourceLayouts = new ResourceLayout[2];
            pipeline.Outputs = Device.SwapchainFramebuffer.OutputDescription;
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

            Clear(new ClearInfo(Color4.FromHsv(new Vector4(ResetId % 360 / 360f, 0.5f, 0.5f, 1f))));
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

            if (Device.SwapchainFramebuffer.DepthTarget != null)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
        }

        protected override void SetScissorStateImplementation(bool enabled) => pipeline.RasterizerState.ScissorTestEnabled = enabled;

        protected override bool SetTextureImplementation(INativeTexture? texture, int unit) => true;

        protected override void SetShaderImplementation(IShader shader)
        {
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
            Commands.SetFramebuffer(Device.SwapchainFramebuffer);
            pipeline.Outputs = Device.SwapchainFramebuffer.OutputDescription;
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

        public void DrawVertices(PrimitiveTopology type, int indexStart, int indicesCount)
        {
            pipeline.PrimitiveTopology = type;

            // we can't draw yet as we're missing shader support.
            // Commands.SetPipeline(getPipelineInstance());
            // Commands.DrawIndexed((uint)indicesCount, 1, (uint)indexStart, 0, 0);
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
            => new DummyShaderPart();

        protected override IShader CreateShader(string name, params IShaderPart[] parts)
            => new DummyShader(this);

        public override IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new DummyFrameBuffer(this);

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, Rendering.PrimitiveTopology primitiveType)
            => new VeldridLinearBatch<TVertex>(this, size, maxBuffers, primitiveType.ToPrimitiveTopology());

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
            => new VeldridQuadBatch<TVertex>(this, size, maxBuffers);

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Rgba32 initialisationColour = default)
            => new DummyNativeTexture(this);

        protected override INativeTexture CreateNativeVideoTexture(int width, int height) => new DummyNativeTexture(this);

        protected override void SetUniformImplementation<T>(IUniformWithValue<T> uniform)
        {
        }
    }
}
