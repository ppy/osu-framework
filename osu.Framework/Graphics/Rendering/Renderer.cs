// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Lists;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using RectangleF = osu.Framework.Graphics.Primitives.RectangleF;
using Texture = osu.Framework.Graphics.Textures.Texture;

namespace osu.Framework.Graphics.Rendering
{
    /// <summary>
    /// Represents a base <see cref="IRenderer"/> implementation for working renderers.
    /// </summary>
    public abstract class Renderer : IRenderer
    {
        /// <summary>
        /// The length of no usage (in frames) before freeing unused resources.
        /// </summary>
        internal const int RESOURCE_FREE_NO_USAGE_LENGTH = 300;

        protected internal abstract bool VerticalSync { get; set; }
        protected internal abstract bool AllowTearing { get; set; }

        protected internal Storage? CacheStorage
        {
            set => shaderCompilationStore.CacheStorage = value;
        }

        public int MaxTextureSize { get; protected set; } = 4096; // default value is to allow roughly normal flow in cases we don't have graphics context, like headless CI.

        public int MaxTexturesUploadedPerFrame { get; set; } = 32;
        public int MaxPixelsUploadedPerFrame { get; set; } = 1024 * 1024 * 2;

        public abstract bool IsDepthRangeZeroToOne { get; }
        public abstract bool IsUvOriginTopLeft { get; }
        public abstract bool IsClipSpaceYInverted { get; }

        public ulong FrameIndex { get; private set; }

        public ref readonly MaskingInfo CurrentMaskingInfo => ref currentMaskingInfo;

        public RectangleI Viewport { get; private set; }
        public RectangleI Scissor { get; private set; }
        public Vector2I ScissorOffset { get; private set; }
        public Matrix4 ProjectionMatrix { get; private set; }
        public DepthInfo CurrentDepthInfo { get; private set; }
        public StencilInfo CurrentStencilInfo { get; private set; }
        public WrapMode CurrentWrapModeS { get; private set; }
        public WrapMode CurrentWrapModeT { get; private set; }
        public bool IsMaskingActive { get; private set; }
        public bool UsingBackbuffer { get; private set; }
        public Texture WhitePixel => whitePixel.Value;
        DepthValue IRenderer.BackbufferDepth => backBufferDepth;

        public bool IsInitialised { get; private set; }

        protected ClearInfo CurrentClearInfo { get; private set; }
        public BlendingParameters CurrentBlendingParameters { get; private set; }
        protected BlendingMask CurrentBlendingMask { get; private set; }

        /// <summary>
        /// Whether scissor is currently enabled.
        /// </summary>
        protected bool ScissorState { get; private set; }

        /// <summary>
        /// The current framebuffer, or null if the backbuffer is used.
        /// </summary>
        protected IFrameBuffer? FrameBuffer { get; private set; }

        /// <summary>
        /// The current shader, or null if no shader is currently bound.
        /// </summary>
        protected IShader? Shader { get; private set; }

        private readonly ShaderCompilationStore shaderCompilationStore = new ShaderCompilationStore();

        private readonly GlobalStatistic<int> statExpensiveOperationsQueued;
        private readonly GlobalStatistic<int> statTextureUploadsQueued;
        private readonly GlobalStatistic<int> statTextureUploadsDequeued;
        private readonly GlobalStatistic<int> statTextureUploadsPerformed;
        private readonly GlobalStatistic<int> vboInUse;

        private readonly ConcurrentQueue<ScheduledDelegate> expensiveOperationQueue = new ConcurrentQueue<ScheduledDelegate>();
        private readonly ConcurrentQueue<INativeTexture> textureUploadQueue = new ConcurrentQueue<INativeTexture>();
        private readonly RendererDisposalQueue disposalQueue = new RendererDisposalQueue();

        private readonly Scheduler resetScheduler = new Scheduler(() => ThreadSafety.IsDrawThread, new StopwatchClock(true)); // force no thread set until we are actually on the draw thread.

        private readonly DepthValue backBufferDepth = new DepthValue();

        private readonly Dictionary<string, IUniformBuffer> boundUniformBuffers = new Dictionary<string, IUniformBuffer>();
        private readonly Stack<IVertexBatch<TexturedVertex2D>> quadBatches = new Stack<IVertexBatch<TexturedVertex2D>>();
        private readonly List<IVertexBuffer> vertexBuffersInUse = new List<IVertexBuffer>();
        private readonly List<IVertexBatch> batchResetList = new List<IVertexBatch>();
        private readonly Stack<RectangleI> viewportStack = new Stack<RectangleI>();
        private readonly Stack<Matrix4> projectionMatrixStack = new Stack<Matrix4>();
        private readonly Stack<MaskingInfo> maskingStack = new Stack<MaskingInfo>();
        private readonly Stack<RectangleI> scissorRectStack = new Stack<RectangleI>();
        private readonly Stack<DepthInfo> depthStack = new Stack<DepthInfo>();
        private readonly Stack<StencilInfo> stencilStack = new Stack<StencilInfo>();
        private readonly Stack<Vector2I> scissorOffsetStack = new Stack<Vector2I>();
        private readonly Stack<IFrameBuffer> frameBufferStack = new Stack<IFrameBuffer>();
        private readonly Stack<IShader> shaderStack = new Stack<IShader>();
        private readonly Stack<bool> scissorStateStack = new Stack<bool>();

        private readonly INativeTexture?[] lastBoundTexture = new INativeTexture?[16];
        private readonly bool[] lastBoundTextureIsAtlas = new bool[16];

        // in case no other textures are used in the project, create a new atlas as a fallback source for the white pixel area (used to draw boxes etc.)
        private readonly Lazy<TextureWhitePixel> whitePixel;
        private readonly LockedWeakList<Texture> allTextures = new LockedWeakList<Texture>();

        private IUniformBuffer<GlobalUniformData>? globalUniformBuffer;
        private IVertexBatch<TexturedVertex2D>? defaultQuadBatch;
        private IVertexBatch? currentActiveBatch;
        private MaskingInfo currentMaskingInfo;
        private int lastActiveTextureUnit;
        private bool globalUniformsChanged;

        private static readonly GlobalStatistic<int>[] flush_source_statistics;

        static Renderer()
        {
            var sources = Enum.GetValues<FlushBatchSource>();

            flush_source_statistics = new GlobalStatistic<int>[sources.Length];
            foreach (FlushBatchSource source in sources)
                flush_source_statistics[(int)source] = GlobalStatistics.Get<int>(nameof(FlushBatchSource), source.ToString());
        }

        protected Renderer()
        {
            statTextureUploadsPerformed = GlobalStatistics.Get<int>(GetType().Name, "Texture uploads performed");
            statTextureUploadsDequeued = GlobalStatistics.Get<int>(GetType().Name, "Texture uploads dequeued");
            statTextureUploadsQueued = GlobalStatistics.Get<int>(GetType().Name, "Texture upload queue length");
            statExpensiveOperationsQueued = GlobalStatistics.Get<int>(GetType().Name, "Expensive operation queue length");
            vboInUse = GlobalStatistics.Get<int>(GetType().Name, "VBOs in use");

            whitePixel = new Lazy<TextureWhitePixel>(() =>
                new TextureAtlas(this, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, true).WhitePixel);
        }

        void IRenderer.Initialise(IGraphicsSurface graphicsSurface)
        {
            switch (graphicsSurface.Type)
            {
                case GraphicsSurfaceType.OpenGL:
                    Trace.Assert(graphicsSurface is IOpenGLGraphicsSurface, $"Window must implement {nameof(IOpenGLGraphicsSurface)}.");
                    break;

                case GraphicsSurfaceType.Metal:
                    Trace.Assert(graphicsSurface is IMetalGraphicsSurface, $"Window graphics API must implement {nameof(IMetalGraphicsSurface)}.");
                    break;
            }

            Initialise(graphicsSurface);

            defaultQuadBatch = CreateQuadBatch<TexturedVertex2D>(100, 1000);
            resetScheduler.AddDelayed(disposalQueue.CheckPendingDisposals, 0, true);

            IsInitialised = true;
        }

        /// <summary>
        /// Resets any states to prepare for drawing a new frame.
        /// </summary>
        /// <param name="windowSize">The full window size.</param>
        protected internal virtual void BeginFrame(Vector2 windowSize)
        {
            foreach (var source in flush_source_statistics)
                source.Value = 0;

            globalUniformBuffer ??= ((IRenderer)this).CreateUniformBuffer<GlobalUniformData>();

            Debug.Assert(defaultQuadBatch != null);

            FrameIndex++;

            backBufferDepth.Reset();

            resetScheduler.Update();

            statExpensiveOperationsQueued.Value = expensiveOperationQueue.Count;

            while (expensiveOperationQueue.TryDequeue(out ScheduledDelegate? operation))
            {
                if (operation.State == ScheduledDelegate.RunState.Waiting)
                {
                    operation.RunTask();
                    break;
                }
            }

            globalUniformsChanged = true;
            currentActiveBatch = null;
            CurrentBlendingParameters = new BlendingParameters();
            currentMaskingInfo = default;

            foreach (var b in batchResetList)
                b.ResetCounters();
            batchResetList.Clear();

            Shader?.Unbind();
            Shader = null;

            boundUniformBuffers.Clear();
            viewportStack.Clear();
            projectionMatrixStack.Clear();
            maskingStack.Clear();
            scissorRectStack.Clear();
            frameBufferStack.Clear();
            depthStack.Clear();
            stencilStack.Clear();
            scissorStateStack.Clear();
            scissorOffsetStack.Clear();
            shaderStack.Clear();

            quadBatches.Clear();
            quadBatches.Push(defaultQuadBatch);

            setFrameBuffer(null, true);

            Scissor = RectangleI.Empty;
            ScissorOffset = Vector2I.Zero;
            Viewport = RectangleI.Empty;
            ProjectionMatrix = Matrix4.Identity;

            PushScissorState(true);
            PushViewport(new RectangleI(0, 0, (int)windowSize.X, (int)windowSize.Y));
            PushScissor(new RectangleI(0, 0, (int)windowSize.X, (int)windowSize.Y));
            PushScissorOffset(Vector2I.Zero);
            PushMaskingInfo(new MaskingInfo
            {
                ScreenSpaceAABB = new RectangleI(0, 0, (int)windowSize.X, (int)windowSize.Y),
                MaskingRect = new RectangleF(0, 0, windowSize.X, windowSize.Y),
                ToMaskingSpace = Matrix3.Identity,
                BlendRange = 1,
                AlphaExponent = 1,
                CornerExponent = 2.5f,
            }, true);

            PushDepthInfo(DepthInfo.Default);
            PushStencilInfo(StencilInfo.Default);

            Clear(new ClearInfo(Color4.Black));

            freeUnusedVertexBuffers();
            vboInUse.Value = vertexBuffersInUse.Count;

            statTextureUploadsQueued.Value = textureUploadQueue.Count;
            statTextureUploadsDequeued.Value = 0;
            statTextureUploadsPerformed.Value = 0;

            // increase the number of items processed with the queue length to ensure it doesn't get out of hand.
            int targetUploads = Math.Clamp(textureUploadQueue.Count / 2, 1, MaxTexturesUploadedPerFrame);
            int uploads = 0;
            int uploadedPixels = 0;

            // continue attempting to upload textures until enough uploads have been performed.
            while (textureUploadQueue.TryDequeue(out INativeTexture? texture))
            {
                statTextureUploadsDequeued.Value++;

                if (!texture.Upload())
                    continue;

                statTextureUploadsPerformed.Value++;

                if (++uploads >= targetUploads)
                    break;

                if ((uploadedPixels += texture.Width * texture.Height) > MaxPixelsUploadedPerFrame)
                    break;
            }

            lastBoundTexture.AsSpan().Clear();
            lastBoundTextureIsAtlas.AsSpan().Clear();
        }

        /// <summary>
        /// Performs any last actions before a frame ends.
        /// </summary>
        protected internal virtual void FinishFrame()
        {
            FlushCurrentBatch(FlushBatchSource.FinishFrame);
        }

        public void ScheduleExpensiveOperation(ScheduledDelegate operation)
        {
            if (IsInitialised)
                expensiveOperationQueue.Enqueue(operation);
        }

        public void ScheduleDisposal<T>(Action<T> disposalAction, T target)
        {
            if (IsInitialised)
                disposalQueue.ScheduleDisposal(disposalAction, target);
            else
                disposalAction.Invoke(target);
        }

        /// <summary>
        /// Returns an image containing the current content of the backbuffer, i.e. takes a screenshot.
        /// </summary>
        protected internal abstract Image<Rgba32> TakeScreenshot();

        /// <summary>
        /// Returns an image containing the content of a framebuffer.
        /// </summary>
        protected internal virtual Image<Rgba32>? ExtractFrameBufferData(IFrameBuffer frameBuffer) => null;

        /// <summary>
        /// Performs a once-off initialisation of this <see cref="Renderer"/>.
        /// </summary>
        protected abstract void Initialise(IGraphicsSurface graphicsSurface);

        /// <summary>
        /// Swaps the back buffer with the front buffer to display the new frame.
        /// </summary>
        protected internal abstract void SwapBuffers();

        /// <summary>
        /// Waits until all renderer commands have been fully executed GPU-side, as signaled by the graphics backend.
        /// </summary>
        /// <remarks>
        /// This is equivalent to a <c>glFinish</c> call.
        /// </remarks>
        protected internal abstract void WaitUntilIdle();

        protected internal abstract void WaitUntilNextFrameReady();

        /// <summary>
        /// Invoked when the rendering thread is active and commands will be enqueued.
        /// This is mainly required for OpenGL renderers to mark context as current before performing GL calls.
        /// </summary>
        protected internal abstract void MakeCurrent();

        /// <summary>
        /// Invoked when the rendering thread is suspended and no more commands will be enqueued.
        /// This is mainly required for OpenGL renderers to mark context as current before performing GL calls.
        /// </summary>
        protected internal abstract void ClearCurrent();

        #region Clear

        public void Clear(ClearInfo clearInfo)
        {
            PushDepthInfo(new DepthInfo(writeDepth: true));
            PushScissorState(false);

            ClearImplementation(clearInfo);

            CurrentClearInfo = clearInfo;

            PopScissorState();
            PopDepthInfo();
        }

        /// <summary>
        /// Informs the graphics device to clear the color and depth targets of the currently bound framebuffer.
        /// </summary>
        /// <param name="clearInfo">The clear parameters.</param>
        protected abstract void ClearImplementation(ClearInfo clearInfo);

        #endregion

        #region Blending

        public void SetBlend(BlendingParameters blendingParameters)
        {
            if (CurrentBlendingParameters == blendingParameters)
                return;

            FlushCurrentBatch(FlushBatchSource.SetBlend);
            SetBlendImplementation(blendingParameters);

            CurrentBlendingParameters = blendingParameters;
        }

        public void SetBlendMask(BlendingMask blendingMask)
        {
            if (CurrentBlendingMask == blendingMask)
                return;

            FlushCurrentBatch(FlushBatchSource.SetBlendMask);
            SetBlendMaskImplementation(blendingMask);

            CurrentBlendingMask = blendingMask;
        }

        /// <summary>
        /// Updates the graphics device with the new blending parameters.
        /// </summary>
        /// <param name="blendingParameters">The blending parameters.</param>
        protected abstract void SetBlendImplementation(BlendingParameters blendingParameters);

        /// <summary>
        /// Updates the graphics device with the new blending mask.
        /// </summary>
        /// <param name="blendingMask">The blending mask.</param>
        protected abstract void SetBlendMaskImplementation(BlendingMask blendingMask);

        #endregion

        #region Viewport

        public void PushViewport(RectangleI viewport)
        {
            var actualRect = viewport;

            if (actualRect.Width < 0)
            {
                actualRect.X += viewport.Width;
                actualRect.Width = -viewport.Width;
            }

            if (actualRect.Height < 0)
            {
                actualRect.Y += viewport.Height;
                actualRect.Height = -viewport.Height;
            }

            this.PushOrtho(viewport);

            viewportStack.Push(actualRect);
            setViewport(viewport);
        }

        public void PopViewport()
        {
            Trace.Assert(viewportStack.Count > 1);

            PopProjectionMatrix();

            viewportStack.Pop();
            setViewport(viewportStack.Peek());
        }

        private void setViewport(RectangleI viewport)
        {
            if (Viewport == viewport)
                return;

            SetViewportImplementation(viewport);
            Viewport = viewport;
        }

        /// <summary>
        /// Updates the graphics device with a new viewport rectangle.
        /// </summary>
        /// <param name="viewport">The viewport to use.</param>
        protected abstract void SetViewportImplementation(RectangleI viewport);

        #endregion

        #region Scissor

        public void PushScissor(RectangleI scissor)
        {
            scissorRectStack.Push(scissor);
            setScissor(scissor);
        }

        public void PushScissorState(bool enabled)
        {
            scissorStateStack.Push(enabled);
            setScissorState(enabled);
        }

        public void PushScissorOffset(Vector2I offset)
        {
            scissorOffsetStack.Push(offset);
            setScissorOffset(offset);
        }

        public void PopScissor()
        {
            Trace.Assert(scissorRectStack.Count > 1);

            scissorRectStack.Pop();
            setScissor(scissorRectStack.Peek());
        }

        public void PopScissorState()
        {
            Trace.Assert(scissorStateStack.Count > 1);

            scissorStateStack.Pop();
            setScissorState(scissorStateStack.Peek());
        }

        public void PopScissorOffset()
        {
            Trace.Assert(scissorOffsetStack.Count > 1);

            scissorOffsetStack.Pop();
            setScissorOffset(scissorOffsetStack.Peek());
        }

        private void setScissor(RectangleI scissor)
        {
            if (scissor.Width < 0)
            {
                scissor.X += scissor.Width;
                scissor.Width = -scissor.Width;
            }

            if (scissor.Height < 0)
            {
                scissor.Y += scissor.Height;
                scissor.Height = -scissor.Height;
            }

            if (Scissor == scissor)
                return;

            // when rendering to a framebuffer on a backend which has the UV origin set to bottom-left (OpenGL),
            // vertex positions are flipped vertically in sh_Vertex_Output.h to match that UV origin.
            // for scissoring to continue to work correctly with that, the scissor box has to be inverted too.
            var compensatedScissor = scissor;
            if (!UsingBackbuffer && !IsUvOriginTopLeft)
                compensatedScissor.Y = Viewport.Height - scissor.Bottom;

            FlushCurrentBatch(FlushBatchSource.SetScissor);
            SetScissorImplementation(compensatedScissor);
            // do not expose the implementation detail of flipping the scissor box to Scissor readers.
            Scissor = scissor;
        }

        private void setScissorState(bool enabled)
        {
            if (enabled == ScissorState)
                return;

            FlushCurrentBatch(FlushBatchSource.SetScissor);
            SetScissorStateImplementation(enabled);
            ScissorState = enabled;
        }

        private void setScissorOffset(Vector2I offset)
        {
            if (ScissorOffset == offset)
                return;

            FlushCurrentBatch(FlushBatchSource.SetScissor);
            ScissorOffset = offset;
        }

        /// <summary>
        /// Updates the graphics device with a new scissor rectangle.
        /// </summary>
        /// <param name="scissor">The scissor rectangle to use.</param>
        protected abstract void SetScissorImplementation(RectangleI scissor);

        /// <summary>
        /// Updates the graphics device with the new scissor state.
        /// </summary>
        /// <param name="enabled">Whether scissor should be enabled.</param>
        protected abstract void SetScissorStateImplementation(bool enabled);

        #endregion

        #region Projection Matrix

        public void PushProjectionMatrix(Matrix4 matrix)
        {
            projectionMatrixStack.Push(matrix);
            setProjectionMatrix(matrix);
        }

        public void PopProjectionMatrix()
        {
            Trace.Assert(projectionMatrixStack.Count > 1);

            projectionMatrixStack.Pop();
            setProjectionMatrix(projectionMatrixStack.Peek());
        }

        private void setProjectionMatrix(Matrix4 matrix)
        {
            if (ProjectionMatrix == matrix)
                return;

            FlushCurrentBatch(FlushBatchSource.SetProjection);

            globalUniformsChanged = true;
            ProjectionMatrix = matrix;
        }

        #endregion

        #region Masking

        public void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false)
        {
            maskingStack.Push(maskingInfo);
            setMaskingInfo(maskingInfo, true, overwritePreviousScissor);
        }

        public void PopMaskingInfo()
        {
            Trace.Assert(maskingStack.Count > 1);

            maskingStack.Pop();
            setMaskingInfo(maskingStack.Peek(), false, true);
        }

        private void setMaskingInfo(MaskingInfo maskingInfo, bool isPushing, bool overwritePreviousScissor)
        {
            if (CurrentMaskingInfo == maskingInfo)
                return;

            FlushCurrentBatch(FlushBatchSource.SetMasking);

            if (isPushing)
            {
                // When drawing to a viewport that doesn't match the projection size (e.g. via framebuffers), the resultant image will be scaled
                Vector2 projectionScale = new Vector2(ProjectionMatrix.Row0.X / 2, -ProjectionMatrix.Row1.Y / 2);
                Vector2 viewportScale = Vector2.Multiply(Viewport.Size, projectionScale);

                Vector2 location = (maskingInfo.ScreenSpaceAABB.Location - ScissorOffset) * viewportScale;
                Vector2 size = maskingInfo.ScreenSpaceAABB.Size * viewportScale;

                RectangleI actualRect = new RectangleI(
                    (int)Math.Floor(location.X),
                    (int)Math.Floor(location.Y),
                    (int)Math.Ceiling(size.X),
                    (int)Math.Ceiling(size.Y));

                PushScissor(overwritePreviousScissor ? actualRect : RectangleI.Intersect(scissorRectStack.Peek(), actualRect));
            }
            else
                PopScissor();

            currentMaskingInfo = maskingInfo;

            // Masking is enabled for as long as any masking info is active that's not the default.
            IsMaskingActive = maskingStack.Count > 1;
            globalUniformsChanged = true;
        }

        #endregion

        #region Depth & Stencil

        public void PushDepthInfo(DepthInfo depthInfo)
        {
            depthStack.Push(depthInfo);
            setDepthInfo(depthInfo);
        }

        public void PushStencilInfo(StencilInfo stencilInfo)
        {
            stencilStack.Push(stencilInfo);
            setStencilInfo(stencilInfo);
        }

        public void PopDepthInfo()
        {
            Trace.Assert(depthStack.Count > 1);

            depthStack.Pop();
            setDepthInfo(depthStack.Peek());
        }

        public void PopStencilInfo()
        {
            Trace.Assert(stencilStack.Count > 1);

            stencilStack.Pop();
            setStencilInfo(stencilStack.Peek());
        }

        private void setDepthInfo(DepthInfo depthInfo)
        {
            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            FlushCurrentBatch(FlushBatchSource.SetDepthInfo);
            SetDepthInfoImplementation(depthInfo);

            CurrentDepthInfo = depthInfo;
        }

        private void setStencilInfo(StencilInfo stencilInfo)
        {
            if (CurrentStencilInfo.Equals(stencilInfo))
                return;

            FlushCurrentBatch(FlushBatchSource.SetStencilInfo);
            SetStencilInfoImplementation(stencilInfo);

            CurrentStencilInfo = stencilInfo;
        }

        /// <summary>
        /// Updates the graphics device with new depth parameters.
        /// </summary>
        /// <param name="depthInfo">The depth parameters to use.</param>
        protected abstract void SetDepthInfoImplementation(DepthInfo depthInfo);

        /// <summary>
        /// Updates the graphics device with new stencil parameters.
        /// </summary>
        /// <param name="stencilInfo">The stencil parameters to use.</param>
        protected abstract void SetStencilInfoImplementation(StencilInfo stencilInfo);

        #endregion

        #region Batches

        internal IVertexBatch<TexturedVertex2D> DefaultQuadBatch => quadBatches.Peek();

        internal void PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch) => quadBatches.Push(quadBatch);

        internal void PopQuadBatch() => quadBatches.Pop();

        /// <summary>
        /// Notifies that a <see cref="IVertexBuffer"/> has begun being used.
        /// </summary>
        /// <param name="buffer">The <see cref="IVertexBuffer"/> in use.</param>
        internal void RegisterVertexBufferUse(IVertexBuffer buffer) => vertexBuffersInUse.Add(buffer);

        /// <summary>
        /// Sets the last vertex batch used for drawing.
        /// <para>
        /// This is done so that various methods that change renderer state can force-draw the batch
        /// before continuing with the state change.
        /// </para>
        /// </summary>
        /// <param name="batch">The batch.</param>
        internal void SetActiveBatch(IVertexBatch batch)
        {
            if (currentActiveBatch == batch)
                return;

            batchResetList.Add(batch);

            FlushCurrentBatch(FlushBatchSource.SetActiveBatch);

            currentActiveBatch = batch;
        }

        /// <summary>
        /// Flushes the currently active vertex batch.
        /// </summary>
        /// <param name="source">The source performing the flush, for profiling purposes.</param>
        protected internal void FlushCurrentBatch(FlushBatchSource? source)
        {
            if (currentActiveBatch?.Draw() > 0 && source != null)
                flush_source_statistics[(int)source].Value++;
        }

        private void freeUnusedVertexBuffers()
        {
            foreach (var buf in vertexBuffersInUse)
            {
                if (buf.InUse && FrameIndex - buf.LastUseFrameIndex > RESOURCE_FREE_NO_USAGE_LENGTH)
                {
                    // Calling Free will mark InUse as false internally, which allows the cleanup below to work.
                    buf.Free();
                }
            }

            vertexBuffersInUse.RemoveAll(b => !b.InUse);
        }

        #endregion

        #region Textures

        public bool BindTexture(Texture texture, int unit, WrapMode? wrapModeS, WrapMode? wrapModeT)
        {
            ObjectDisposedException.ThrowIf(!texture.Available, texture);

            if (texture is TextureWhitePixel && lastBoundTextureIsAtlas[unit])
            {
                setWrapMode(wrapModeS ?? texture.WrapModeS, wrapModeT ?? texture.WrapModeT);

                // We can use the special white space from any atlas texture.
                return true;
            }

            texture.NativeTexture.Upload();

            bool didBind = BindTexture(texture.NativeTexture, unit, wrapModeS ?? texture.WrapModeS, wrapModeT ?? texture.WrapModeT);
            lastBoundTextureIsAtlas[unit] = texture.IsAtlasTexture;

            return didBind;
        }

        /// <summary>
        /// Binds a native texture. Generally used by internal components of renderer implementations.
        /// </summary>
        /// <param name="texture">The native texture to bind.</param>
        /// <param name="unit">The sampling unit in which the texture is to be bound.</param>
        /// <param name="wrapModeS">The texture's horizontal wrap mode.</param>
        /// <param name="wrapModeT">The texture's vertex wrap mode.</param>
        /// <returns>Whether the texture was successfully bound.</returns>
        public bool BindTexture(INativeTexture texture, int unit = 0, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            if (lastActiveTextureUnit == unit && lastBoundTexture[unit] == texture)
            {
                setWrapMode(wrapModeS, wrapModeT);
                return true;
            }

            FlushCurrentBatch(FlushBatchSource.BindTexture);

            if (!SetTextureImplementation(texture, unit))
                return false;

            setWrapMode(wrapModeS, wrapModeT);

            lastBoundTexture[unit] = texture;
            lastBoundTextureIsAtlas[unit] = false;
            lastActiveTextureUnit = unit;

            FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            texture.TotalBindCount++;

            return true;
        }

        private void setWrapMode(WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (wrapModeS != CurrentWrapModeS)
            {
                FlushCurrentBatch(FlushBatchSource.BindTexture);

                CurrentWrapModeS = wrapModeS;
                globalUniformsChanged = true;
            }

            if (wrapModeT != CurrentWrapModeT)
            {
                FlushCurrentBatch(FlushBatchSource.BindTexture);

                CurrentWrapModeT = wrapModeT;
                globalUniformsChanged = true;
            }
        }

        /// <summary>
        /// Unbinds any bound texture.
        /// </summary>
        /// <param name="unit">The sampling unit in which the texture is to be unbound.</param>
        public void UnbindTexture(int unit = 0)
        {
            if (lastBoundTexture[unit] == null)
                return;

            FlushCurrentBatch(FlushBatchSource.UnbindTexture);
            SetTextureImplementation(null, unit);

            lastBoundTexture[unit] = null;
            lastBoundTextureIsAtlas[unit] = false;
        }

        /// <summary>
        /// Enqueues a texture to be uploaded in the next frame.
        /// </summary>
        /// <param name="texture">The texture to be uploaded.</param>
        internal void EnqueueTextureUpload(INativeTexture texture)
        {
            if (!IsInitialised || textureUploadQueue.Contains(texture))
                return;

            textureUploadQueue.Enqueue(texture);
        }

        /// <summary>
        /// Informs the graphics device to use the given texture for drawing.
        /// </summary>
        /// <param name="texture">The texture, or null to use default texture.</param>
        /// <param name="unit">The sampling unit in which the texture is to be bound.</param>
        /// <returns>Whether the texture was set successfully.</returns>
        protected abstract bool SetTextureImplementation(INativeTexture? texture, int unit);

        #endregion

        #region Framebuffers

        public void BindFrameBuffer(IFrameBuffer frameBuffer)
        {
            frameBufferStack.Push(frameBuffer);
            setFrameBuffer(frameBuffer);
        }

        public void UnbindFrameBuffer(IFrameBuffer? frameBuffer)
        {
            if (FrameBuffer != frameBuffer)
                return;

            frameBufferStack.Pop();
            setFrameBuffer(frameBufferStack.TryPeek(out var lastFramebuffer) ? lastFramebuffer : null);
        }

        private void setFrameBuffer(IFrameBuffer? frameBuffer, bool force = false)
        {
            if (frameBuffer == FrameBuffer && !force)
                return;

            FlushCurrentBatch(FlushBatchSource.SetFrameBuffer);
            SetFrameBufferImplementation(frameBuffer);

            FrameBuffer = frameBuffer;

            UsingBackbuffer = frameBuffer == null;
            globalUniformsChanged = true;
        }

        /// <summary>
        /// Informs the graphics device to use the given framebuffer for drawing.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to use, or null to use the backbuffer (i.e. main framebuffer).</param>
        protected abstract void SetFrameBufferImplementation(IFrameBuffer? frameBuffer);

        /// <summary>
        /// Deletes a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to delete.</param>
        public void DeleteFrameBuffer(IFrameBuffer frameBuffer)
        {
            while (FrameBuffer == frameBuffer)
                UnbindFrameBuffer(frameBuffer);

            ScheduleDisposal(DeleteFrameBufferImplementation, frameBuffer);
        }

        protected abstract void DeleteFrameBufferImplementation(IFrameBuffer frameBuffer);

        #endregion

        public void DrawVertices(PrimitiveTopology topology, int vertexStart, int verticesCount)
        {
            if (Shader == null)
                throw new InvalidOperationException("No shader bound.");

            if (globalUniformsChanged)
            {
                globalUniformBuffer!.Data = new GlobalUniformData
                {
                    BackbufferDraw = UsingBackbuffer,
                    IsDepthRangeZeroToOne = IsDepthRangeZeroToOne,
                    IsClipSpaceYInverted = IsClipSpaceYInverted,
                    IsUvOriginTopLeft = IsUvOriginTopLeft,
                    ProjMatrix = ProjectionMatrix,
                    ToMaskingSpace = currentMaskingInfo.ToMaskingSpace,
                    IsMasking = IsMaskingActive,
                    CornerRadius = currentMaskingInfo.CornerRadius,
                    CornerExponent = currentMaskingInfo.CornerExponent,
                    MaskingRect = new Vector4(
                        currentMaskingInfo.MaskingRect.Left,
                        currentMaskingInfo.MaskingRect.Top,
                        currentMaskingInfo.MaskingRect.Right,
                        currentMaskingInfo.MaskingRect.Bottom),
                    BorderThickness = currentMaskingInfo.BorderThickness / currentMaskingInfo.BlendRange,
                    BorderColour = currentMaskingInfo.BorderThickness > 0
                        ? new Matrix4(
                            // TopLeft
                            currentMaskingInfo.BorderColour.TopLeft.SRGB.R,
                            currentMaskingInfo.BorderColour.TopLeft.SRGB.G,
                            currentMaskingInfo.BorderColour.TopLeft.SRGB.B,
                            currentMaskingInfo.BorderColour.TopLeft.SRGB.A,
                            // BottomLeft
                            currentMaskingInfo.BorderColour.BottomLeft.SRGB.R,
                            currentMaskingInfo.BorderColour.BottomLeft.SRGB.G,
                            currentMaskingInfo.BorderColour.BottomLeft.SRGB.B,
                            currentMaskingInfo.BorderColour.BottomLeft.SRGB.A,
                            // TopRight
                            currentMaskingInfo.BorderColour.TopRight.SRGB.R,
                            currentMaskingInfo.BorderColour.TopRight.SRGB.G,
                            currentMaskingInfo.BorderColour.TopRight.SRGB.B,
                            currentMaskingInfo.BorderColour.TopRight.SRGB.A,
                            // BottomRight
                            currentMaskingInfo.BorderColour.BottomRight.SRGB.R,
                            currentMaskingInfo.BorderColour.BottomRight.SRGB.G,
                            currentMaskingInfo.BorderColour.BottomRight.SRGB.B,
                            currentMaskingInfo.BorderColour.BottomRight.SRGB.A)
                        : globalUniformBuffer.Data.BorderColour,
                    MaskingBlendRange = currentMaskingInfo.BlendRange,
                    AlphaExponent = currentMaskingInfo.AlphaExponent,
                    EdgeOffset = currentMaskingInfo.EdgeOffset,
                    DiscardInner = currentMaskingInfo.Hollow,
                    InnerCornerRadius = currentMaskingInfo.Hollow
                        ? currentMaskingInfo.HollowCornerRadius
                        : globalUniformBuffer.Data.InnerCornerRadius,
                    WrapModeS = (int)CurrentWrapModeS,
                    WrapModeT = (int)CurrentWrapModeT
                };

                globalUniformsChanged = false;
            }

            Shader.BindUniformBlock("g_GlobalUniforms", globalUniformBuffer!);

            DrawVerticesImplementation(topology, vertexStart, verticesCount);

            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, verticesCount);
        }

        public abstract void DrawVerticesImplementation(PrimitiveTopology topology, int vertexStart, int verticesCount);

        #region Shaders

        public void BindShader(IShader shader)
        {
            bool alreadyBound = shaderStack.Count > 0 && shaderStack.Peek() == shader;

            shaderStack.Push(shader);

            if (!alreadyBound)
                setShader(shader);
        }

        public void UnbindShader(IShader shader)
        {
            if (shaderStack.Peek() != shader)
                throw new InvalidOperationException("Attempting to unbind shader while isn't the latest bound shader.");

            shaderStack.Pop();
            setShader(shaderStack.TryPeek(out var lastShader) ? lastShader : null);
        }

        private void setShader(IShader? shader)
        {
            ThreadSafety.EnsureDrawThread();

            if (shader == Shader)
                return;

            if (shader != null)
            {
                FrameStatistics.Increment(StatisticsCounterType.ShaderBinds);

                FlushCurrentBatch(FlushBatchSource.SetShader);
                SetShaderImplementation(shader);

                // importantly, when a shader is unbound, it remains bound in the implementation.
                // to save VBO flushing overhead, keep reference of the last shader.
                Shader = shader;
            }
        }

        internal void SetUniform<T>(IUniformWithValue<T> uniform)
            where T : unmanaged, IEquatable<T>
        {
            if (uniform.Owner == Shader)
                FlushCurrentBatch(FlushBatchSource.SetUniform);

            SetUniformImplementation(uniform);
        }

        /// <summary>
        /// Informs the graphics device to use the given shader for drawing.
        /// </summary>
        /// <param name="shader">The shader to use.</param>
        protected abstract void SetShaderImplementation(IShader shader);

        /// <summary>
        /// Informs the graphics device to update the value of the given uniform.
        /// </summary>
        /// <param name="uniform">The uniform to update.</param>
        protected abstract void SetUniformImplementation<T>(IUniformWithValue<T> uniform) where T : unmanaged, IEquatable<T>;

        public void BindUniformBuffer(string blockName, IUniformBuffer buffer)
        {
            if (boundUniformBuffers.TryGetValue(blockName, out IUniformBuffer? current) && current == buffer)
                return;

            FlushCurrentBatch(FlushBatchSource.BindBuffer);
            SetUniformBufferImplementation(blockName, buffer);

            boundUniformBuffers[blockName] = buffer;
        }

        protected abstract void SetUniformBufferImplementation(string blockName, IUniformBuffer buffer);

        #endregion

        #region Factory

        public abstract IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear);

        /// <inheritdoc cref="IRenderer.CreateShaderPart"/>
        protected abstract IShaderPart CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType);

        /// <inheritdoc cref="IRenderer.CreateShader"/>
        protected abstract IShader CreateShader(string name, IShaderPart[] parts, ShaderCompilationStore compilationStore);

        private IShader? mipmapShader;

        internal IShader GetMipmapShader()
        {
            if (mipmapShader != null)
                return mipmapShader;

            var store = new PassthroughShaderStore(
                new NamespacedResourceStore<byte[]>(
                    new NamespacedResourceStore<byte[]>(
                        new DllResourceStore(typeof(Game).Assembly),
                        @"Resources"),
                    "Shaders"));

            mipmapShader = CreateShader("mipmap", new[]
            {
                CreateShaderPart(store, "mipmap.vs", store.GetRawData("sh_mipmap.vs"), ShaderPartType.Vertex),
                CreateShaderPart(store, "mipmap.fs", store.GetRawData("sh_mipmap.fs"), ShaderPartType.Fragment),
            }, shaderCompilationStore);

            return mipmapShader;
        }

        /// <inheritdoc cref="IRenderer.CreateLinearBatch{TVertex}"/>
        protected abstract IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology) where TVertex : unmanaged, IEquatable<TVertex>, IVertex;

        /// <inheritdoc cref="IRenderer.CreateQuadBatch{TVertex}"/>
        protected abstract IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : unmanaged, IEquatable<TVertex>, IVertex;

        /// <inheritdoc cref="IRenderer.CreateUniformBuffer{TData}"/>
        protected abstract IUniformBuffer<TData> CreateUniformBuffer<TData>() where TData : unmanaged, IEquatable<TData>;

        /// <inheritdoc cref="IRenderer.CreateShaderStorageBufferObject{TData}"/>
        protected abstract IShaderStorageBufferObject<TData> CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize) where TData : unmanaged, IEquatable<TData>;

        /// <summary>
        /// Creates a new <see cref="INativeTexture"/>.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="manualMipmaps">Whether manual mipmaps will be uploaded to the texture. If false, the texture will compute mipmaps automatically.</param>
        /// <param name="filteringMode">The filtering mode.</param>
        /// <param name="initialisationColour">The colour to initialise texture levels with (in the case of sub region initial uploads). If null, no initialisation is provided out-of-the-box.</param>
        /// <returns>The <see cref="INativeTexture"/>.</returns>
        protected abstract INativeTexture CreateNativeTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear,
                                                              Color4? initialisationColour = null);

        /// <summary>
        /// Creates a new <see cref="INativeTexture"/> for video sprites.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <returns>The video <see cref="INativeTexture"/>.</returns>
        protected abstract INativeTexture CreateNativeVideoTexture(int width, int height);

        public Texture CreateTexture(int width, int height, bool manualMipmaps, TextureFilteringMode filteringMode, WrapMode wrapModeS, WrapMode wrapModeT, Color4? initialisationColour)
            => CreateTexture(CreateNativeTexture(width, height, manualMipmaps, filteringMode, initialisationColour), wrapModeS, wrapModeT);

        public Texture CreateVideoTexture(int width, int height) => CreateTexture(CreateNativeVideoTexture(width, height));

        /// <summary>
        /// Creates a new <see cref="Texture"/> based off an <see cref="INativeTexture"/>.
        /// </summary>
        /// <param name="nativeTexture">The <see cref="INativeTexture"/> to create the texture with.</param>
        /// <param name="wrapModeS">The horizontal wrap mode of the texture.</param>
        /// <param name="wrapModeT">The vertical wrap mode of the texture.</param>
        /// <returns>The <see cref="Texture"/>.</returns>
        internal Texture CreateTexture(INativeTexture nativeTexture, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
            => registerTexture(new Texture(nativeTexture, wrapModeS, wrapModeT));

        private Texture registerTexture(Texture texture)
        {
            allTextures.Add(texture);
            TextureCreated?.Invoke(texture);
            return texture;
        }

        #endregion

        #region IRenderer explicit implementation

        bool IRenderer.VerticalSync
        {
            get => VerticalSync;
            set => VerticalSync = value;
        }

        bool IRenderer.AllowTearing
        {
            get => AllowTearing;
            set => AllowTearing = value;
        }

        Storage? IRenderer.CacheStorage
        {
            set => CacheStorage = value;
        }

        IVertexBatch<TexturedVertex2D> IRenderer.DefaultQuadBatch => DefaultQuadBatch;
        void IRenderer.BeginFrame(Vector2 windowSize) => BeginFrame(windowSize);
        void IRenderer.FinishFrame() => FinishFrame();
        void IRenderer.FlushCurrentBatch(FlushBatchSource? source) => FlushCurrentBatch(source);
        void IRenderer.SwapBuffers() => SwapBuffers();
        void IRenderer.WaitUntilIdle() => WaitUntilIdle();
        void IRenderer.WaitUntilNextFrameReady() => WaitUntilNextFrameReady();
        void IRenderer.MakeCurrent() => MakeCurrent();
        void IRenderer.ClearCurrent() => ClearCurrent();
        void IRenderer.SetUniform<T>(IUniformWithValue<T> uniform) => SetUniform(uniform);
        void IRenderer.PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch) => PushQuadBatch(quadBatch);
        void IRenderer.PopQuadBatch() => PopQuadBatch();
        Image<Rgba32> IRenderer.TakeScreenshot() => TakeScreenshot();
        Image<Rgba32>? IRenderer.ExtractFrameBufferData(IFrameBuffer frameBuffer) => ExtractFrameBufferData(frameBuffer);
        IShaderPart IRenderer.CreateShaderPart(IShaderStore store, string name, byte[]? rawData, ShaderPartType partType) => CreateShaderPart(store, name, rawData, partType);
        IShader IRenderer.CreateShader(string name, IShaderPart[] parts) => CreateShader(name, parts, shaderCompilationStore);

        IVertexBatch<TVertex> IRenderer.CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
        {
            if (size <= 0)
                throw new ArgumentException("Linear batch size must be > 0.", nameof(size));

            if (size > IRenderer.MAX_VERTICES)
                throw new ArgumentException($"Linear batch may not have more than {IRenderer.MAX_VERTICES} vertices.", nameof(size));

            if (maxBuffers <= 0)
                throw new ArgumentException("Maximum number of buffers must be > 0.", nameof(maxBuffers));

            return CreateLinearBatch<TVertex>(size, maxBuffers, topology);
        }

        IVertexBatch<TVertex> IRenderer.CreateQuadBatch<TVertex>(int size, int maxBuffers)
        {
            if (size <= 0)
                throw new ArgumentException("Quad batch size must be > 0.", nameof(size));

            if (size > IRenderer.MAX_QUADS)
                throw new ArgumentException($"Quad batch may not have more than {IRenderer.MAX_QUADS} quads.", nameof(size));

            if (maxBuffers <= 0)
                throw new ArgumentException("Maximum number of buffers must be > 0.", nameof(maxBuffers));

            return CreateQuadBatch<TVertex>(size, maxBuffers);
        }

        private readonly HashSet<Type> validUboTypes = new HashSet<Type>();

        IUniformBuffer<TData> IRenderer.CreateUniformBuffer<TData>()
        {
            validateUniformLayout<TData>();
            return CreateUniformBuffer<TData>();
        }

        IShaderStorageBufferObject<TData> IRenderer.CreateShaderStorageBufferObject<TData>(int uboSize, int ssboSize)
        {
            validateUniformLayout<TData>();
            return CreateShaderStorageBufferObject<TData>(uboSize, ssboSize);
        }

        private void validateUniformLayout<TData>()
        {
            if (validUboTypes.Contains(typeof(TData)))
                return;

            if (typeof(TData).StructLayoutAttribute?.Pack != 1)
                throw new ArgumentException($"{typeof(TData).ReadableName()} requires a packing size of 1.");

            int offset = 0;

            foreach (var field in typeof(TData).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                checkValidType(field);

                if (field.FieldType == typeof(UniformMatrix3)
                    || field.FieldType == typeof(UniformMatrix4)
                    || field.FieldType == typeof(UniformVector3)
                    || field.FieldType == typeof(UniformVector4))
                {
                    checkAlignment(field, offset, 16);
                }

                if (field.FieldType == typeof(UniformVector2))
                    checkAlignment(field, offset, 8);

                offset += Marshal.SizeOf(field.FieldType);
            }

            Type? finalPadding = suggestPadding(offset, 16);
            if (finalPadding != null)
                throw new ArgumentException($"{typeof(TData).ReadableName()} alignment requires a {finalPadding} to be added at the end.");

            validUboTypes.Add(typeof(TData));
            return;

            static void checkValidType(FieldInfo field)
            {
                if (field.FieldType == typeof(UniformBool)
                    || field.FieldType == typeof(UniformFloat)
                    || field.FieldType == typeof(UniformInt)
                    || field.FieldType == typeof(UniformMatrix3)
                    || field.FieldType == typeof(UniformMatrix4)
                    || field.FieldType == typeof(UniformPadding4)
                    || field.FieldType == typeof(UniformPadding8)
                    || field.FieldType == typeof(UniformPadding12)
                    || field.FieldType == typeof(UniformVector2)
                    || field.FieldType == typeof(UniformVector4)
                    || field.FieldType == typeof(UniformVector4))
                {
                    return;
                }

                throw new ArgumentException($"{typeof(TData).ReadableName()} has an unsupported type of {field.FieldType} for field \"{field.Name}\".");
            }

            static void checkAlignment(FieldInfo field, int offset, int expectedAlignment)
            {
                Type? suggestedPadding = suggestPadding(offset, expectedAlignment);
                if (suggestedPadding != null)
                    throw new ArgumentException($"{typeof(TData).ReadableName()} alignment requires a {suggestedPadding} to be inserted before \"{field.Name}\".");
            }

            static Type? suggestPadding(int offset, int expectedAlignment)
            {
                int currentAlignment = offset % expectedAlignment;
                int paddingRequired = expectedAlignment - currentAlignment;

                if (currentAlignment == 0)
                    return null;

                return paddingRequired switch
                {
                    4 => typeof(UniformPadding4),
                    8 => typeof(UniformPadding8),
                    12 => typeof(UniformPadding12),
                    _ => null
                };
            }
        }

        #endregion

        #region TextureVisualiser support

        /// <summary>
        /// An event which is invoked every time a <see cref="Texture"/> is created.
        /// </summary>
        internal event Action<Texture>? TextureCreated;

        event Action<Texture>? IRenderer.TextureCreated
        {
            add => TextureCreated += value;
            remove => TextureCreated -= value;
        }

        Texture[] IRenderer.GetAllTextures() => allTextures.ToArray();

        #endregion

        private class PassthroughShaderStore : IShaderStore
        {
            private readonly IResourceStore<byte[]> store;

            public PassthroughShaderStore(IResourceStore<byte[]> store)
            {
                this.store = store;
            }

            public byte[]? GetRawData(string fileName) => store.Get(fileName);
        }
    }
}
