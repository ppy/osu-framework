// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Batches;
using osu.Framework.Platform;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Buffers.Staging;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;
using Veldrid.OpenGL;
using Veldrid.OpenGLBinding;
using Image = SixLabors.ImageSharp.Image;
using PixelFormat = Veldrid.PixelFormat;
using PrimitiveTopology = Veldrid.PrimitiveTopology;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridRenderer : Renderer
    {
        public GraphicsSurfaceType SurfaceType => graphicsSurface.Type;

        protected internal override bool VerticalSync
        {
            get => Device.SyncToVerticalBlank;
            set => Device.SyncToVerticalBlank = value;
        }

        protected internal override bool AllowTearing
        {
            get => Device.AllowTearing;
            set => Device.AllowTearing = value;
        }

        public override bool IsDepthRangeZeroToOne => Device.IsDepthRangeZeroToOne;
        public override bool IsUvOriginTopLeft => Device.IsUvOriginTopLeft;
        public override bool IsClipSpaceYInverted => Device.IsClipSpaceYInverted;

        public GraphicsDevice Device { get; private set; } = null!;

        public ResourceFactory Factory => Device.ResourceFactory;

        public CommandList Commands { get; private set; } = null!;
        public CommandList BufferUpdateCommands { get; private set; } = null!;

        public VeldridIndexData SharedLinearIndex { get; }
        public VeldridIndexData SharedQuadIndex { get; }

        private readonly HashSet<IVeldridUniformBuffer> uniformBufferResetList = new HashSet<IVeldridUniformBuffer>();
        private readonly Dictionary<int, VeldridTextureResources> boundTextureUnits = new Dictionary<int, VeldridTextureResources>();
        private readonly Dictionary<string, IVeldridUniformBuffer> boundUniformBuffers = new Dictionary<string, IVeldridUniformBuffer>();
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
                SyncToVerticalBlank = true,
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
                {
                    swapchain.Source = SwapchainSource.CreateWin32(graphicsSurface.WindowHandle, IntPtr.Zero);
                    break;
                }

                case RuntimeInfo.Platform.macOS:
                {
                    // OpenGL doesn't use a swapchain, so it's only needed on Metal.
                    // Creating a Metal surface in general would otherwise destroy the GL context.
                    if (graphicsSurface.Type == GraphicsSurfaceType.Metal)
                    {
                        var metalGraphics = (IMetalGraphicsSurface)graphicsSurface;
                        swapchain.Source = SwapchainSource.CreateNSView(metalGraphics.CreateMetalView());
                    }

                    break;
                }

                case RuntimeInfo.Platform.iOS:
                {
                    // OpenGL doesn't use a swapchain, so it's only needed on Metal.
                    // Creating a Metal surface in general would otherwise destroy the GL context.
                    if (graphicsSurface.Type == GraphicsSurfaceType.Metal)
                    {
                        var metalGraphics = (IMetalGraphicsSurface)graphicsSurface;
                        swapchain.Source = SwapchainSource.CreateUIView(metalGraphics.CreateMetalView());
                    }

                    break;
                }

                case RuntimeInfo.Platform.Linux:
                {
                    var linuxGraphics = (ILinuxGraphicsSurface)graphicsSurface;
                    swapchain.Source = linuxGraphics.IsWayland
                        ? SwapchainSource.CreateWayland(graphicsSurface.DisplayHandle, graphicsSurface.WindowHandle)
                        : SwapchainSource.CreateXlib(graphicsSurface.DisplayHandle, graphicsSurface.WindowHandle);
                    break;
                }
            }

            switch (graphicsSurface.Type)
            {
                case GraphicsSurfaceType.OpenGL:
                    var openGLGraphics = (IOpenGLGraphicsSurface)graphicsSurface;
                    var openGLInfo = new OpenGLPlatformInfo(
                        openGLContextHandle: openGLGraphics.WindowContext,
                        getProcAddress: openGLGraphics.GetProcAddress,
                        makeCurrent: openGLGraphics.MakeCurrent,
                        getCurrentContext: () => openGLGraphics.CurrentContext,
                        clearCurrentContext: openGLGraphics.ClearCurrent,
                        deleteContext: openGLGraphics.DeleteContext,
                        swapBuffers: openGLGraphics.SwapBuffers,
                        setSyncToVerticalBlank: v => openGLGraphics.VerticalSync = v,
                        setSwapchainFramebuffer: () => OpenGLNative.glBindFramebuffer(FramebufferTarget.Framebuffer, (uint)(openGLGraphics.BackbufferFramebuffer ?? 0)),
                        null);

                    Device = GraphicsDevice.CreateOpenGL(options, openGLInfo, swapchain.Width, swapchain.Height);
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
            BufferUpdateCommands = Factory.CreateCommandList();

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

            foreach (var ubo in uniformBufferResetList)
                ubo.ResetCounters();
            uniformBufferResetList.Clear();

            Commands.Begin();
            BufferUpdateCommands.Begin();

            base.BeginFrame(windowSize);
        }

        protected internal override void FinishFrame()
        {
            base.FinishFrame();

            BufferUpdateCommands.End();
            Device.SubmitCommands(BufferUpdateCommands);

            Commands.End();
            Device.SubmitCommands(Commands);
        }

        protected internal override void SwapBuffers() => Device.SwapBuffers();
        protected internal override void WaitUntilIdle() => Device.WaitForIdle();
        protected internal override void WaitUntilNextFrameReady() => Device.WaitForNextFrameReady();

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

            var framebuffer = (FrameBuffer as VeldridFrameBuffer)?.Framebuffer ?? Device.SwapchainFramebuffer;
            if (framebuffer.DepthTarget != null)
                Commands.ClearDepthStencil((float)clearInfo.Depth, (byte)clearInfo.Stencil);
        }

        protected override void SetScissorStateImplementation(bool enabled) => pipeline.RasterizerState.ScissorTestEnabled = enabled;

        protected override bool SetTextureImplementation(INativeTexture? texture, int unit)
        {
            if (texture is not VeldridTexture veldridTexture)
                return false;

            var resources = veldridTexture.GetResourceList();

            for (int i = 0; i < resources.Count; i++)
                BindTextureResource(resources[i], unit++);

            return true;
        }

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
        public void UpdateTexture<T>(global::Veldrid.Texture texture, int x, int y, int width, int height, int level, ReadOnlySpan<T> data)
            where T : unmanaged
        {
            Device.UpdateTexture(texture, data, (uint)x, (uint)y, 0, (uint)width, (uint)height, 1, (uint)level, 0);
        }

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
        public void UpdateTexture(global::Veldrid.Texture texture, int x, int y, int width, int height, int level, IntPtr data, int rowLengthInBytes)
        {
            using var staging = Factory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, texture.Format, TextureUsage.Staging));

            unsafe
            {
                MappedResource mappedData = Device.Map(staging, MapMode.Write);

                try
                {
                    void* srcPtr = data.ToPointer();
                    void* dstPtr = mappedData.Data.ToPointer();

                    for (int i = 0; i < height; i++)
                    {
                        Unsafe.CopyBlockUnaligned(dstPtr, srcPtr, mappedData.RowPitch);

                        srcPtr = Unsafe.Add<byte>(srcPtr, rowLengthInBytes);
                        dstPtr = Unsafe.Add<byte>(dstPtr, (int)mappedData.RowPitch);
                    }
                }
                finally
                {
                    Device.Unmap(staging);
                }
            }

            BufferUpdateCommands.CopyTexture(
                staging, 0, 0, 0, 0, 0,
                texture, (uint)x, (uint)y, 0, (uint)level, 0, (uint)width, (uint)height, 1, 1);
        }

        protected override void SetShaderImplementation(IShader shader)
        {
            var veldridShader = (VeldridShader)shader;
            pipeline.ShaderSet.Shaders = veldridShader.Shaders;
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

            SetFramebuffer(framebuffer);
        }

        public void SetFramebuffer(Framebuffer framebuffer)
        {
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

        public void BindUniformBuffer(string blockName, IVeldridUniformBuffer veldridBuffer)
        {
            FlushCurrentBatch(FlushBatchSource.BindBuffer);
            boundUniformBuffers[blockName] = veldridBuffer;
        }

        public void DrawVertices(PrimitiveTopology type, int indexStart, int indicesCount)
        {
            var veldridShader = (VeldridShader)Shader!;

            pipeline.PrimitiveTopology = type;
            Array.Resize(ref pipeline.ResourceLayouts, veldridShader.LayoutCount);

            // Activate texture layouts.
            foreach (var (unit, _) in boundTextureUnits)
            {
                var layout = veldridShader.GetTextureLayout(unit);
                if (layout == null)
                    continue;

                pipeline.ResourceLayouts[layout.Set] = layout.Layout;
            }

            // Activate uniform buffer layouts.
            foreach (var (name, _) in boundUniformBuffers)
            {
                var layout = veldridShader.GetUniformBufferLayout(name);
                if (layout == null)
                    continue;

                pipeline.ResourceLayouts[layout.Set] = layout.Layout;
            }

            // Activate the pipeline.
            Commands.SetPipeline(getPipelineInstance());

            // Activate texture resources.
            foreach (var (unit, texture) in boundTextureUnits)
            {
                var layout = veldridShader.GetTextureLayout(unit);
                if (layout == null)
                    continue;

                Commands.SetGraphicsResourceSet((uint)layout.Set, texture.GetResourceSet(this, layout.Layout));
            }

            // Activate uniform buffer resources.
            foreach (var (name, buffer) in boundUniformBuffers)
            {
                var layout = veldridShader.GetUniformBufferLayout(name);
                if (layout == null)
                    continue;

                Commands.SetGraphicsResourceSet((uint)layout.Set, buffer.GetResourceSet(layout.Layout));
            }

            Commands.DrawIndexed((uint)indicesCount, 1, (uint)indexStart, 0, 0);
        }

        /// <summary>
        /// Checks whether the given frame buffer is currently bound.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to check.</param>
        public bool IsFrameBufferBound(IFrameBuffer frameBuffer) => FrameBuffer == frameBuffer;

        /// <summary>
        /// Deletes a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to delete.</param>
        public void DeleteFrameBuffer(VeldridFrameBuffer frameBuffer)
        {
            while (FrameBuffer == frameBuffer)
                UnbindFrameBuffer(frameBuffer);

            frameBuffer.DeleteResources(true);
        }

        private readonly Dictionary<GraphicsPipelineDescription, Pipeline> pipelineCache = new Dictionary<GraphicsPipelineDescription, Pipeline>();

        private Pipeline getPipelineInstance()
        {
            if (!pipelineCache.TryGetValue(pipeline, out var instance))
            {
                pipelineCache[pipeline.Clone()] = instance = Factory.CreateGraphicsPipeline(ref pipeline);
                stat_graphics_pipeline_created.Value++;
            }

            return instance;
        }

        protected internal override unsafe Image<Rgba32> TakeScreenshot()
        {
            var texture = Device.SwapchainFramebuffer.ColorTargets[0].Target;

            switch (graphicsSurface.Type)
            {
                // Veldrid doesn't support copying content from a swapchain framebuffer texture on OpenGL.
                // OpenGL already provides a method for reading pixels directly from the active framebuffer, so let's just use that for now.
                case GraphicsSurfaceType.OpenGL:
                {
                    var pixelData = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<Rgba32>((int)(texture.Width * texture.Height));

                    var info = Device.GetOpenGLInfo();

                    info.ExecuteOnGLThread(() =>
                    {
                        fixed (Rgba32* data = pixelData.Memory.Span)
                            OpenGLNative.glReadPixels(0, 0, texture.Width, texture.Height, GLPixelFormat.Rgba, GLPixelType.UnsignedByte, data);
                    });

                    var glImage = Image.LoadPixelData<Rgba32>(pixelData.Memory.Span, (int)texture.Width, (int)texture.Height);
                    glImage.Mutate(i => i.Flip(FlipMode.Vertical));
                    return glImage;
                }

                default:
                {
                    uint width = texture.Width;
                    uint height = texture.Height;

                    using var staging = Factory.CreateTexture(TextureDescription.Texture2D(width, height, 1, 1, texture.Format, TextureUsage.Staging));
                    using var commands = Factory.CreateCommandList();
                    using var fence = Factory.CreateFence(false);

                    commands.Begin();
                    commands.CopyTexture(texture, staging);
                    commands.End();
                    Device.SubmitCommands(commands, fence);
                    Device.WaitForFence(fence);

                    var resource = Device.Map(staging, MapMode.Read);
                    var span = new Span<Bgra32>(resource.Data.ToPointer(), (int)(resource.SizeInBytes / Marshal.SizeOf<Bgra32>()));

                    // on some backends (Direct3D11, in particular), the staging resource data may contain padding at the end of each row for alignment,
                    // which means that for the image width, we cannot use the framebuffer's width raw.
                    using var image = Image.LoadPixelData<Bgra32>(span, (int)(resource.RowPitch / Marshal.SizeOf<Bgra32>()), (int)height);

                    if (!Device.IsUvOriginTopLeft)
                        image.Mutate(i => i.Flip(FlipMode.Vertical));

                    // if the image width doesn't match the framebuffer, it means that we still have padding at the end of each row mentioned above to get rid of.
                    // snip it to get a clean image.
                    if (image.Width != width)
                        image.Mutate(i => i.Crop((int)width, (int)height));

                    Device.Unmap(staging);

                    return image.CloneAs<Rgba32>();
                }
            }
        }

        protected override IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType)
            => new VeldridShaderPart(rawData, partType, store);

        protected override IShader CreateShader(string name, IShaderPart[] parts, IUniformBuffer<GlobalUniformData> globalUniformBuffer, ShaderCompilationStore compilationStore)
            => new VeldridShader(this, name, parts.Cast<VeldridShaderPart>().ToArray(), globalUniformBuffer, compilationStore);

        public override IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
            => new VeldridFrameBuffer(this, renderBufferFormats?.ToPixelFormats(), filteringMode.ToSamplerFilter());

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, Rendering.PrimitiveTopology primitiveType)
        {
            // maxBuffers is ignored because batches are not allowed to wrap around in Veldrid.
            return new VeldridLinearBatch<TVertex>(this, size, primitiveType.ToPrimitiveTopology());
        }

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers)
        {
            // maxBuffers is ignored because batches are not allowed to wrap around in Veldrid.
            return new VeldridQuadBatch<TVertex>(this, size);
        }

        protected override IUniformBuffer<TData> CreateUniformBuffer<TData>()
            => new VeldridUniformBuffer<TData>(this);

        protected override INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Color4? initialisationColour = null)
            => new VeldridTexture(this, width, height, manualMipmaps, filteringMode.ToSamplerFilter(), initialisationColour);

        protected override INativeTexture CreateNativeVideoTexture(int width, int height)
            => new VeldridVideoTexture(this, width, height);

        internal IStagingBuffer<T> CreateStagingBuffer<T>(uint count)
            where T : unmanaged
        {
            switch (FrameworkEnvironment.StagingBufferType)
            {
                case 0:
                    return new ManagedStagingBuffer<T>(this, count);

                case 1:
                    return new PersistentStagingBuffer<T>(this, count);

                case 2:
                    return new DeferredStagingBuffer<T>(this, count);

                default:
                    switch (Device.BackendType)
                    {
                        case GraphicsBackend.Direct3D11:
                        case GraphicsBackend.Vulkan:
                            return new PersistentStagingBuffer<T>(this, count);

                        default:
                        // Metal uses a more optimal path that elides a Blit Command Encoder.
                        case GraphicsBackend.Metal:
                        // OpenGL backends need additional work to support coherency and persistently mapped buffers.
                        case GraphicsBackend.OpenGL:
                        case GraphicsBackend.OpenGLES:
                            return new ManagedStagingBuffer<T>(this, count);
                    }
            }
        }

        protected override void SetUniformImplementation<T>(IUniformWithValue<T> uniform)
        {
        }

        public void RegisterUniformBufferForReset(IVeldridUniformBuffer buffer)
        {
            uniformBufferResetList.Add(buffer);
        }

        public void BindTextureResource(VeldridTextureResources resource, int unit) => boundTextureUnits[unit] = resource;
    }
}
