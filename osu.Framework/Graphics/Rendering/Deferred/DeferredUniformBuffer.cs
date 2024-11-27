// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering.Deferred.Allocation;
using osu.Framework.Graphics.Rendering.Deferred.Events;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Rendering.Deferred
{
    internal class DeferredUniformBuffer<TData> : IUniformBuffer<TData>, IDeferredUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        private static readonly List<(ChunkReference, ResourceLayout)> dead_chunks = new List<(ChunkReference, ResourceLayout)>();

        private readonly DeferredRenderer renderer;
        private readonly Dictionary<(ChunkReference, ResourceLayout), ResourceSet> bufferChunks = new Dictionary<(ChunkReference, ResourceLayout), ResourceSet>();

        private TData data;
        private ChunkReference currentChunk;

        public DeferredUniformBuffer(DeferredRenderer renderer)
        {
            Trace.Assert(ThreadSafety.IsDrawThread);

            this.renderer = renderer;
        }

        TData IUniformBuffer<TData>.Data
        {
            get => data;
            set
            {
                data = value;

                renderer.Context.EnqueueEvent(SetUniformBufferDataEvent.Create(renderer, this, value));
                renderer.RegisterUniformBufferForReset(this);

                FrameStatistics.Increment(StatisticsCounterType.UniformUpl);
            }
        }

        public UniformBufferReference Write(in MemoryReference memory)
            => renderer.Context.UniformBufferManager.Write(memory);

        public void Activate(UniformBufferChunk chunk)
            => currentChunk = new ChunkReference(renderer, chunk);

        ResourceSet IVeldridUniformBuffer.GetResourceSet(ResourceLayout layout)
        {
            if (bufferChunks.TryGetValue((currentChunk, layout), out ResourceSet? existing))
                return existing;

            return bufferChunks[(currentChunk, layout)] = renderer.Factory.CreateResourceSet(
                new ResourceSetDescription(
                    layout,
                    new DeviceBufferRange(
                        currentChunk.Buffer,
                        currentChunk.Offset,
                        currentChunk.Size)));
        }

        void IVeldridUniformBuffer.ResetCounters()
        {
            foreach (((ChunkReference chunk, ResourceLayout layout) key, ResourceSet set) in bufferChunks)
            {
                if (key.chunk.Buffer.IsDisposed)
                {
                    dead_chunks.Add(key);
                    set.Dispose();
                }
            }

            foreach (var key in dead_chunks)
                bufferChunks.Remove(key);

            data = default;
            currentChunk = default;
            dead_chunks.Clear();
        }

        ~DeferredUniformBuffer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            renderer.ScheduleDisposal(static b =>
            {
                foreach ((_, ResourceSet set) in b.bufferChunks)
                    set.Dispose();
            }, this);
        }

        private readonly record struct ChunkReference
        {
            public readonly DeviceBuffer Buffer;
            public readonly uint Size;
            public readonly uint Offset;

            public ChunkReference(DeferredRenderer renderer, UniformBufferChunk chunk)
            {
                Buffer = renderer.Context.Dereference<DeviceBuffer>(chunk.Buffer);
                Size = (uint)chunk.Size;
                Offset = (uint)chunk.Offset;
            }
        }
    }
}
