// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Development;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    internal class ResourceAllocator
    {
        private const int min_buffer_size = 1024 * 1024; // 1MB

        private readonly List<object> resources = new List<object>();
        private readonly List<MemoryBuffer> memoryBuffers = new List<MemoryBuffer>();

        public void Reset()
        {
            ThreadSafety.EnsureDrawThread();

            for (int i = 0; i < memoryBuffers.Count; i++)
                memoryBuffers[i].Dispose();

            resources.Clear();
            memoryBuffers.Clear();

            // Special value used by NullReference().
            resources.Add(null!);
        }

        public ResourceReference Reference<T>(T obj)
            where T : class
        {
            ThreadSafety.EnsureDrawThread();

            resources.Add(obj);
            return new ResourceReference(resources.Count - 1);
        }

        public object Dereference(ResourceReference reference)
        {
            ThreadSafety.EnsureDrawThread();

            return resources[reference.Id];
        }

        public ResourceReference NullReference() => new ResourceReference(0);

        public MemoryReference AllocateObject<T>(T data)
            where T : unmanaged
        {
            ThreadSafety.EnsureDrawThread();

            MemoryReference reference = AllocateRegion(Unsafe.SizeOf<T>());
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(GetRegion(reference)), data);
            return reference;
        }

        public MemoryReference AllocateRegion<T>(ReadOnlySpan<T> data)
            where T : unmanaged
        {
            ThreadSafety.EnsureDrawThread();

            ReadOnlySpan<byte> byteData = MemoryMarshal.Cast<T, byte>(data);
            MemoryReference region = AllocateRegion(byteData.Length);
            byteData.CopyTo(GetRegion(region));

            return region;
        }

        public MemoryReference AllocateRegion(int length)
        {
            ThreadSafety.EnsureDrawThread();

            if (memoryBuffers.Count == 0 || memoryBuffers[^1].Remaining < length)
                memoryBuffers.Add(new MemoryBuffer(memoryBuffers.Count, Math.Max(min_buffer_size * (1 << memoryBuffers.Count), length)));

            return memoryBuffers[^1].Reserve(length);
        }

        public Span<byte> GetRegion(MemoryReference reference)
        {
            ThreadSafety.EnsureDrawThread();

            return memoryBuffers[reference.BufferId].GetBuffer(reference);
        }

        private class MemoryBuffer : IDisposable
        {
            public readonly int Id;
            public int Size => buffer.Length;
            public int Remaining { get; private set; }

            private readonly byte[] buffer;

            public MemoryBuffer(int id, int minSize)
            {
                Id = id;
                buffer = ArrayPool<byte>.Shared.Rent(minSize);
                Remaining = Size;
            }

            public MemoryReference Reserve(int length)
            {
                Debug.Assert(length <= Remaining);

                int start = Size - Remaining;
                Remaining -= length;
                return new MemoryReference(Id, start, length);
            }

            public Span<byte> GetBuffer(MemoryReference reference)
            {
                Debug.Assert(reference.BufferId == Id);
                return buffer.AsSpan().Slice(reference.Index, reference.Length);
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
