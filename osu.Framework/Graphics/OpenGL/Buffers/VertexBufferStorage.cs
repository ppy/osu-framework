// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Platform;
using SixLabors.Memory;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal interface IVertexBufferStorage
    {
        ulong LastUseResetId { get; }

        void Free();
    }

    public class VertexBufferStorage<T> : IVertexBufferStorage, IDisposable
        where T : struct, IEquatable<T>, IVertex
    {
        private readonly int amountVertices;
        private readonly MemoryAllocator allocator;

        public VertexBufferStorage(int amountVertices, MemoryAllocator allocator)
        {
            this.amountVertices = amountVertices;
            this.allocator = allocator;
        }

        public ulong LastUseResetId { get; private set; }

        private IMemoryOwner<T> memoryOwner;
        private Memory<T> memory;
        private NativeMemoryTracker.NativeMemoryLease memoryLease;

        public ref Memory<T> Memory
        {
            get
            {
                if (memoryOwner == null)
                {
                    memoryOwner = allocator.Allocate<T>(amountVertices, AllocationOptions.Clean);
                    memory = memoryOwner.Memory;
                    memoryLease = NativeMemoryTracker.AddMemory(this, amountVertices * VertexUtils<T>.STRIDE);
                }

                LastUseResetId = GLWrapper.ResetId;

                return ref memory;
            }
        }

        void IVertexBufferStorage.Free()
        {
            if (memoryOwner == null)
                return;

            memoryOwner.Dispose();
            memoryOwner = null;
            memory = Memory<T>.Empty;
            memoryLease.Dispose();

            LastUseResetId = 0;
        }

        public void Dispose()
        {
            ((IVertexBufferStorage)this).Free();
        }
    }
}
