// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Statistics;

namespace osu.Framework.Graphics.Rendering.Deferred.Allocation
{
    /// <summary>
    /// Handles allocation of objects in a deferred rendering context.
    /// </summary>
    internal class ResourceAllocator
    {
        private const int min_buffer_size = 2 * 1024 * 1024; // 2MB per buffer.

        private readonly List<object?> resources = new List<object?>();
        private readonly List<MemoryBuffer> memoryBuffers = new List<MemoryBuffer>();

        /// <summary>
        /// Prepares this <see cref="ResourceAllocator"/> for a new frame.
        /// </summary>
        public void NewFrame()
        {
            ThreadSafety.EnsureDrawThread();

            for (int i = 0; i < memoryBuffers.Count; i++)
                memoryBuffers[i].Dispose();

            resources.Clear();
            memoryBuffers.Clear();
        }

        /// <summary>
        /// References an object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>A reference to the object. May be dereferenced via <see cref="Dereference{T}"/>.</returns>
        public ResourceReference Reference<T>(T obj)
            where T : class?
        {
            ThreadSafety.EnsureDrawThread();

            resources.Add(obj);
            return new ResourceReference(resources.Count - 1);
        }

        /// <summary>
        /// Dereferences an object.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>The object.</returns>
        public T Dereference<T>(ResourceReference reference)
            where T : class?
        {
            ThreadSafety.EnsureDrawThread();

            return (T)resources[reference.Id]!;
        }

        /// <summary>
        /// Allocates a region of memory containing an object.
        /// </summary>
        /// <param name="data">The object.</param>
        /// <typeparam name="T">The object type.</typeparam>
        /// <returns>A reference to the memory region containing the object.</returns>
        public MemoryReference AllocateObject<T>(T data)
            where T : unmanaged
        {
            ThreadSafety.EnsureDrawThread();

            MemoryReference reference = AllocateRegion(Unsafe.SizeOf<T>());
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(GetRegion(reference)), data);
            return reference;
        }

        /// <summary>
        /// Allocates a region of memory containing some data.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <typeparam name="T">The data type.</typeparam>
        /// <returns>A reference to the memory region containing the data.</returns>
        public MemoryReference AllocateRegion<T>(ReadOnlySpan<T> data)
            where T : unmanaged
        {
            ThreadSafety.EnsureDrawThread();

            ReadOnlySpan<byte> byteData = MemoryMarshal.Cast<T, byte>(data);
            MemoryReference region = AllocateRegion(byteData.Length);
            byteData.CopyTo(GetRegion(region));

            return region;
        }

        /// <summary>
        /// Allocates an empty memory region of the specified length.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns>A reference to the memory region.</returns>
        public MemoryReference AllocateRegion(int length)
        {
            ThreadSafety.EnsureDrawThread();

            if (memoryBuffers.Count == 0 || memoryBuffers[^1].Remaining < length)
                memoryBuffers.Add(new MemoryBuffer(memoryBuffers.Count, Math.Max(min_buffer_size, length)));

            return memoryBuffers[^1].Reserve(length);
        }

        /// <summary>
        /// Retrieves a <see cref="Span{T}"/> over a referenced memory region.
        /// </summary>
        /// <param name="reference">The memory reference.</param>
        /// <returns>The <see cref="Span{T}"/>.</returns>
        public Span<byte> GetRegion(MemoryReference reference)
        {
            ThreadSafety.EnsureDrawThread();

            return memoryBuffers[reference.BufferId].GetBuffer(reference);
        }

        private class MemoryBuffer : IDisposable
        {
            private static readonly GlobalStatistic<long> statistic = GlobalStatistics.Get<long>(nameof(ResourceAllocator), "Total Bytes");

            public readonly int Id;
            public int Size => buffer.Length;
            public int Remaining { get; private set; }

            private readonly byte[] buffer;

            public MemoryBuffer(int id, int minSize)
            {
                Id = id;
                buffer = ArrayPool<byte>.Shared.Rent(minSize);
                Remaining = Size;

                statistic.Value += buffer.Length;
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
                return buffer.AsSpan(reference.Offset, reference.Length);
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(buffer);
                statistic.Value -= buffer.Length;
            }
        }
    }
}
