// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Platform;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    /// <summary>
    /// A vertex buffer implementation specialised for the Metal renderer.
    /// </summary>
    internal class VeldridMetalVertexBuffer<T> : IVeldridVertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly VeldridRenderer renderer;

        private DeviceBuffer? sharedBuffer;
        private unsafe T* sharedBufferMemory;
        private NativeMemoryTracker.NativeMemoryLease? memoryLease;

        public int Size { get; }

        public DeviceBuffer Buffer => sharedBuffer ?? throw new InvalidOperationException("The buffer is not initialised yet.");

        public VeldridMetalVertexBuffer(VeldridRenderer renderer, int amountVertices)
        {
            Debug.Assert(renderer.SurfaceType == GraphicsSurfaceType.Metal);

            this.renderer = renderer;
            Size = amountVertices;
        }

        public unsafe bool SetVertex(int vertexIndex, T vertex)
        {
            if (sharedBuffer == null)
                initialiseBuffer();

            sharedBufferMemory[vertexIndex] = vertex;

            // we use a buffer with memory storage shared between the CPU and the GPU, therefore we don't need to do explicit synchronisation/updates (which is queued when returning true here).
            return false;
        }

        private unsafe void initialiseBuffer()
        {
            sharedBuffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(IVeldridVertexBuffer<T>.STRIDE * Size), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
            sharedBufferMemory = (T*)renderer.Device.Map(sharedBuffer, MapMode.Write).Data;
            memoryLease = NativeMemoryTracker.AddMemory(this, sharedBuffer.SizeInBytes);

            LastUseFrameIndex = renderer.FrameIndex;
        }

        void IVeldridVertexBuffer<T>.UpdateRange(int from, int to) => throw new NotSupportedException("This implementation shares buffer storage with the GPU, no explicit synchronisation is required prior to drawing. See https://developer.apple.com/documentation/metal/mtlstoragemode/mtlstoragemodeshared?language=objc for more information.");

        ~VeldridMetalVertexBuffer()
        {
            renderer.ScheduleDisposal(v => v.Dispose(false), this);
        }

        public void Dispose()
        {
            renderer.ScheduleDisposal(v => v.Dispose(true), this);
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

        public ulong LastUseFrameIndex { get; private set; }

        public bool InUse => LastUseFrameIndex > 0;

        public void Free()
        {
            memoryLease?.Dispose();
            memoryLease = null;

            sharedBuffer?.Dispose();
            sharedBuffer = null;

            LastUseFrameIndex = 0;
        }
    }
}
