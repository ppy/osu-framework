// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Development;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Threading;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.MathUtils;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Platform;
using GameWindow = osu.Framework.Platform.GameWindow;

namespace osu.Framework.Graphics.OpenGL
{
    public static class GLWrapper
    {
        /// <summary>
        /// Maximum number of <see cref="DrawNode"/>s a <see cref="Drawable"/> can draw with.
        /// This is a carefully-chosen number to enable the update and draw threads to work concurrently without causing unnecessary load.
        /// </summary>
        public const int MAX_DRAW_NODES = 3;

        public static MaskingInfo CurrentMaskingInfo { get; private set; }
        public static RectangleI Viewport { get; private set; }
        public static RectangleF Ortho { get; private set; }
        public static Matrix4 ProjectionMatrix { get; private set; }
        public static DepthInfo CurrentDepthInfo { get; private set; }

        public static float BackbufferDrawDepth { get; private set; }

        public static bool UsingBackbuffer => frame_buffer_stack.Peek() == DefaultFrameBuffer;

        public static int DefaultFrameBuffer;

        public static bool IsEmbedded { get; private set; }

        /// <summary>
        /// Check whether we have an initialised and non-disposed GL context.
        /// </summary>
        public static bool HasContext => GraphicsContext.CurrentContext != null;

        public static int MaxTextureSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.
        public static int MaxRenderBufferSize { get; private set; } = 4096; // default value is to allow roughly normal flow in cases we don't have a GL context, like headless CI.

        private static readonly Scheduler reset_scheduler = new Scheduler(null); // force no thread set until we are actually on the draw thread.

        /// <summary>
        /// A queue from which a maximum of one operation is invoked per draw frame.
        /// </summary>
        private static readonly ConcurrentQueue<Action> expensive_operations_queue = new ConcurrentQueue<Action>();

        private static readonly List<IVertexBatch> batch_reset_list = new List<IVertexBatch>();

        public static bool IsInitialized { get; private set; }

        private static WeakReference<GameHost> host;

        internal static void Initialize(GameHost host)
        {
            if (IsInitialized) return;

            if (host.Window is GameWindow win)
                IsEmbedded = win.IsEmbedded;

            GLWrapper.host = new WeakReference<GameHost>(host);
            reset_scheduler.SetCurrentThread();

            MaxTextureSize = GL.GetInteger(GetPName.MaxTextureSize);
            MaxRenderBufferSize = GL.GetInteger(GetPName.MaxRenderbufferSize);

            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.Blend);

            IsInitialized = true;
        }

        internal static void ScheduleDisposal(Action disposalAction)
        {
            int frameCount = 0;

            if (host != null && host.TryGetTarget(out GameHost h))
                h.UpdateThread.Scheduler.Add(scheduleNextDisposal);
            else
                disposalAction.Invoke();

            void scheduleNextDisposal() => reset_scheduler.Add(() =>
            {
                // There may be a number of DrawNodes queued to be drawn
                // Disposal should only take place after
                if (frameCount++ >= MAX_DRAW_NODES)
                    disposalAction.Invoke();
                else
                    scheduleNextDisposal();
            });
        }

        internal static void Reset(Vector2 size)
        {
            Trace.Assert(shader_stack.Count == 0);

            reset_scheduler.Update();

            if (expensive_operations_queue.TryDequeue(out Action action))
                action.Invoke();

            Array.Clear(last_bound_texture, 0, last_bound_texture.Length);
            lastActiveBatch = null;
            lastBlendingParameters = new BlendingParameters();
            lastBlendingEnabledState = null;

            foreach (var b in batch_reset_list)
                b.ResetCounters();
            batch_reset_list.Clear();

            viewport_stack.Clear();
            ortho_stack.Clear();
            masking_stack.Clear();
            scissor_rect_stack.Clear();
            frame_buffer_stack.Clear();
            depth_stack.Clear();
            scissor_state_stack.Clear();

            BindFrameBuffer(DefaultFrameBuffer);

            scissor_rect_stack.Push(new RectangleI(0, 0, (int)size.X, (int)size.Y));

            Viewport = RectangleI.Empty;
            Ortho = RectangleF.Empty;

            PushScissorState(true);
            PushViewport(new RectangleI(0, 0, (int)size.X, (int)size.Y));
            PushMaskingInfo(new MaskingInfo
            {
                ScreenSpaceAABB = new RectangleI(0, 0, (int)size.X, (int)size.Y),
                MaskingRect = new RectangleF(0, 0, size.X, size.Y),
                ToMaskingSpace = Matrix3.Identity,
                BlendRange = 1,
                AlphaExponent = 1,
            }, true);

            PushDepthInfo(DepthInfo.Default);
            Clear(new ClearInfo(Color4.Black));
        }

        private static ClearInfo currentClearInfo;

        public static void Clear(ClearInfo clearInfo)
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

        private static readonly Stack<bool> scissor_state_stack = new Stack<bool>();

        private static bool currentScissorState;

        public static void PushScissorState(bool enabled)
        {
            scissor_state_stack.Push(enabled);
            setScissorState(enabled);
        }

        public static void PopScissorState()
        {
            Trace.Assert(scissor_state_stack.Count > 1);

            scissor_state_stack.Pop();

            setScissorState(scissor_state_stack.Peek());
        }

        private static void setScissorState(bool enabled)
        {
            if (enabled == currentScissorState)
                return;

            currentScissorState = enabled;

            if (enabled)
                GL.Enable(EnableCap.ScissorTest);
            else
                GL.Disable(EnableCap.ScissorTest);
        }

        /// <summary>
        /// Enqueues a texture to be uploaded in the next frame.
        /// </summary>
        /// <param name="texture">The texture to be uploaded.</param>
        public static void EnqueueTextureUpload(TextureGL texture)
        {
            if (host != null)
                expensive_operations_queue.Enqueue(() => texture.Upload());
        }

        /// <summary>
        /// Enqueues the compile of a shader.
        /// </summary>
        /// <param name="shader">The shader to compile.</param>
        public static void EnqueueShaderCompile(Shader shader)
        {
            if (host != null)
                expensive_operations_queue.Enqueue(shader.EnsureLoaded);
        }

        private static readonly int[] last_bound_buffers = new int[2];

        /// <summary>
        /// Bind an OpenGL buffer object.
        /// </summary>
        /// <param name="target">The buffer type to bind.</param>
        /// <param name="buffer">The buffer ID to bind.</param>
        /// <returns>Whether an actual bind call was necessary. This value is false when repeatedly binding the same buffer.</returns>
        public static bool BindBuffer(BufferTarget target, int buffer)
        {
            int bufferIndex = target - BufferTarget.ArrayBuffer;
            if (last_bound_buffers[bufferIndex] == buffer)
                return false;

            last_bound_buffers[bufferIndex] = buffer;
            GL.BindBuffer(target, buffer);

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);

            return true;
        }

        private static IVertexBatch lastActiveBatch;

        /// <summary>
        /// Sets the last vertex batch used for drawing.
        /// <para>
        /// This is done so that various methods that change GL state can force-draw the batch
        /// before continuing with the state change.
        /// </para>
        /// </summary>
        /// <param name="batch">The batch.</param>
        internal static void SetActiveBatch(IVertexBatch batch)
        {
            if (lastActiveBatch == batch)
                return;

            batch_reset_list.Add(batch);

            FlushCurrentBatch();

            lastActiveBatch = batch;
        }

        private static readonly TextureGL[] last_bound_texture = new TextureGL[16];

        internal static int GetTextureUnitId(TextureUnit unit) => (int)unit - (int)TextureUnit.Texture0;
        internal static bool AtlasTextureIsBound(TextureUnit unit) => last_bound_texture[GetTextureUnitId(unit)] is TextureGLAtlas;

        /// <summary>
        /// Binds a texture to darw with.
        /// </summary>
        /// <param name="texture">The texture to bind.</param>
        /// <param name="unit">The texture unit to bind it to.</param>
        public static void BindTexture(TextureGL texture, TextureUnit unit = TextureUnit.Texture0)
        {
            var index = GetTextureUnitId(unit);

            if (last_bound_texture[index] != texture)
            {
                FlushCurrentBatch();

                GL.ActiveTexture(unit);
                GL.BindTexture(TextureTarget.Texture2D, texture?.TextureId ?? 0);
                last_bound_texture[index] = texture;

                FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            }
        }

        private static BlendingParameters lastBlendingParameters;
        private static bool? lastBlendingEnabledState;

        /// <summary>
        /// Sets the blending function to draw with.
        /// </summary>
        /// <param name="blendingParameters">The info we should use to update the active state.</param>
        public static void SetBlend(BlendingParameters blendingParameters)
        {
            if (lastBlendingParameters.Equals(blendingParameters))
                return;

            FlushCurrentBatch();

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

        private static readonly Stack<RectangleI> viewport_stack = new Stack<RectangleI>();

        /// <summary>
        /// Applies a new viewport rectangle.
        /// </summary>
        /// <param name="viewport">The viewport rectangle.</param>
        public static void PushViewport(RectangleI viewport)
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

            PushOrtho(viewport);

            viewport_stack.Push(actualRect);

            if (Viewport == actualRect)
                return;

            Viewport = actualRect;

            GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);

            UpdateScissorToCurrentViewportAndOrtho();
        }

        /// <summary>
        /// Applies the last viewport rectangle.
        /// </summary>
        public static void PopViewport()
        {
            Trace.Assert(viewport_stack.Count > 1);

            PopOrtho();

            viewport_stack.Pop();
            RectangleI actualRect = viewport_stack.Peek();

            if (Viewport == actualRect)
                return;

            Viewport = actualRect;

            GL.Viewport(Viewport.Left, Viewport.Top, Viewport.Width, Viewport.Height);

            UpdateScissorToCurrentViewportAndOrtho();
        }

        private static readonly Stack<RectangleF> ortho_stack = new Stack<RectangleF>();

        /// <summary>
        /// Applies a new orthographic projection rectangle.
        /// </summary>
        /// <param name="ortho">The orthographic projection rectangle.</param>
        public static void PushOrtho(RectangleF ortho)
        {
            FlushCurrentBatch();

            ortho_stack.Push(ortho);
            if (Ortho == ortho)
                return;

            Ortho = ortho;

            ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(Ortho.Left, Ortho.Right, Ortho.Bottom, Ortho.Top, -1, 1);
            GlobalPropertyManager.Set(GlobalProperty.ProjMatrix, ProjectionMatrix);

            UpdateScissorToCurrentViewportAndOrtho();
        }

        /// <summary>
        /// Applies the last orthographic projection rectangle.
        /// </summary>
        public static void PopOrtho()
        {
            Trace.Assert(ortho_stack.Count > 1);

            FlushCurrentBatch();

            ortho_stack.Pop();
            RectangleF actualRect = ortho_stack.Peek();

            if (Ortho == actualRect)
                return;

            Ortho = actualRect;

            ProjectionMatrix = Matrix4.CreateOrthographicOffCenter(Ortho.Left, Ortho.Right, Ortho.Bottom, Ortho.Top, -1, 1);
            GlobalPropertyManager.Set(GlobalProperty.ProjMatrix, ProjectionMatrix);

            UpdateScissorToCurrentViewportAndOrtho();
        }

        private static readonly Stack<MaskingInfo> masking_stack = new Stack<MaskingInfo>();
        private static readonly Stack<RectangleI> scissor_rect_stack = new Stack<RectangleI>();
        private static readonly Stack<int> frame_buffer_stack = new Stack<int>();
        private static readonly Stack<DepthInfo> depth_stack = new Stack<DepthInfo>();

        public static void UpdateScissorToCurrentViewportAndOrtho()
        {
            RectangleF viewportRect = Viewport;
            Vector2 offset = viewportRect.TopLeft - Ortho.TopLeft;

            RectangleI currentScissorRect = scissor_rect_stack.Peek();

            RectangleI scissorRect = new RectangleI(
                currentScissorRect.X + (int)Math.Floor(offset.X),
                Viewport.Height - currentScissorRect.Bottom - (int)Math.Ceiling(offset.Y),
                currentScissorRect.Width,
                currentScissorRect.Height);

            if (!Precision.AlmostEquals(offset, Vector2.Zero))
            {
                ++scissorRect.Width;
                ++scissorRect.Height;
            }

            GL.Scissor(scissorRect.X, scissorRect.Y, scissorRect.Width, scissorRect.Height);
        }

        private static void setMaskingInfo(MaskingInfo maskingInfo, bool isPushing, bool overwritePreviousScissor)
        {
            FlushCurrentBatch();

            GlobalPropertyManager.Set(GlobalProperty.MaskingRect, new Vector4(
                maskingInfo.MaskingRect.Left,
                maskingInfo.MaskingRect.Top,
                maskingInfo.MaskingRect.Right,
                maskingInfo.MaskingRect.Bottom));

            GlobalPropertyManager.Set(GlobalProperty.ToMaskingSpace, maskingInfo.ToMaskingSpace);
            GlobalPropertyManager.Set(GlobalProperty.CornerRadius, maskingInfo.CornerRadius);

            GlobalPropertyManager.Set(GlobalProperty.BorderThickness, maskingInfo.BorderThickness / maskingInfo.BlendRange);

            if (maskingInfo.BorderThickness > 0)
            {
                GlobalPropertyManager.Set(GlobalProperty.BorderColour, new Vector4(
                    maskingInfo.BorderColour.Linear.R,
                    maskingInfo.BorderColour.Linear.G,
                    maskingInfo.BorderColour.Linear.B,
                    maskingInfo.BorderColour.Linear.A));
            }

            GlobalPropertyManager.Set(GlobalProperty.MaskingBlendRange, maskingInfo.BlendRange);
            GlobalPropertyManager.Set(GlobalProperty.AlphaExponent, maskingInfo.AlphaExponent);

            GlobalPropertyManager.Set(GlobalProperty.EdgeOffset, maskingInfo.EdgeOffset);

            GlobalPropertyManager.Set(GlobalProperty.DiscardInner, maskingInfo.Hollow);
            if (maskingInfo.Hollow)
                GlobalPropertyManager.Set(GlobalProperty.InnerCornerRadius, maskingInfo.HollowCornerRadius);

            RectangleI actualRect = maskingInfo.ScreenSpaceAABB;
            actualRect.X += Viewport.X;
            actualRect.Y += Viewport.Y;

            // Ensure the rectangle only has positive width and height. (Required by OGL)
            if (actualRect.Width < 0)
            {
                actualRect.X += actualRect.Width;
                actualRect.Width = -actualRect.Width;
            }

            if (actualRect.Height < 0)
            {
                actualRect.Y += actualRect.Height;
                actualRect.Height = -actualRect.Height;
            }

            if (isPushing)
            {
                scissor_rect_stack.Push(overwritePreviousScissor ? actualRect : RectangleI.Intersect(scissor_rect_stack.Peek(), actualRect));
            }
            else
            {
                Trace.Assert(scissor_rect_stack.Count > 1);
                scissor_rect_stack.Pop();
            }

            UpdateScissorToCurrentViewportAndOrtho();
        }

        internal static void FlushCurrentBatch()
        {
            lastActiveBatch?.Draw();
        }

        public static bool IsMaskingActive => masking_stack.Count > 1;

        /// <summary>
        /// Applies a new scissor rectangle.
        /// </summary>
        /// <param name="maskingInfo">The masking info.</param>
        /// <param name="overwritePreviousScissor">Whether or not to shrink an existing scissor rectangle.</param>
        public static void PushMaskingInfo(MaskingInfo maskingInfo, bool overwritePreviousScissor = false)
        {
            masking_stack.Push(maskingInfo);
            if (CurrentMaskingInfo.Equals(maskingInfo))
                return;

            CurrentMaskingInfo = maskingInfo;
            setMaskingInfo(CurrentMaskingInfo, true, overwritePreviousScissor);
        }

        /// <summary>
        /// Applies the last scissor rectangle.
        /// </summary>
        public static void PopMaskingInfo()
        {
            Trace.Assert(masking_stack.Count > 1);

            masking_stack.Pop();
            MaskingInfo maskingInfo = masking_stack.Peek();

            if (CurrentMaskingInfo.Equals(maskingInfo))
                return;

            CurrentMaskingInfo = maskingInfo;
            setMaskingInfo(CurrentMaskingInfo, false, true);
        }

        /// <summary>
        /// Applies a new depth information.
        /// </summary>
        /// <param name="depthInfo">The depth information.</param>
        public static void PushDepthInfo(DepthInfo depthInfo)
        {
            depth_stack.Push(depthInfo);

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        /// <summary>
        /// Applies the last depth information.
        /// </summary>
        public static void PopDepthInfo()
        {
            Trace.Assert(depth_stack.Count > 1);

            depth_stack.Pop();
            DepthInfo depthInfo = depth_stack.Peek();

            if (CurrentDepthInfo.Equals(depthInfo))
                return;

            CurrentDepthInfo = depthInfo;
            setDepthInfo(CurrentDepthInfo);
        }

        private static void setDepthInfo(DepthInfo depthInfo)
        {
            FlushCurrentBatch();

            if (depthInfo.DepthTest)
            {
                GL.Enable(EnableCap.DepthTest);
                GL.DepthFunc(depthInfo.Function);
            }
            else
                GL.Disable(EnableCap.DepthTest);

            GL.DepthMask(depthInfo.WriteDepth);
        }

        /// <summary>
        /// Sets the current draw depth.
        /// The draw depth is written to every vertex added to <see cref="VertexBuffer{T}"/>s.
        /// </summary>
        /// <param name="drawDepth">The draw depth.</param>
        internal static void SetDrawDepth(float drawDepth) => BackbufferDrawDepth = drawDepth;

        /// <summary>
        /// Binds a framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to bind.</param>
        public static void BindFrameBuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            bool alreadyBound = frame_buffer_stack.Count > 0 && frame_buffer_stack.Peek() == frameBuffer;

            frame_buffer_stack.Push(frameBuffer);

            if (!alreadyBound)
            {
                FlushCurrentBatch();
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
                GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            }

            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        /// <summary>
        /// Binds a framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to bind.</param>
        public static void UnbindFrameBuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            if (frame_buffer_stack.Peek() != frameBuffer)
                return;

            frame_buffer_stack.Pop();

            FlushCurrentBatch();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frame_buffer_stack.Peek());

            GlobalPropertyManager.Set(GlobalProperty.BackbufferDraw, UsingBackbuffer);
            GlobalPropertyManager.Set(GlobalProperty.GammaCorrection, UsingBackbuffer);
        }

        /// <summary>
        /// Deletes a frame buffer.
        /// </summary>
        /// <param name="frameBuffer">The frame buffer to delete.</param>
        internal static void DeleteFrameBuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            while (frame_buffer_stack.Peek() == frameBuffer)
                UnbindFrameBuffer(frameBuffer);

            ScheduleDisposal(() => { GL.DeleteFramebuffer(frameBuffer); });
        }

        private static int currentShader;

        private static readonly Stack<int> shader_stack = new Stack<int>();

        public static void UseProgram(int? shader)
        {
            ThreadSafety.EnsureDrawThread();

            if (shader != null)
            {
                shader_stack.Push(shader.Value);
            }
            else
            {
                shader_stack.Pop();

                //check if the stack is empty, and if so don't restore the previous shader.
                if (shader_stack.Count == 0)
                    return;
            }

            int s = shader ?? shader_stack.Peek();

            if (currentShader == s) return;

            FrameStatistics.Increment(StatisticsCounterType.ShaderBinds);

            FlushCurrentBatch();

            GL.UseProgram(s);
            currentShader = s;
        }

        internal static void SetUniform<T>(IUniformWithValue<T> uniform)
            where T : struct, IEquatable<T>
        {
            if (uniform.Owner == currentShader)
                FlushCurrentBatch();

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
    }

    public struct MaskingInfo : IEquatable<MaskingInfo>
    {
        public RectangleI ScreenSpaceAABB;
        public RectangleF MaskingRect;

        public Quad ConservativeScreenSpaceQuad;

        /// <summary>
        /// This matrix transforms screen space coordinates to masking space (likely the parent
        /// space of the container doing the masking).
        /// It is used by a shader to determine which pixels to discard.
        /// </summary>
        public Matrix3 ToMaskingSpace;

        public float CornerRadius;

        public float BorderThickness;
        public SRGBColour BorderColour;

        public float BlendRange;
        public float AlphaExponent;

        public Vector2 EdgeOffset;

        public bool Hollow;
        public float HollowCornerRadius;

        public bool Equals(MaskingInfo other) =>
            ScreenSpaceAABB == other.ScreenSpaceAABB &&
            MaskingRect == other.MaskingRect &&
            ToMaskingSpace == other.ToMaskingSpace &&
            CornerRadius == other.CornerRadius &&
            BorderThickness == other.BorderThickness &&
            BorderColour.Equals(other.BorderColour) &&
            BlendRange == other.BlendRange &&
            AlphaExponent == other.AlphaExponent &&
            EdgeOffset == other.EdgeOffset &&
            Hollow == other.Hollow &&
            HollowCornerRadius == other.HollowCornerRadius;
    }
}
