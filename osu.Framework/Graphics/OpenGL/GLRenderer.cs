// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Development;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Batches;
using osu.Framework.Graphics.OpenGL.Shaders;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osu.Framework.Lists;
using osu.Framework.Statistics;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Graphics.OpenGL
{
    internal class GLRenderer : IRenderer
    {
        /// <summary>
        /// The interval (in frames) before checking whether VBOs should be freed.
        /// VBOs may remain unused for at most double this length before they are recycled.
        /// </summary>
        private const int vbo_free_check_interval = 300;

        public int MaxTextureSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.

        /// <summary>
        /// The maximum allowed render buffer size.
        /// </summary>
        public int MaxRenderBufferSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.

        public int MaxTexturesUploadedPerFrame { get; set; } = 32;
        public int MaxPixelsUploadedPerFrame { get; set; } = 1024 * 1024 * 2;

        /// <summary>
        /// Whether the current platform is embedded.
        /// </summary>
        public bool IsEmbedded { get; private set; }

        /// <summary>
        /// The current reset index.
        /// </summary>
        public ulong ResetId { get; private set; }

        public ref readonly MaskingInfo CurrentMaskingInfo => ref currentMaskingInfo;

        public RectangleI Viewport { get; private set; }
        public RectangleI Scissor { get; private set; }
        public Vector2I ScissorOffset { get; private set; }
        public Matrix4 ProjectionMatrix { get; private set; }
        public DepthInfo CurrentDepthInfo { get; private set; }
        public StencilInfo CurrentStencilInfo { get; private set; }
        public WrapMode CurrentWrapModeS { get; private set; }
        public WrapMode CurrentWrapModeT { get; private set; }
        public bool IsMaskingActive => maskingStack.Count > 1;
        public float BackbufferDrawDepth { get; private set; }
        public bool UsingBackbuffer => frameBufferStack.Count > 0 && frameBufferStack.Peek() == BackbufferFramebuffer;
        public Texture WhitePixel => whitePixel.Value;

        protected virtual int BackbufferFramebuffer => 0;

        private readonly GlobalStatistic<int> statExpensiveOperationsQueued = GlobalStatistics.Get<int>(nameof(GLRenderer), "Expensive operation queue length");
        private readonly GlobalStatistic<int> statTextureUploadsQueued = GlobalStatistics.Get<int>(nameof(GLRenderer), "Texture upload queue length");
        private readonly GlobalStatistic<int> statTextureUploadsDequeued = GlobalStatistics.Get<int>(nameof(GLRenderer), "Texture uploads dequeued");
        private readonly GlobalStatistic<int> statTextureUploadsPerformed = GlobalStatistics.Get<int>(nameof(GLRenderer), "Texture uploads performed");

        private readonly ConcurrentQueue<ScheduledDelegate> expensiveOperationQueue = new ConcurrentQueue<ScheduledDelegate>();
        private readonly ConcurrentQueue<GLTexture> textureUploadQueue = new ConcurrentQueue<GLTexture>();
        private readonly GLDisposalQueue disposalQueue = new GLDisposalQueue();

        private readonly Scheduler resetScheduler = new Scheduler(() => ThreadSafety.IsDrawThread, new StopwatchClock(true)); // force no thread set until we are actually on the draw thread.

        private readonly Stack<IVertexBatch<TexturedVertex2D>> quadBatches = new Stack<IVertexBatch<TexturedVertex2D>>();
        private readonly List<IGLVertexBuffer> vertexBuffersInUse = new List<IGLVertexBuffer>();
        private readonly List<IVertexBatch> batchResetList = new List<IVertexBatch>();
        private readonly Stack<RectangleI> viewportStack = new Stack<RectangleI>();
        private readonly Stack<Matrix4> projectionMatrixStack = new Stack<Matrix4>();
        private readonly Stack<MaskingInfo> maskingStack = new Stack<MaskingInfo>();
        private readonly Stack<RectangleI> scissorRectStack = new Stack<RectangleI>();
        private readonly Stack<DepthInfo> depthStack = new Stack<DepthInfo>();
        private readonly Stack<StencilInfo> stencilStack = new Stack<StencilInfo>();
        private readonly Stack<Vector2I> scissorOffsetStack = new Stack<Vector2I>();
        private readonly Stack<IShader> shaderStack = new Stack<IShader>();
        private readonly Stack<bool> scissorStateStack = new Stack<bool>();
        private readonly Stack<int> frameBufferStack = new Stack<int>();
        private readonly bool[] lastBoundTextureIsAtlas = new bool[16];
        private readonly int[] lastBoundBuffers = new int[2];
        private readonly int[] lastBoundTexture = new int[16];

        // in case no other textures are used in the project, create a new atlas as a fallback source for the white pixel area (used to draw boxes etc.)
        private readonly Lazy<TextureWhitePixel> whitePixel;
        private readonly LockedWeakList<Texture> allTextures = new LockedWeakList<Texture>();

        private IVertexBatch<TexturedVertex2D>? defaultQuadBatch;
        private BlendingParameters lastBlendingParameters;
        private IVertexBatch? lastActiveBatch;
        private MaskingInfo currentMaskingInfo;
        private ClearInfo currentClearInfo;
        private IShader? currentShader;
        private bool? lastBlendingEnabledState;
        private bool currentScissorState;
        private bool isInitialised;
        private int lastActiveTextureUnit;

        public GLRenderer()
        {
            whitePixel = new Lazy<TextureWhitePixel>(() =>
                new TextureAtlas(this, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, true).WhitePixel);
        }

        void IRenderer.Initialise()
        {
            string version = GL.GetString(StringName.Version);
            IsEmbedded = version.Contains("OpenGL ES"); // As defined by https://www.khronos.org/registry/OpenGL-Refpages/es2.0/xhtml/glGetString.xml

            MaxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);
            MaxRenderBufferSize = GL.GetInteger(GetPName.MaxRenderbufferSize);

            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.Blend);

            defaultQuadBatch = CreateQuadBatch<TexturedVertex2D>(100, 1000);

            resetScheduler.AddDelayed(checkPendingDisposals, 0, true);
            isInitialised = true;
        }

        private void checkPendingDisposals()
        {
            disposalQueue.CheckPendingDisposals();
        }

        void IRenderer.BeginFrame(Vector2 windowSize)
        {
            Debug.Assert(defaultQuadBatch != null);

            ResetId++;

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

            lastActiveBatch = null;
            lastBlendingParameters = new BlendingParameters();
            lastBlendingEnabledState = null;

            foreach (var b in batchResetList)
                b.ResetCounters();
            batchResetList.Clear();

            currentShader?.Unbind();
            currentShader = null;
            shaderStack.Clear();
            GL.UseProgram(0);

            viewportStack.Clear();
            projectionMatrixStack.Clear();
            maskingStack.Clear();
            scissorRectStack.Clear();
            frameBufferStack.Clear();
            depthStack.Clear();
            stencilStack.Clear();
            scissorStateStack.Clear();
            scissorOffsetStack.Clear();

            quadBatches.Clear();
            quadBatches.Push(defaultQuadBatch);

            BindFrameBuffer(BackbufferFramebuffer);

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

            statTextureUploadsQueued.Value = textureUploadQueue.Count;
            statTextureUploadsDequeued.Value = 0;
            statTextureUploadsPerformed.Value = 0;

            // increase the number of items processed with the queue length to ensure it doesn't get out of hand.
            int targetUploads = Math.Clamp(textureUploadQueue.Count / 2, 1, MaxTexturesUploadedPerFrame);
            int uploads = 0;
            int uploadedPixels = 0;

            // continue attempting to upload textures until enough uploads have been performed.
            while (textureUploadQueue.TryDequeue(out GLTexture? texture))
            {
                statTextureUploadsDequeued.Value++;

                texture.IsQueuedForUpload = false;

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
            lastBoundBuffers.AsSpan().Clear();
        }

        void IRenderer.FinishFrame()
        {
            flushCurrentBatch();
        }

        private void freeUnusedVertexBuffers()
        {
            if (ResetId % vbo_free_check_interval != 0)
                return;

            foreach (var buf in vertexBuffersInUse)
            {
                if (buf.InUse && ResetId - buf.LastUseResetId > vbo_free_check_interval)
                    buf.Free();
            }

            vertexBuffersInUse.RemoveAll(b => !b.InUse);
        }

        public bool BindBuffer(BufferTarget target, int buffer)
        {
            int bufferIndex = target - BufferTarget.ArrayBuffer;
            if (lastBoundBuffers[bufferIndex] == buffer)
                return false;

            lastBoundBuffers[bufferIndex] = buffer;
            GL.BindBuffer(target, buffer);

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            return true;
        }

        public bool BindTexture(Texture texture, int unit = 0, WrapMode? wrapModeS = null, WrapMode? wrapModeT = null)
        {
            if (texture is TextureWhitePixel && lastBoundTextureIsAtlas[unit])
            {
                // We can use the special white space from any atlas texture.
                return true;
            }

            bool didBind = texture.NativeTexture.Bind(unit, wrapModeS ?? texture.WrapModeS, wrapModeT ?? texture.WrapModeT);
            lastBoundTextureIsAtlas[unit] = texture.IsAtlasTexture;

            return didBind;
        }

        public void UseProgram(IShader? shader)
        {
            ThreadSafety.EnsureDrawThread();

            if (shader != null)
                shaderStack.Push(shader);
            else
            {
                shaderStack.Pop();

                //check if the stack is empty, and if so don't restore the previous shader.
                if (shaderStack.Count == 0)
                    return;
            }

            shader ??= shaderStack.Peek();

            if (currentShader == shader)
                return;

            FrameStatistics.Increment(StatisticsCounterType.ShaderBinds);

            flushCurrentBatch();

            GL.UseProgram((GLShader)shader);
            currentShader = shader;
        }

        public void BindFrameBuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            bool alreadyBound = frameBufferStack.Count > 0 && frameBufferStack.Peek() == frameBuffer;

            frameBufferStack.Push(frameBuffer);

            if (!alreadyBound)
            {
                flushCurrentBatch();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);

                GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            }

            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        public void UnbindFrameBuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            if (frameBufferStack.Peek() != frameBuffer)
                return;

            frameBufferStack.Pop();

            flushCurrentBatch();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferStack.Peek());

            GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        public void Clear(ClearInfo clearInfo)
        {
            PushDepthInfo(new DepthInfo(writeDepth: true));
            PushScissorState(false);
            if (clearInfo.Colour != currentClearInfo.Colour)
                GL.ClearColor(clearInfo.Colour);

            if (clearInfo.Depth != currentClearInfo.Depth)
            {
                if (IsEmbedded)
                {
                    // GL ES only supports glClearDepthf
                    // See: https://www.khronos.org/registry/OpenGL-Refpages/es3.0/html/glClearDepthf.xhtml
                    GL.ClearDepth((float)clearInfo.Depth);
                }
                else
                {
                    // Older desktop platforms don't support glClearDepthf, so standard GL's double version is used instead
                    // See: https://www.khronos.org/registry/OpenGL-Refpages/gl4/html/glClearDepth.xhtml
                    osuTK.Graphics.OpenGL.GL.ClearDepth(clearInfo.Depth);
                }
            }

            if (clearInfo.Stencil != currentClearInfo.Stencil)
                GL.ClearStencil(clearInfo.Stencil);

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            currentClearInfo = clearInfo;

            PopScissorState();
            PopDepthInfo();
        }

        public void PushScissorState(bool enabled)
        {
            scissorStateStack.Push(enabled);
            setScissorState(enabled);
        }

        public void PopScissorState()
        {
            Trace.Assert(scissorStateStack.Count > 1);

            scissorStateStack.Pop();

            setScissorState(scissorStateStack.Peek());
        }

        private void setScissorState(bool enabled)
        {
            if (enabled == currentScissorState)
                return;

            currentScissorState = enabled;

            if (enabled)
                GL.Enable(EnableCap.ScissorTest);
            else
                GL.Disable(EnableCap.ScissorTest);
        }

        public void SetBlend(BlendingParameters blendingParameters)
        {
            if (lastBlendingParameters == blendingParameters)
                return;

            flushCurrentBatch();

            if (blendingParameters.IsDisabled)
            {
                if (!lastBlendingEnabledState.HasValue || lastBlendingEnabledState.Value)
                    GL.Disable(EnableCap.Blend);

                lastBlendingEnabledState = false;
            }
            else
            {
                if (!lastBlendingEnabledState.HasValue || !lastBlendingEnabledState.Value)
                    GL.Enable(EnableCap.Blend);

                lastBlendingEnabledState = true;

                GL.BlendEquationSeparate(blendingParameters.RGBEquationMode, blendingParameters.AlphaEquationMode);
                GL.BlendFuncSeparate(blendingParameters.SourceBlendingFactor, blendingParameters.DestinationBlendingFactor,
                    blendingParameters.SourceAlphaBlendingFactor, blendingParameters.DestinationAlphaBlendingFactor);
            }

            lastBlendingParameters = blendingParameters;
        }

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

            if (Viewport == actualRect)
                return;

            Viewport = actualRect;

            GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);
        }

        public void PopViewport()
        {
            Trace.Assert(viewportStack.Count > 1);

            PopProjectionMatrix();

            viewportStack.Pop();
            RectangleI actualRect = viewportStack.Peek();

            if (Viewport == actualRect)
                return;

            Viewport = actualRect;

            GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);
        }

        public void PushScissor(RectangleI scissor)
        {
            flushCurrentBatch();

            scissorRectStack.Push(scissor);
            if (Scissor == scissor)
                return;

            Scissor = scissor;
            setScissor(scissor);
        }

        public void PopScissor()
        {
            Trace.Assert(scissorRectStack.Count > 1);

            flushCurrentBatch();

            scissorRectStack.Pop();
            RectangleI scissor = scissorRectStack.Peek();

            if (Scissor == scissor)
                return;

            Scissor = scissor;
            setScissor(scissor);
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

            GL.Scissor(scissor.X, Viewport.Height - scissor.Bottom, scissor.Width, scissor.Height);
        }

        public void PushScissorOffset(Vector2I offset)
        {
            flushCurrentBatch();

            scissorOffsetStack.Push(offset);
            if (ScissorOffset == offset)
                return;

            ScissorOffset = offset;
        }

        public void PopScissorOffset()
        {
            Trace.Assert(scissorOffsetStack.Count > 1);

            flushCurrentBatch();

            scissorOffsetStack.Pop();
            Vector2I offset = scissorOffsetStack.Peek();

            if (ScissorOffset == offset)
                return;

            ScissorOffset = offset;
        }

        public void PushProjectionMatrix(Matrix4 matrix)
        {
            flushCurrentBatch();

            projectionMatrixStack.Push(matrix);
            if (ProjectionMatrix == matrix)
                return;

            ProjectionMatrix = matrix;

            GlobalPropertyManager.Set(GlobalProperty.ProjMatrix, ProjectionMatrix);
        }

        public void PopProjectionMatrix()
        {
            Trace.Assert(projectionMatrixStack.Count > 1);

            flushCurrentBatch();

            projectionMatrixStack.Pop();
            Matrix4 matrix = projectionMatrixStack.Peek();

            if (ProjectionMatrix == matrix)
                return;

            ProjectionMatrix = matrix;

            GlobalPropertyManager.Set(GlobalProperty.ProjMatrix, ProjectionMatrix);
        }

        public void PushMaskingInfo(in MaskingInfo maskingInfo, bool overwritePreviousScissor = false)
        {
            maskingStack.Push(maskingInfo);
            if (CurrentMaskingInfo == maskingInfo)
                return;

            currentMaskingInfo = maskingInfo;
            setMaskingInfo(CurrentMaskingInfo, true, overwritePreviousScissor);
        }

        public void PopMaskingInfo()
        {
            Trace.Assert(maskingStack.Count > 1);

            maskingStack.Pop();
            MaskingInfo maskingInfo = maskingStack.Peek();

            if (CurrentMaskingInfo == maskingInfo)
                return;

            currentMaskingInfo = maskingInfo;
            setMaskingInfo(CurrentMaskingInfo, false, true);
        }

        private void setMaskingInfo(MaskingInfo maskingInfo, bool isPushing, bool overwritePreviousScissor)
        {
            flushCurrentBatch();

            GlobalPropertyManager.Set(GlobalProperty.MaskingRect, new Vector4(
                maskingInfo.MaskingRect.Left,
                maskingInfo.MaskingRect.Top,
                maskingInfo.MaskingRect.Right,
                maskingInfo.MaskingRect.Bottom));

            GlobalPropertyManager.Set(GlobalProperty.ToMaskingSpace, maskingInfo.ToMaskingSpace);

            GlobalPropertyManager.Set(GlobalProperty.CornerRadius, maskingInfo.CornerRadius);
            GlobalPropertyManager.Set(GlobalProperty.CornerExponent, maskingInfo.CornerExponent);

            GlobalPropertyManager.Set(GlobalProperty.BorderThickness, maskingInfo.BorderThickness / maskingInfo.BlendRange);

            if (maskingInfo.BorderThickness > 0)
            {
                GlobalPropertyManager.Set(GlobalProperty.BorderColour, new Matrix4(
                    // TopLeft
                    maskingInfo.BorderColour.TopLeft.Linear.R,
                    maskingInfo.BorderColour.TopLeft.Linear.G,
                    maskingInfo.BorderColour.TopLeft.Linear.B,
                    maskingInfo.BorderColour.TopLeft.Linear.A,
                    // BottomLeft
                    maskingInfo.BorderColour.BottomLeft.Linear.R,
                    maskingInfo.BorderColour.BottomLeft.Linear.G,
                    maskingInfo.BorderColour.BottomLeft.Linear.B,
                    maskingInfo.BorderColour.BottomLeft.Linear.A,
                    // TopRight
                    maskingInfo.BorderColour.TopRight.Linear.R,
                    maskingInfo.BorderColour.TopRight.Linear.G,
                    maskingInfo.BorderColour.TopRight.Linear.B,
                    maskingInfo.BorderColour.TopRight.Linear.A,
                    // BottomRight
                    maskingInfo.BorderColour.BottomRight.Linear.R,
                    maskingInfo.BorderColour.BottomRight.Linear.G,
                    maskingInfo.BorderColour.BottomRight.Linear.B,
                    maskingInfo.BorderColour.BottomRight.Linear.A));
            }

            GlobalPropertyManager.Set(GlobalProperty.MaskingBlendRange, maskingInfo.BlendRange);
            GlobalPropertyManager.Set(GlobalProperty.AlphaExponent, maskingInfo.AlphaExponent);

            GlobalPropertyManager.Set(GlobalProperty.EdgeOffset, maskingInfo.EdgeOffset);

            GlobalPropertyManager.Set(GlobalProperty.DiscardInner, maskingInfo.Hollow);
            if (maskingInfo.Hollow)
                GlobalPropertyManager.Set(GlobalProperty.InnerCornerRadius, maskingInfo.HollowCornerRadius);

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
        }

        public void PushDepthInfo(DepthInfo depthInfo)
        {
            depthStack.Push(depthInfo);

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        public void PopDepthInfo()
        {
            Trace.Assert(depthStack.Count > 1);

            depthStack.Pop();
            DepthInfo depthInfo = depthStack.Peek();

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        private void setDepthInfo(DepthInfo depthInfo)
        {
            flushCurrentBatch();

            if (depthInfo.DepthTest)
            {
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(GLUtils.ToDepthFunction(depthInfo.Function));
            }
            else
                GL.Disable(EnableCap.DepthTest);

            GL.DepthMask(depthInfo.WriteDepth);
        }

        public void PushStencilInfo(StencilInfo stencilInfo)
        {
            stencilStack.Push(stencilInfo);

            if (CurrentStencilInfo.Equals(stencilInfo))
                return;

            CurrentStencilInfo = stencilInfo;
            setStencilInfo(stencilInfo);
        }

        public void PopStencilInfo()
        {
            Trace.Assert(stencilStack.Count > 1);

            stencilStack.Pop();
            StencilInfo stencilInfo = stencilStack.Peek();

            if (CurrentStencilInfo.Equals(stencilInfo))
                return;

            CurrentStencilInfo = stencilInfo;
            setStencilInfo(CurrentStencilInfo);
        }

        private void setStencilInfo(StencilInfo stencilInfo)
        {
            flushCurrentBatch();

            if (stencilInfo.StencilTest)
            {
                GL.Enable(EnableCap.StencilTest);
                GL.StencilFunc(GLUtils.ToStencilFunction(stencilInfo.TestFunction), stencilInfo.TestValue, stencilInfo.Mask);
                GL.StencilOp(
                    GLUtils.ToStencilOperation(stencilInfo.StencilTestFailOperation),
                    GLUtils.ToStencilOperation(stencilInfo.DepthTestFailOperation),
                    GLUtils.ToStencilOperation(stencilInfo.TestPassedOperation));
            }
            else
                GL.Disable(EnableCap.StencilTest);
        }

        public void ScheduleExpensiveOperation(ScheduledDelegate operation)
        {
            if (isInitialised)
                expensiveOperationQueue.Enqueue(operation);
        }

        public void ScheduleDisposal<T>(Action<T> disposalAction, T target)
        {
            if (isInitialised)
                disposalQueue.ScheduleDisposal(disposalAction, target);
            else
                disposalAction.Invoke(target);
        }

        /// <summary>
        /// Enqueues a texture to be uploaded in the next frame.
        /// </summary>
        /// <param name="texture">The texture to be uploaded.</param>
        public void EnqueueTextureUpload(GLTexture texture)
        {
            if (texture.IsQueuedForUpload)
                return;

            if (isInitialised)
            {
                texture.IsQueuedForUpload = true;
                textureUploadQueue.Enqueue(texture);
            }
        }

        public bool BindTexture(int textureId, int unit = 0, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            if (wrapModeS != CurrentWrapModeS)
            {
                // Will flush the current batch internally.
                GlobalPropertyManager.Set(GlobalProperty.WrapModeS, (int)wrapModeS);
                CurrentWrapModeS = wrapModeS;
            }

            if (wrapModeT != CurrentWrapModeT)
            {
                // Will flush the current batch internally.
                GlobalPropertyManager.Set(GlobalProperty.WrapModeT, (int)wrapModeT);
                CurrentWrapModeT = wrapModeT;
            }

            if (lastActiveTextureUnit == unit && lastBoundTexture[unit] == textureId)
                return false;

            flushCurrentBatch();

            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            lastBoundTexture[unit] = textureId;
            lastBoundTextureIsAtlas[unit] = false;
            lastActiveTextureUnit = unit;

            FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            return true;
        }

        IShaderPart IRenderer.CreateShaderPart(ShaderManager manager, string name, byte[]? rawData, ShaderPartType partType)
        {
            ShaderType glType;

            switch (partType)
            {
                case ShaderPartType.Fragment:
                    glType = ShaderType.FragmentShader;
                    break;

                case ShaderPartType.Vertex:
                    glType = ShaderType.VertexShader;
                    break;

                default:
                    throw new ArgumentException($"Unsupported shader part type: {partType}", nameof(partType));
            }

            return new GLShaderPart(this, name, rawData, glType, manager);
        }

        IShader IRenderer.CreateShader(string name, params IShaderPart[] parts) => new GLShader(this, name, parts.Cast<GLShaderPart>().ToArray());

        public IFrameBuffer CreateFrameBuffer(RenderBufferFormat[]? renderBufferFormats = null, TextureFilteringMode filteringMode = TextureFilteringMode.Linear)
        {
            All glFilteringMode;
            RenderbufferInternalFormat[]? glFormats = null;

            switch (filteringMode)
            {
                case TextureFilteringMode.Linear:
                    glFilteringMode = All.Linear;
                    break;

                case TextureFilteringMode.Nearest:
                    glFilteringMode = All.Nearest;
                    break;

                default:
                    throw new ArgumentException($"Unsupported filtering mode: {filteringMode}", nameof(filteringMode));
            }

            if (renderBufferFormats != null)
            {
                glFormats = new RenderbufferInternalFormat[renderBufferFormats.Length];

                for (int i = 0; i < renderBufferFormats.Length; i++)
                {
                    switch (renderBufferFormats[i])
                    {
                        case RenderBufferFormat.D16:
                            glFormats[i] = RenderbufferInternalFormat.DepthComponent16;
                            break;

                        case RenderBufferFormat.D32:
                            glFormats[i] = RenderbufferInternalFormat.DepthComponent32f;
                            break;

                        case RenderBufferFormat.D24S8:
                            glFormats[i] = RenderbufferInternalFormat.Depth24Stencil8;
                            break;

                        case RenderBufferFormat.D32S8:
                            glFormats[i] = RenderbufferInternalFormat.Depth32fStencil8;
                            break;

                        default:
                            throw new ArgumentException($"Unsupported render buffer format: {renderBufferFormats[i]}", nameof(renderBufferFormats));
                    }
                }
            }

            return new GLFrameBuffer(this, glFormats, glFilteringMode);
        }

        public Texture CreateTexture(int width, int height, bool manualMipmaps = false, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, WrapMode wrapModeS = WrapMode.None,
                                     WrapMode wrapModeT = WrapMode.None, Rgba32 initialisationColour = default)
        {
            All glFilteringMode;

            switch (filteringMode)
            {
                case TextureFilteringMode.Linear:
                    glFilteringMode = All.Linear;
                    break;

                case TextureFilteringMode.Nearest:
                    glFilteringMode = All.Nearest;
                    break;

                default:
                    throw new ArgumentException($"Unsupported filtering mode: {filteringMode}", nameof(filteringMode));
            }

            return CreateTexture(new GLTexture(this, width, height, manualMipmaps, glFilteringMode, initialisationColour), wrapModeS, wrapModeT);
        }

        public Texture CreateVideoTexture(int width, int height)
            => CreateTexture(new GLVideoTexture(this, width, height), WrapMode.None, WrapMode.None);

        public Texture CreateTexture(INativeTexture nativeTexture, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            var tex = new Texture(nativeTexture, wrapModeS, wrapModeT);

            allTextures.Add(tex);
            TextureCreated?.Invoke(tex);

            return tex;
        }

        public IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
        {
            if (size <= 0)
                throw new ArgumentException("Linear batch size must be > 0.", nameof(size));

            if (size > GLLinearBuffer<TVertex>.MAX_VERTICES)
                throw new ArgumentException($"Linear batch may not have more than {GLLinearBuffer<TVertex>.MAX_VERTICES} vertices.", nameof(size));

            if (maxBuffers <= 0)
                throw new ArgumentException("Maximum number of buffers must be > 0.", nameof(maxBuffers));

            return new GLLinearBatch<TVertex>(this, size, maxBuffers, GLUtils.ToPrimitiveType(topology));
        }

        public IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) where TVertex : unmanaged, IEquatable<TVertex>, IVertex
        {
            if (size <= 0)
                throw new ArgumentException("Quad batch size must be > 0.", nameof(size));

            if (size > GLQuadBuffer<TVertex>.MAX_QUADS)
                throw new ArgumentException($"Quad batch may not have more than {GLQuadBuffer<TVertex>.MAX_QUADS} quads.", nameof(size));

            if (maxBuffers <= 0)
                throw new ArgumentException("Maximum number of buffers must be > 0.", nameof(maxBuffers));

            return new GLQuadBatch<TVertex>(this, size, maxBuffers);
        }

        void IRenderer.SetUniform<T>(IUniformWithValue<T> uniform)
        {
            if (uniform.Owner == currentShader)
                flushCurrentBatch();

            switch (uniform)
            {
                case IUniformWithValue<bool> b:
                    GL.Uniform1(uniform.Location, b.GetValue() ? 1 : 0);
                    break;

                case IUniformWithValue<int> i:
                    GL.Uniform1(uniform.Location, i.GetValue());
                    break;

                case IUniformWithValue<float> f:
                    GL.Uniform1(uniform.Location, f.GetValue());
                    break;

                case IUniformWithValue<Vector2> v2:
                    GL.Uniform2(uniform.Location, ref v2.GetValueByRef());
                    break;

                case IUniformWithValue<Vector3> v3:
                    GL.Uniform3(uniform.Location, ref v3.GetValueByRef());
                    break;

                case IUniformWithValue<Vector4> v4:
                    GL.Uniform4(uniform.Location, ref v4.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix2> m2:
                    GL.UniformMatrix2(uniform.Location, false, ref m2.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix3> m3:
                    GL.UniformMatrix3(uniform.Location, false, ref m3.GetValueByRef());
                    break;

                case IUniformWithValue<Matrix4> m4:
                    GL.UniformMatrix4(uniform.Location, false, ref m4.GetValueByRef());
                    break;
            }
        }

        /// <summary>
        /// Notifies that a <see cref="IGLVertexBuffer"/> has begun being used.
        /// </summary>
        /// <param name="buffer">The <see cref="IGLVertexBuffer"/> in use.</param>
        public void RegisterVertexBufferUse(IGLVertexBuffer buffer) => vertexBuffersInUse.Add(buffer);

        /// <summary>
        /// Sets the last vertex batch used for drawing.
        /// <para>
        /// This is done so that various methods that change GL state can force-draw the batch
        /// before continuing with the state change.
        /// </para>
        /// </summary>
        /// <param name="batch">The batch.</param>
        public void SetActiveBatch(IVertexBatch batch)
        {
            if (lastActiveBatch == batch)
                return;

            batchResetList.Add(batch);

            flushCurrentBatch();

            lastActiveBatch = batch;
        }

        void IRenderer.SetDrawDepth(float drawDepth) => BackbufferDrawDepth = drawDepth;

        IVertexBatch<TexturedVertex2D> IRenderer.DefaultQuadBatch => quadBatches.Peek();

        void IRenderer.PushQuadBatch(IVertexBatch<TexturedVertex2D> quadBatch) => quadBatches.Push(quadBatch);

        void IRenderer.PopQuadBatch() => quadBatches.Pop();

        /// <summary>
        /// Deletes a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to delete.</param>
        public void DeleteFrameBuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            while (frameBufferStack.Peek() == frameBuffer)
                UnbindFrameBuffer(frameBuffer);

            ScheduleDisposal(GL.DeleteFramebuffer, frameBuffer);
        }

        private void flushCurrentBatch()
        {
            lastActiveBatch?.Draw();
        }

        public event Action<Texture>? TextureCreated;

        event Action<Texture>? IRenderer.TextureCreated
        {
            add => TextureCreated += value;
            remove => TextureCreated -= value;
        }

        Texture[] IRenderer.GetAllTextures() => allTextures.ToArray();
    }
}
