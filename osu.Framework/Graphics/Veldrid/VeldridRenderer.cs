// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using osu.Framework.Development;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Veldrid.Batches;
using osu.Framework.Platform;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Buffers.Staging;
using osu.Framework.Graphics.Veldrid.Shaders;
using osu.Framework.Graphics.Veldrid.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Statistics;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Veldrid;
using Veldrid.OpenGL;
using Veldrid.OpenGLBinding;
using Veldrid.SPIRV;
using Vortice.Direct3D;
using BufferUsage = Veldrid.BufferUsage;
using FramebufferTarget = Veldrid.OpenGLBinding.FramebufferTarget;
using Image = SixLabors.ImageSharp.Image;
using PixelFormat = Veldrid.PixelFormat;
using PrimitiveTopology = Veldrid.PrimitiveTopology;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;

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

        /// <summary>
        /// Represents the <see cref="Renderer.FrameIndex"/> of the latest frame that has completed rendering by the GPU.
        /// </summary>
        public ulong LatestCompletedFrameIndex { get; private set; }

        public GraphicsDevice Device { get; private set; } = null!;

        public ResourceFactory Factory => Device.ResourceFactory;

        public CommandList Commands { get; private set; } = null!;
        public CommandList BufferUpdateCommands { get; private set; } = null!;

        public CommandList MipmapGenerationCommands { get; private set; } = null!;

        private readonly ConcurrentDictionary<VeldridTexture, List<RectangleI>> computeMipmapGenerationQueue = new ConcurrentDictionary<VeldridTexture, List<RectangleI>>();

        private VeldridShader renderMipmapShader = null!;

        private Shader? computeMipmapShader;
        private Pipeline? computeMipmapPipeline;
        private ResourceLayout? computeMipmapTextureResourceLayout;
        private ResourceLayout? computeMipmapBufferResourceLayout;
        private VeldridUniformBuffer<ComputeMipmapGenerationParameters>? computeMipmapParametersBuffer;

        private bool supportsComputeMipmapGeneration =>
            // Requires GPU device to support compute shaders (not supported on old OpenGL versions).
            Device.Features.ComputeShader &&
            // Requires GPU device to support creating texture views for each mipmap level (not supported on old OpenGL versions).
            Device.Features.SubsetTextureView &&
            // On Direct3D 11, requires feature level 11_0+ for binding output textures as read-write UAV resources and for larger thread group sizes.
            // See https://learn.microsoft.com/en-us/windows/win32/direct3d11/direct3d-11-advanced-stages-compute-shader#using-compute-shader-on-direct3d-11x-hardware for more information.
            (Device.BackendType != GraphicsBackend.Direct3D11 || Device.GetD3D11Info().FeatureLevel >= FeatureLevel.Level_11_0);

        /// <summary>
        /// The number of threads in a thread group for the mipmap compute shader.
        /// </summary>
        /// <remarks>
        /// This defines 1024 threads per thread group, which is the limit on Metal and Direct3D 11.
        /// This should always match with the values specified in the compute shader.
        /// todo: add validation logic at some point.
        /// </remarks>
        private static readonly Vector2I compute_mipmap_threads = new Vector2I(32, 32);

        public CommandList TextureUpdateCommands { get; private set; } = null!;

        private bool beganTextureUpdateCommands;

        /// <summary>
        /// A list of fences which tracks in-flight frames for the purpose of knowing the last completed frame.
        /// This is tracked for the purpose of exposing <see cref="LatestCompletedFrameIndex"/>.
        /// </summary>
        private readonly List<FrameCompletionFence> pendingFramesFences = new List<FrameCompletionFence>();

        /// <summary>
        /// We are using fences every frame. Construction can be expensive, so let's pool some.
        /// </summary>
        private readonly Queue<Fence> perFrameFencePool = new Queue<Fence>();

        private VeldridIndexBuffer? linearIndexBuffer;
        private VeldridIndexBuffer? quadIndexBuffer;

        private readonly VeldridStagingTexturePool stagingTexturePool;

        private readonly HashSet<IVeldridUniformBuffer> uniformBufferResetList = new HashSet<IVeldridUniformBuffer>();

        private readonly Dictionary<int, VeldridTextureResources> boundTextureUnits = new Dictionary<int, VeldridTextureResources>();
        private readonly Dictionary<string, IVeldridUniformBuffer> boundUniformBuffers = new Dictionary<string, IVeldridUniformBuffer>();
        private IGraphicsSurface graphicsSurface = null!;

        private IVertexBuffer? boundVertexBuffer;
        private VeldridIndexBuffer? boundIndexBuffer;

        private GraphicsPipelineDescription pipeline = new GraphicsPipelineDescription
        {
            RasterizerState = RasterizerStateDescription.CullNone,
            BlendState = BlendStateDescription.SingleOverrideBlend,
            ShaderSet = { VertexLayouts = new VertexLayoutDescription[1] }
        };

        private static readonly GlobalStatistic<int> stat_graphics_pipeline_created = GlobalStatistics.Get<int>(nameof(VeldridRenderer), "Total pipelines created");

        public VeldridRenderer()
        {
            stagingTexturePool = new VeldridStagingTexturePool(this);
        }

        protected override void InitialiseDevice(IGraphicsSurface graphicsSurface)
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
        }

        protected override void SetupResources()
        {
            base.SetupResources();

            Commands = Factory.CreateCommandList();
            BufferUpdateCommands = Factory.CreateCommandList();
            MipmapGenerationCommands = Factory.CreateCommandList();
            TextureUpdateCommands = Factory.CreateCommandList();

            pipeline.Outputs = Device.SwapchainFramebuffer.OutputDescription;

            using (var store = new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), @"Resources/Shaders"))
            {
                if (supportsComputeMipmapGeneration)
                {
                    var shaderLines = Encoding.UTF8.GetString(store.Get("sh_mipmap.comp")).Split(Environment.NewLine).ToList();
                    shaderLines.Insert(1, $"#define {graphicsSurface.Type.ToString().ToUpperInvariant()}");
                    computeMipmapShader = Factory.CreateFromSpirv(new ShaderDescription(ShaderStages.Compute, Encoding.UTF8.GetBytes(string.Join(Environment.NewLine, shaderLines)), "main"));

                    var resourceLayouts = new List<ResourceLayout>();

                    resourceLayouts.Add(computeMipmapTextureResourceLayout = Factory.CreateResourceLayout(new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription("inputTexture", ResourceKind.TextureReadOnly, ShaderStages.Compute),
                        new ResourceLayoutElementDescription("inputSampler", ResourceKind.Sampler, ShaderStages.Compute),
                        new ResourceLayoutElementDescription("outputTexture", ResourceKind.TextureReadWrite, ShaderStages.Compute))));

                    resourceLayouts.Add(computeMipmapBufferResourceLayout = Factory.CreateResourceLayout(new ResourceLayoutDescription(
                        new ResourceLayoutElementDescription("parameters", ResourceKind.UniformBuffer, ShaderStages.Compute))));

                    computeMipmapPipeline = Factory.CreateComputePipeline(new ComputePipelineDescription
                    {
                        ComputeShader = computeMipmapShader,
                        ResourceLayouts = resourceLayouts.ToArray(),
                        ThreadGroupSizeX = (uint)compute_mipmap_threads.X,
                        ThreadGroupSizeY = (uint)compute_mipmap_threads.Y,
                        ThreadGroupSizeZ = 1,
                    });

                    computeMipmapParametersBuffer = new VeldridUniformBuffer<ComputeMipmapGenerationParameters>(this);
                }
                else
                {
                    var shaderStore = new PassthroughShaderStore(store);

                    renderMipmapShader = new VeldridShader(this, "mipmap/mipmap", new[]
                    {
                        new VeldridShaderPart(store.Get("sh_mipmap.vs"), ShaderPartType.Vertex, shaderStore),
                        new VeldridShaderPart(store.Get("sh_mipmap.fs"), ShaderPartType.Fragment, shaderStore),
                    }, GlobalUniformBuffer!);
                }
            }
        }

        private Vector2 currentSize;

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            updateLastCompletedFrameIndex();

            if (windowSize != currentSize)
            {
                Device.ResizeMainWindow((uint)windowSize.X, (uint)windowSize.Y);
                currentSize = windowSize;
            }

            foreach (var ubo in uniformBufferResetList)
                ubo.ResetCounters();
            uniformBufferResetList.Clear();

            stagingTexturePool.NewFrame();

            Commands.Begin();
            BufferUpdateCommands.Begin();

            base.BeginFrame(windowSize);
        }

        private void updateLastCompletedFrameIndex()
        {
            int? lastSignalledFenceIndex = null;

            // We have a sequential list of all fences which are in flight.
            // Frame usages are assumed to be sequential and linear.
            //
            // Iterate backwards to find the last signalled fence, which can be considered the last completed frame index.
            for (int i = pendingFramesFences.Count - 1; i >= 0; i--)
            {
                var fence = pendingFramesFences[i];

                if (!fence.Fence.Signaled)
                {
                    // this rule is broken on metal, if a new command buffer has been submitted while a previous fence wasn't signalled yet,
                    // then the previous fence will be thrown away and will never be signalled. keep iterating regardless of signal on metal.
                    if (graphicsSurface.Type != GraphicsSurfaceType.Metal)
                        Debug.Assert(lastSignalledFenceIndex == null, "A non-signalled fence was detected before the latest signalled frame.");

                    continue;
                }

                lastSignalledFenceIndex ??= i;

                Device.ResetFence(fence.Fence);
                perFrameFencePool.Enqueue(fence.Fence);
            }

            if (lastSignalledFenceIndex != null)
            {
                ulong frameIndex = pendingFramesFences[lastSignalledFenceIndex.Value].FrameIndex;

                Debug.Assert(frameIndex > LatestCompletedFrameIndex);
                LatestCompletedFrameIndex = frameIndex;

                pendingFramesFences.RemoveRange(0, lastSignalledFenceIndex.Value + 1);
            }

            Debug.Assert(pendingFramesFences.Count < 16, "Completion frame fence leak detected");
        }

        protected internal override void FinishFrame()
        {
            base.FinishFrame();

            MipmapGenerationCommands.Begin();

            var textures = computeMipmapGenerationQueue.Keys.ToArray();

            foreach (var texture in textures)
            {
                computeMipmapGenerationQueue.TryRemove(texture, out var regions);
                generateMipmapsViaComputeShader(texture, regions!);
            }
            flushTextureUploadCommands();

            BufferUpdateCommands.End();
            Device.SubmitCommands(BufferUpdateCommands);

            MipmapGenerationCommands.End();
            Device.SubmitCommands(MipmapGenerationCommands);
            // This is returned via the end-of-lifetime tracking in `pendingFrameFences`.
            // See `updateLastCompletedFrameIndex`.
            if (!perFrameFencePool.TryDequeue(out Fence? fence))
                fence = Factory.CreateFence(false);

            Commands.End();
            Device.SubmitCommands(Commands, fence);

            pendingFramesFences.Add(new FrameCompletionFence(fence, FrameIndex));
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
                bindTextureResource(resources[i], unit++);

            return true;
        }

        private void bindTextureResource(VeldridTextureResources resource, int unit) => boundTextureUnits[unit] = resource;

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
            ensureTextureUploadCommandsBegan();

            // This code is doing the same as the simpler approach of:
            //
            // Device.UpdateTexture(texture, data, (uint)x, (uint)y, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            //
            // Except we are using a staging texture pool to avoid the alloc overhead of each staging texture.
            var staging = stagingTexturePool.Get(width, height, texture.Format);
            Device.UpdateTexture(staging, data, 0, 0, 0, (uint)width, (uint)height, 1, (uint)level, 0);
            TextureUpdateCommands.CopyTexture(staging, 0, 0, 0, 0, 0, texture, (uint)x, (uint)y, 0, (uint)level, 0, (uint)width, (uint)height, 1, 1);
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
            var staging = stagingTexturePool.Get(width, height, texture.Format);

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

        /// <summary>
        /// Whether calling <see cref="GenerateMipmaps"/> should process enqueuing commands immediately rather than queuing it until end of frame.
        /// This will also block <see cref="GenerateMipmaps"/> until GPU processes all sent commands.
        /// </summary>
        /// <remarks>
        /// This is used in framework tests for profiling mipmap generation performance.
        /// </remarks>
        public bool BypassMipmapGenerationQueue { get; set; }

        public override void GenerateMipmaps(INativeTexture texture, List<RectangleI> regions)
        {
            var veldridTexture = (VeldridTexture)texture;

            if (!supportsComputeMipmapGeneration)
            {
                generateMipmapsViaFramebuffer(veldridTexture, regions);
                return;
            }

            if (BypassMipmapGenerationQueue)
                generateMipmapsViaComputeShader(veldridTexture, regions);
            else
            {
                computeMipmapGenerationQueue.AddOrUpdate(veldridTexture,
                    _ => regions,
                    (_, existing) =>
                    {
                        existing.AddRange(regions);
                        return existing;
                    });
            }
        }

        private void generateMipmapsViaComputeShader(VeldridTexture texture, List<RectangleI> regions)
        {
            computeMipmapParametersBuffer ??= new VeldridUniformBuffer<ComputeMipmapGenerationParameters>(this);

            var textureResources = texture.GetResourceList().Single().AsNonNull();

            int width = texture.Width;
            int height = texture.Height;

            MipmapGenerationCommands.SetPipeline(computeMipmapPipeline);

            for (int level = 1; level < IRenderer.MAX_MIPMAP_LEVELS + 1 && (width > 1 || height > 1); level++)
            {
                width /= 2;
                height /= 2;

                MipmapGenerationCommands.SetComputeResourceSet(0, textureResources.GetMipmapResourceSet(this, computeMipmapTextureResourceLayout.AsNonNull(), level));

                for (int i = 0; i < regions.Count; i++)
                {
                    Vector2I topLeft = regions[i].TopLeft;
                    topLeft = new Vector2I(topLeft.X / 2, topLeft.Y / 2);
                    Vector2I bottomRight = regions[i].BottomRight;
                    bottomRight = new Vector2I(MathUtils.DivideRoundUp(bottomRight.X, 2), MathUtils.DivideRoundUp(bottomRight.Y, 2));
                    regions[i] = new RectangleI(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

                    uint groupCountX = (uint)MathUtils.DivideRoundUp(regions[i].Width, compute_mipmap_threads.X);
                    uint groupCountY = (uint)MathUtils.DivideRoundUp(regions[i].Height, compute_mipmap_threads.Y);

                    computeMipmapParametersBuffer!.Data = new ComputeMipmapGenerationParameters
                    {
                        Region = new Vector4(regions[i].X, regions[i].Y, regions[i].Width, regions[i].Height),
                        OutputWidth = width,
                    };

                    MipmapGenerationCommands.SetComputeResourceSet(1, computeMipmapParametersBuffer.GetResourceSet(computeMipmapBufferResourceLayout.AsNonNull()));
                    MipmapGenerationCommands.Dispatch(groupCountX, groupCountY, 1);
                }
            }
        }

        private void generateMipmapsViaFramebuffer(VeldridTexture texture, List<RectangleI> regions)
        {
            Debug.Assert(graphicsSurface.Type == GraphicsSurfaceType.OpenGL);

            var deviceTexture = texture.GetResourceList().Single().Texture;

            BlendingParameters previousBlendingParameters = CurrentBlendingParameters;

            SetBlend(BlendingParameters.None);
            PushDepthInfo(new DepthInfo(false, false));
            PushStencilInfo(new StencilInfo(false));
            PushScissorState(false);

            // Create render state for mipmap generation
            BindTexture(texture);
            renderMipmapShader.Bind();

            int width = texture.Width;
            int height = texture.Height;

            // Generate quad buffer that will hold all the updated regions
            var quadBuffer = new VeldridQuadBuffer<UncolouredVertex2D>(this, regions.Count, BufferUsage.Dynamic);

            // Compute mipmap by iteratively blitting coarser and coarser versions of the updated regions
            for (int level = 1; level < IRenderer.MAX_MIPMAP_LEVELS + 1 && (width > 1 || height > 1); ++level)
            {
                width /= 2;
                height /= 2;

                // Fill quad buffer with downscaled (and conservatively rounded) draw rectangles
                for (int i = 0; i < regions.Count; ++i)
                {
                    // Conservatively round the draw rectangles. Rounding to integer coords is required
                    // in order to ensure all the texels affected by linear interpolation are touched.
                    // We could skip the rounding & use a single vertex buffer for all levels if we had
                    // conservative raster, but alas, that's only supported on NV and Intel.
                    Vector2I topLeft = regions[i].TopLeft;
                    topLeft = new Vector2I(topLeft.X / 2, topLeft.Y / 2);
                    Vector2I bottomRight = regions[i].BottomRight;
                    bottomRight = new Vector2I(MathUtils.DivideRoundUp(bottomRight.X, 2), MathUtils.DivideRoundUp(bottomRight.Y, 2));
                    regions[i] = new RectangleI(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y);

                    // Normalize the draw rectangle into the unit square, which doubles as texture sampler coordinates.
                    RectangleF r = (RectangleF)regions[i] / new Vector2(width, height);

                    quadBuffer.SetVertex(i * 4 + 0, new UncolouredVertex2D { Position = r.BottomLeft });
                    quadBuffer.SetVertex(i * 4 + 1, new UncolouredVertex2D { Position = r.BottomRight });
                    quadBuffer.SetVertex(i * 4 + 2, new UncolouredVertex2D { Position = r.TopRight });
                    quadBuffer.SetVertex(i * 4 + 3, new UncolouredVertex2D { Position = r.TopLeft });
                }

                // Read the texture from 1 mip level higher...
                var samplerDescription = new SamplerDescription
                {
                    AddressModeU = SamplerAddressMode.Clamp,
                    AddressModeV = SamplerAddressMode.Clamp,
                    AddressModeW = SamplerAddressMode.Clamp,
                    Filter = SamplerFilter.MinLinear_MagLinear_MipLinear,
                    MinimumLod = (uint)level - 1,
                    MaximumLod = (uint)level - 1,
                    MaximumAnisotropy = 0,
                };

                using (var sampler = Factory.CreateSampler(ref samplerDescription))
                using (var resources = new VeldridTextureResources(deviceTexture, sampler, this, false))
                {
                    // ...than the one we're writing to via frame buffer.
                    using var frameBuffer = new VeldridFrameBuffer(this, texture, level);

                    BindFrameBuffer(frameBuffer);
                    bindTextureResource(resources, 0);

                    // Perform the actual mip level draw
                    PushViewport(new RectangleI(0, 0, width, height));

                    quadBuffer.Update();
                    quadBuffer.Draw();

                    PopViewport();

                    UnbindFrameBuffer(frameBuffer);
                }
            }

            // Restore previous render state
            renderMipmapShader.Unbind();

            PopScissorState();
            PopStencilInfo();
            PopDepthInfo();

            SetBlend(previousBlendingParameters);
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

        public void BindVertexBuffer<T>(IVeldridVertexBuffer<T> buffer)
            where T : unmanaged, IEquatable<T>, IVertex
        {
            if (buffer == boundVertexBuffer)
                return;

            Commands.SetVertexBuffer(0, buffer.Buffer);
            pipeline.ShaderSet.VertexLayouts[0] = IVeldridVertexBuffer<T>.LAYOUT;

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            boundVertexBuffer = buffer;
        }

        public void BindIndexBuffer(VeldridIndexLayout layout, int verticesCount)
        {
            ref var indexBuffer = ref layout == VeldridIndexLayout.Quad
                ? ref quadIndexBuffer
                : ref linearIndexBuffer;

            if (indexBuffer == null || indexBuffer.VertexCapacity < verticesCount)
            {
                indexBuffer?.Dispose();
                indexBuffer = new VeldridIndexBuffer(this, layout, verticesCount);
            }

            Commands.SetIndexBuffer(indexBuffer.Buffer, VeldridIndexBuffer.FORMAT);
            boundIndexBuffer = indexBuffer;
        }

        public void BindUniformBuffer(string blockName, IVeldridUniformBuffer veldridBuffer)
        {
            FlushCurrentBatch(FlushBatchSource.BindBuffer);
            boundUniformBuffers[blockName] = veldridBuffer;
        }

        public void DrawVertices(PrimitiveTopology type, int vertexStart, int verticesCount)
        {
            // normally we would flush/submit all texture upload commands at the end of the frame, since no actual rendering by the GPU will happen until then,
            // but turns out on macOS with non-apple GPU, this results in rendering corruption.
            // flushing the texture upload commands here before a draw call fixes the corruption, and there's no explanation as to why that's the case,
            // but there is nothing to be lost in flushing here except for a frame that contains many sprites with Texture.BypassTextureUploadQueue = true.
            // until that appears to be problem, let's just flush here.
            flushTextureUploadCommands();

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

            Debug.Assert(boundIndexBuffer != null);

            int indexStart = boundIndexBuffer.TranslateToIndex(vertexStart);
            int indicesCount = boundIndexBuffer.TranslateToIndex(verticesCount);
            Commands.DrawIndexed((uint)indicesCount, 1, (uint)indexStart, 0, 0);
        }

        private void ensureTextureUploadCommandsBegan()
        {
            if (beganTextureUpdateCommands)
                return;

            TextureUpdateCommands.Begin();
            beganTextureUpdateCommands = true;
        }

        private void flushTextureUploadCommands()
        {
            if (!beganTextureUpdateCommands)
                return;

            TextureUpdateCommands.End();
            Device.SubmitCommands(TextureUpdateCommands);

            beganTextureUpdateCommands = false;
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

                    if (!waitForFence(fence, 5000))
                    {
                        Logger.Log("Failed to capture swapchain framebuffer content within reasonable time.", level: LogLevel.Important);
                        return new Image<Rgba32>((int)width, (int)height);
                    }

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

        private bool waitForFence(Fence fence, int millisecondsTimeout)
        {
            // todo: Metal doesn't support WaitForFence due to lack of implementation and bugs with supporting MTLSharedEvent.notifyListener,
            // until that is fixed in some way or another, poll on the signal state.
            if (graphicsSurface.Type == GraphicsSurfaceType.Metal)
            {
                const int sleep_time = 10;

                while (!fence.Signaled && (millisecondsTimeout -= sleep_time) > 0)
                    Thread.Sleep(sleep_time);

                return fence.Signaled;
            }

            return Device.WaitForFence(fence, (ulong)(millisecondsTimeout * 1_000_000));
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

        private record struct FrameCompletionFence(Fence Fence, ulong FrameIndex);
    }
}
