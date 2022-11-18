// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Buffers;
using osuTK.Graphics.ES30;
using osu.Framework.Statistics;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Platform;
using SixLabors.ImageSharp.Memory;
using osu.Framework.Graphics.Shaders;
using osuTK;
using System.Collections.Generic;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal abstract class GLVertexBuffer<T> : IVertexBuffer, IDisposable
        where T : unmanaged, IEquatable<T>, IVertex
    {
        protected static readonly int STRIDE = GLVertexUtils<DepthWrappingVertex<T>>.STRIDE;

        protected readonly GLRenderer Renderer;
        private readonly BufferUsageHint usage;

        private Memory<DepthWrappingVertex<T>> vertexMemory;
        private IMemoryOwner<DepthWrappingVertex<T>> memoryOwner;
        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        private int vboId = -1;

        protected GLVertexBuffer(GLRenderer renderer, int amountVertices, BufferUsageHint usage)
        {
            Renderer = renderer;
            this.usage = usage;

            Size = amountVertices;
        }

        /// <summary>
        /// Sets the vertex at a specific index of this <see cref="GLVertexBuffer{T}"/>.
        /// </summary>
        /// <param name="vertexIndex">The index of the vertex.</param>
        /// <param name="vertex">The vertex.</param>
        /// <returns>Whether the vertex changed.</returns>
        public bool SetVertex(int vertexIndex, T vertex)
        {
            ref var currentVertex = ref getMemory().Span[vertexIndex];

            bool isNewVertex = !currentVertex.Vertex.Equals(vertex)
                               || currentVertex.BackbufferDrawDepth != Renderer.BackbufferDrawDepth;

            int localId = addMaskingInfo(Renderer.CurrentMaskingInfo);

            currentVertex.Vertex = vertex;
            currentVertex.MaskingTexCoord = new Vector2((localId * MASKING_DATA_LENGTH / 4) % MASKING_TEXTURE_WIDTH, (localId * MASKING_DATA_LENGTH / 4) / MASKING_TEXTURE_WIDTH);
            currentVertex.BackbufferDrawDepth = Renderer.BackbufferDrawDepth;

            return isNewVertex;
        }

        /// <summary>
        /// Gets the number of vertices in this <see cref="GLVertexBuffer{T}"/>.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Initialises this <see cref="GLVertexBuffer{T}"/>. Guaranteed to be run on the draw thread.
        /// </summary>
        protected virtual void Initialise()
        {
            ThreadSafety.EnsureDrawThread();

            GL.GenBuffers(1, out vboId);

            if (Renderer.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                GLVertexUtils<DepthWrappingVertex<T>>.Bind();

            int size = Size * STRIDE;

            memoryLease = NativeMemoryTracker.AddMemory(this, Size * STRIDE);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)size, IntPtr.Zero, usage);
        }

        ~GLVertexBuffer()
        {
            Renderer.ScheduleDisposal(v => v.Dispose(false), this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsDisposed { get; private set; }

        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            ((IVertexBuffer)this).Free();

            IsDisposed = true;
        }

        public virtual void Bind(bool forRendering)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not bind disposed vertex buffers.");

            if (vboId == -1)
                Initialise();

            if (Renderer.BindBuffer(BufferTarget.ArrayBuffer, vboId))
                GLVertexUtils<DepthWrappingVertex<T>>.Bind();
        }

        public virtual void Unbind()
        {
        }

        protected virtual int ToElements(int vertices) => vertices;

        protected virtual int ToElementIndex(int vertexIndex) => vertexIndex;

        protected abstract PrimitiveType Type { get; }

        public void Draw()
        {
            DrawRange(0, Size);
        }

        public const int MASKING_DATA_LENGTH = 64;
        public const int MASKING_TEXTURE_WIDTH = MASKING_DATA_LENGTH / 4; // Single masking info wide for now

        protected int MaskingTextureHeight = 0;
        protected float[] MaskingTextureBuffer = new float[MASKING_TEXTURE_WIDTH * 4];

        private int maskingTexture = -1;

        private int numMaskingInfos;

        private int numUploadedMaskingInfos;

        private Dictionary<int, int> localMaskingInfoIndex = new Dictionary<int, int>();

        private int addMaskingInfo(MaskingInfo info)
        {
            int localId;

            if (localMaskingInfoIndex.TryGetValue(info.Id, out localId))
            {
                return localId;
            }

            localId = numMaskingInfos++;
            localMaskingInfoIndex.Add(info.Id, localId);

            int index = localId * MASKING_DATA_LENGTH;

            // Ensure we have enough space for this masking data by doubling the array size every time.
            if (index >= MaskingTextureBuffer.Length)
                Array.Resize(ref MaskingTextureBuffer, Math.Max(MASKING_DATA_LENGTH, MaskingTextureBuffer.Length * 2));

            float realBorderThickness = info.BorderThickness / info.BlendRange;

            int i = index;

            MaskingTextureBuffer[i++] = info.Id == 0 ? 0.0f : 1.0f;
            MaskingTextureBuffer[i++] = info.ToMaskingSpace.M11;
            MaskingTextureBuffer[i++] = info.ToMaskingSpace.M12;
            MaskingTextureBuffer[i++] = info.ToMaskingSpace.M13;

            MaskingTextureBuffer[i++] = info.ToMaskingSpace.M21;
            MaskingTextureBuffer[i++] = info.ToMaskingSpace.M22;
            MaskingTextureBuffer[i++] = info.ToMaskingSpace.M23;
            MaskingTextureBuffer[i++] = info.ToMaskingSpace.M31;

            MaskingTextureBuffer[i++] = info.ToMaskingSpace.M32;
            MaskingTextureBuffer[i++] = info.ToMaskingSpace.M33;
            MaskingTextureBuffer[i++] = info.CornerRadius;
            MaskingTextureBuffer[i++] = info.CornerExponent;

            MaskingTextureBuffer[i++] = info.MaskingRect.Left;
            MaskingTextureBuffer[i++] = info.MaskingRect.Top;
            MaskingTextureBuffer[i++] = info.MaskingRect.Right;
            MaskingTextureBuffer[i++] = info.MaskingRect.Bottom;

            MaskingTextureBuffer[i++] = info.BorderColour.TopLeft.Linear.R;
            MaskingTextureBuffer[i++] = info.BorderColour.TopLeft.Linear.G;
            MaskingTextureBuffer[i++] = info.BorderColour.TopLeft.Linear.B;
            MaskingTextureBuffer[i++] = info.BorderColour.TopLeft.Linear.A;

            MaskingTextureBuffer[i++] = info.BorderColour.BottomLeft.Linear.R;
            MaskingTextureBuffer[i++] = info.BorderColour.BottomLeft.Linear.G;
            MaskingTextureBuffer[i++] = info.BorderColour.BottomLeft.Linear.B;
            MaskingTextureBuffer[i++] = info.BorderColour.BottomLeft.Linear.A;

            MaskingTextureBuffer[i++] = info.BorderColour.TopRight.Linear.R;
            MaskingTextureBuffer[i++] = info.BorderColour.TopRight.Linear.G;
            MaskingTextureBuffer[i++] = info.BorderColour.TopRight.Linear.B;
            MaskingTextureBuffer[i++] = info.BorderColour.TopRight.Linear.A;

            MaskingTextureBuffer[i++] = info.BorderColour.BottomRight.Linear.R;
            MaskingTextureBuffer[i++] = info.BorderColour.BottomRight.Linear.G;
            MaskingTextureBuffer[i++] = info.BorderColour.BottomRight.Linear.B;
            MaskingTextureBuffer[i++] = info.BorderColour.BottomRight.Linear.A;

            MaskingTextureBuffer[i++] = realBorderThickness;
            MaskingTextureBuffer[i++] = info.BlendRange;
            MaskingTextureBuffer[i++] = info.AlphaExponent;
            MaskingTextureBuffer[i++] = info.Hollow ? 1 : 0;

            MaskingTextureBuffer[i++] = info.EdgeOffset.X;
            MaskingTextureBuffer[i++] = info.EdgeOffset.Y;
            MaskingTextureBuffer[i++] = info.HollowCornerRadius;

            return localId;
        }

        private void uploadMaskingTexture()
        {
            if (MaskingTextureBuffer.Length == 0)
                return;

            GL.ActiveTexture(TextureUnit.Texture10);

            if (maskingTexture == -1)
            {
                maskingTexture = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, maskingTexture);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Nearest);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Nearest);
            }

            GL.BindTexture(TextureTarget.Texture2D, maskingTexture);

            int totalTexels = numMaskingInfos * MASKING_DATA_LENGTH / 4;
            int currentMaskingTextureHeight = (totalTexels + MASKING_TEXTURE_WIDTH - 1) / MASKING_TEXTURE_WIDTH;
            // if (MaskingTextureHeight < currentMaskingTextureHeight) {
            MaskingTextureHeight = currentMaskingTextureHeight;
            GL.TexImage2D(All.Texture2D, 0, All.Rgba32f, MASKING_TEXTURE_WIDTH, MaskingTextureHeight, 0, All.Rgba, All.Float, ref MaskingTextureBuffer[0]);
            // } else if (numUploadedMaskingInfos < currentMaskingTextureHeight) {
            //     GL.TexSubImage2D(All.Texture2D, 0, 0, numUploadedMaskingInfos, MASKING_TEXTURE_WIDTH, currentMaskingTextureHeight - numUploadedMaskingInfos, All.Rgba, All.Float, ref MaskingTextureBuffer[numUploadedMaskingInfos * MASKING_TEXTURE_WIDTH * 4]);
            // }

            numUploadedMaskingInfos = currentMaskingTextureHeight;

            GL.ActiveTexture(TextureUnit.Texture0);
        }

        public void DrawRange(int startIndex, int endIndex)
        {
            uploadMaskingTexture();

            // ReSharper disable once PossibleLossOfFraction
            GlobalPropertyManager.Set(GlobalProperty.MaskingTextureSize, new Vector2(MASKING_TEXTURE_WIDTH, MaskingTextureHeight));

            // Reset masking info state for the next draw.
            numMaskingInfos = 0;
            numUploadedMaskingInfos = 0;
            localMaskingInfoIndex.Clear();

            Bind(true);

            int countVertices = endIndex - startIndex;
            GL.DrawElements(Type, ToElements(countVertices), DrawElementsType.UnsignedShort, (IntPtr)(ToElementIndex(startIndex) * sizeof(ushort)));

            Unbind();
        }

        public void Update()
        {
            UpdateRange(0, Size);
        }

        public void UpdateRange(int startIndex, int endIndex)
        {
            Bind(false);

            int countVertices = endIndex - startIndex;
            GL.BufferSubData(BufferTarget.ArrayBuffer, (IntPtr)(startIndex * STRIDE), (IntPtr)(countVertices * STRIDE), ref getMemory().Span[startIndex]);

            Unbind();

            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, countVertices);
        }

        private ref Memory<DepthWrappingVertex<T>> getMemory()
        {
            ThreadSafety.EnsureDrawThread();

            if (!InUse)
            {
                memoryOwner = SixLabors.ImageSharp.Configuration.Default.MemoryAllocator.Allocate<DepthWrappingVertex<T>>(Size, AllocationOptions.Clean);
                vertexMemory = memoryOwner.Memory;

                Renderer.RegisterVertexBufferUse(this);
            }

            LastUseResetId = Renderer.ResetId;

            return ref vertexMemory;
        }

        public ulong LastUseResetId { get; private set; }

        public bool InUse => LastUseResetId > 0;

        void IVertexBuffer.Free()
        {
            if (vboId != -1)
            {
                Unbind();

                memoryLease?.Dispose();
                memoryLease = null;

                GL.DeleteBuffer(vboId);
                vboId = -1;
            }

            memoryOwner?.Dispose();
            memoryOwner = null;
            vertexMemory = Memory<DepthWrappingVertex<T>>.Empty;

            LastUseResetId = 0;
        }
    }
}
