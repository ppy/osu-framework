// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Development;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.MathUtils;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Colour;
using osu.Framework.Platform;

namespace osu.Framework.Graphics.OpenGL
{
    internal static class GLWrapper
    {
        public static MaskingInfo CurrentMaskingInfo { get; private set; }
        public static RectangleI Viewport { get; private set; }
        public static RectangleF Ortho { get; private set; }
        public static Matrix4 ProjectionMatrix { get; private set; }

        public static bool UsingBackbuffer => lastFrameBuffer == 0;

        /// <summary>
        /// Check whether we have an initialised and non-disposed GL context.
        /// </summary>
        public static bool HasContext => GraphicsContext.CurrentContext != null;

        public static int MaxTextureSize { get; private set; }

        private static readonly Scheduler reset_scheduler = new Scheduler(null); //force no thread set until we are actually on the draw thread.

        /// <summary>
        /// A queue from which a maximum of one operation is invoked per draw frame.
        /// </summary>
        private static readonly ConcurrentQueue<Action> expensive_operations_queue = new ConcurrentQueue<Action>();

        public static bool IsInitialized { get; private set; }

        private static WeakReference<GameHost> host;

        internal static void Initialize(GameHost host)
        {
            if (IsInitialized) return;

            GLWrapper.host = new WeakReference<GameHost>(host);
            reset_scheduler.SetCurrentThread();

            MaxTextureSize = Math.Min(4096, GL.GetInteger(GetPName.MaxTextureSize));

            GL.Disable(EnableCap.DepthTest);
            GL.Disable(EnableCap.StencilTest);
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.ScissorTest);

            IsInitialized = true;
        }

        internal static void ScheduleDisposal(Action disposalAction)
        {
            if (host != null && host.TryGetTarget(out GameHost h))
                h.UpdateThread.Scheduler.Add(() => reset_scheduler.Add(disposalAction.Invoke));
        }

        internal static void Reset(Vector2 size)
        {
            Trace.Assert(shader_stack.Count == 0);

            reset_scheduler.Update();

            if (expensive_operations_queue.TryDequeue(out Action action))
                action.Invoke();

            lastBoundTexture = null;

            lastDepthTest = null;

            lastBlendingInfo = new BlendingInfo();
            lastBlendingEnabledState = null;

            foreach (IVertexBatch b in this_frame_batches)
                b.ResetCounters();

            this_frame_batches.Clear();
            if (lastActiveBatch != null)
                this_frame_batches.Add(lastActiveBatch);

            lastFrameBuffer = 0;

            viewport_stack.Clear();
            ortho_stack.Clear();
            masking_stack.Clear();
            scissor_rect_stack.Clear();

            scissor_rect_stack.Push(new RectangleI(0, 0, (int)size.X, (int)size.Y));

            Viewport = RectangleI.Empty;
            Ortho = RectangleF.Empty;

            PushViewport(new RectangleI(0, 0, (int)size.X, (int)size.Y));
            PushMaskingInfo(new MaskingInfo
            {
                ScreenSpaceAABB = new RectangleI(0, 0, (int)size.X, (int)size.Y),
                MaskingRect = new RectangleF(0, 0, size.X, size.Y),
                ToMaskingSpace = Matrix3.Identity,
                BlendRange = 1,
                AlphaExponent = 1,
            }, true);
        }

        // We initialize to an invalid value such that we are not missing an initial GL.ClearColor call.
        private static Color4 clearColour = new Color4(-1, -1, -1, -1);

        public static void ClearColour(Color4 c)
        {
            if (clearColour != c)
            {
                clearColour = c;
                GL.ClearColor(clearColour);
            }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
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

        private static readonly List<IVertexBatch> this_frame_batches = new List<IVertexBatch>();

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
            if (lastActiveBatch == batch) return;

            FlushCurrentBatch();

            if (batch != null && !this_frame_batches.Contains(batch))
                this_frame_batches.Add(batch);

            lastActiveBatch = batch;
        }

        private static TextureGL lastBoundTexture;

        internal static bool AtlasTextureIsBound => lastBoundTexture is TextureGLAtlas;

        /// <summary>
        /// Binds a texture to darw with.
        /// </summary>
        /// <param name="texture"></param>
        public static void BindTexture(TextureGL texture)
        {
            if (lastBoundTexture != texture)
            {
                FlushCurrentBatch();

                GL.BindTexture(TextureTarget.Texture2D, texture?.TextureId ?? 0);
                lastBoundTexture = texture;

                FrameStatistics.Increment(StatisticsCounterType.TextureBinds);
            }
        }

        private static bool? lastDepthTest;

        public static void SetDepthTest(bool enabled)
        {
            if (lastDepthTest == enabled)
                return;

            lastDepthTest = enabled;

            FlushCurrentBatch();

            if (enabled)
                GL.Enable(EnableCap.DepthTest);
            else
                GL.Disable(EnableCap.DepthTest);
        }

        private static BlendingInfo lastBlendingInfo;
        private static bool? lastBlendingEnabledState;

        /// <summary>
        /// Sets the blending function to draw with.
        /// </summary>
        /// <param name="blendingInfo">The infor we should use to update the active state.</param>
        public static void SetBlend(BlendingInfo blendingInfo)
        {
            if (lastBlendingInfo.Equals(blendingInfo))
                return;

            FlushCurrentBatch();

            if (blendingInfo.IsDisabled)
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

                GL.BlendEquationSeparate(blendingInfo.RGBEquation, blendingInfo.AlphaEquation);
                GL.BlendFuncSeparate(blendingInfo.Source, blendingInfo.Destination, blendingInfo.SourceAlpha, blendingInfo.DestinationAlpha);
            }

            lastBlendingInfo = blendingInfo;
        }

        private static int lastFrameBuffer;

        /// <summary>
        /// Binds a framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to bind.</param>
        /// <returns>The last bound framebuffer.</returns>
        public static int BindFrameBuffer(int frameBuffer)
        {
            if (lastFrameBuffer == frameBuffer)
                return lastFrameBuffer;

            FlushCurrentBatch();

            int last = lastFrameBuffer;

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
            lastFrameBuffer = frameBuffer;

            return last;
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
            Shader.SetGlobalProperty(@"g_ProjMatrix", ProjectionMatrix);

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
            Shader.SetGlobalProperty(@"g_ProjMatrix", ProjectionMatrix);

            UpdateScissorToCurrentViewportAndOrtho();
        }

        private static readonly Stack<MaskingInfo> masking_stack = new Stack<MaskingInfo>();
        private static readonly Stack<RectangleI> scissor_rect_stack = new Stack<RectangleI>();

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

            Shader.SetGlobalProperty(@"g_MaskingRect", new Vector4(
                maskingInfo.MaskingRect.Left,
                maskingInfo.MaskingRect.Top,
                maskingInfo.MaskingRect.Right,
                maskingInfo.MaskingRect.Bottom));

            Shader.SetGlobalProperty(@"g_ToMaskingSpace", maskingInfo.ToMaskingSpace);
            Shader.SetGlobalProperty(@"g_CornerRadius", maskingInfo.CornerRadius);

            Shader.SetGlobalProperty(@"g_BorderThickness", maskingInfo.BorderThickness / maskingInfo.BlendRange);
            Shader.SetGlobalProperty(@"g_BorderColour", new Vector4(
                maskingInfo.BorderColour.Linear.R,
                maskingInfo.BorderColour.Linear.G,
                maskingInfo.BorderColour.Linear.B,
                maskingInfo.BorderColour.Linear.A));

            Shader.SetGlobalProperty(@"g_MaskingBlendRange", maskingInfo.BlendRange);
            Shader.SetGlobalProperty(@"g_AlphaExponent", maskingInfo.AlphaExponent);

            Shader.SetGlobalProperty(@"g_DiscardInner", maskingInfo.Hollow);

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
                RectangleI currentScissorRect;
                if (overwritePreviousScissor)
                    currentScissorRect = actualRect;
                else
                {
                    currentScissorRect = scissor_rect_stack.Peek();
                    currentScissorRect.Intersect(actualRect);
                }

                scissor_rect_stack.Push(currentScissorRect);
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
        /// Deletes a framebuffer.
        /// </summary>
        /// <param name="frameBuffer">The framebuffer to delete.</param>
        internal static void DeleteFramebuffer(int frameBuffer)
        {
            if (frameBuffer == -1) return;

            //todo: don't use scheduler
            ScheduleDisposal(() => { GL.DeleteFramebuffer(frameBuffer); });
        }

        /// <summary>
        /// Deletes a buffer object.
        /// </summary>
        /// <param name="vboId">The buffer object to delete.</param>
        internal static void DeleteBuffer(int vboId)
        {
            //todo: don't use scheduler
            ScheduleDisposal(() => { GL.DeleteBuffer(vboId); });
        }

        /// <summary>
        /// Deletes textures.
        /// </summary>
        /// <param name="ids">An array of textures to delete.</param>
        internal static void DeleteTextures(params int[] ids)
        {
            //todo: don't use scheduler
            ScheduleDisposal(() => { GL.DeleteTextures(ids.Length, ids); });
        }

        /// <summary>
        /// Deletes a shader program.
        /// </summary>
        /// <param name="shader">The shader program to delete.</param>
        internal static void DeleteProgram(Shader shader)
        {
            //todo: don't use scheduler
            ScheduleDisposal(() => { GL.DeleteProgram(shader); });
        }

        /// <summary>
        /// Deletes a shader part.
        /// </summary>
        /// <param name="shaderPart">The shader part to delete.</param>
        internal static void DeleteShader(ShaderPart shaderPart)
        {
            //todo: don't use scheduler
            ScheduleDisposal(() => { GL.DeleteShader(shaderPart); });
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

            FlushCurrentBatch();

            GL.UseProgram(s);
            currentShader = s;
        }

        public static void SetUniform(int shader, ActiveUniformType type, int location, object value)
        {
            if (shader == currentShader)
                FlushCurrentBatch();

            switch (type)
            {
                case ActiveUniformType.Bool:
                    GL.Uniform1(location, (bool)value ? 1 : 0);
                    break;
                case ActiveUniformType.Int:
                    GL.Uniform1(location, (int)value);
                    break;
                case ActiveUniformType.Float:
                    GL.Uniform1(location, (float)value);
                    break;
                case ActiveUniformType.BoolVec2:
                case ActiveUniformType.IntVec2:
                case ActiveUniformType.FloatVec2:
                    GL.Uniform2(location, (Vector2)value);
                    break;
                case ActiveUniformType.FloatMat2:
                    {
                        Matrix2 mat = (Matrix2)value;
                        GL.UniformMatrix2(location, false, ref mat);
                        break;
                    }
                case ActiveUniformType.BoolVec3:
                case ActiveUniformType.IntVec3:
                case ActiveUniformType.FloatVec3:
                    GL.Uniform3(location, (Vector3)value);
                    break;
                case ActiveUniformType.FloatMat3:
                    {
                        Matrix3 mat = (Matrix3)value;
                        GL.UniformMatrix3(location, false, ref mat);
                        break;
                    }
                case ActiveUniformType.BoolVec4:
                case ActiveUniformType.IntVec4:
                case ActiveUniformType.FloatVec4:
                    GL.Uniform4(location, (Vector4)value);
                    break;
                case ActiveUniformType.FloatMat4:
                    {
                        Matrix4 mat = (Matrix4)value;
                        GL.UniformMatrix4(location, false, ref mat);
                        break;
                    }
                case ActiveUniformType.Sampler2D:
                    GL.Uniform1(location, (int)value);
                    break;
            }
        }
    }

    public struct MaskingInfo : IEquatable<MaskingInfo>
    {
        public RectangleI ScreenSpaceAABB;
        public RectangleF MaskingRect;

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

        public bool Hollow;

        public bool Equals(MaskingInfo other)
        {
            return
                ScreenSpaceAABB == other.ScreenSpaceAABB &&
                MaskingRect == other.MaskingRect &&
                ToMaskingSpace == other.ToMaskingSpace &&
                CornerRadius == other.CornerRadius &&
                BorderThickness == other.BorderThickness &&
                BorderColour.Equals(other.BorderColour) &&
                BlendRange == other.BlendRange &&
                AlphaExponent == other.AlphaExponent &&
                Hollow == other.Hollow;
        }
    }
}
