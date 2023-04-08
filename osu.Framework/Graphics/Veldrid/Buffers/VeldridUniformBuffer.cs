// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal interface IVeldridUniformBuffer : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="ResourceSet"/> containing the buffer attached to the given layout.
        /// </summary>
        /// <param name="layout">The <see cref="ResourceLayout"/> which the buffer should be attached to.</param>
        ResourceSet GetResourceSet(ResourceLayout layout);

        /// <summary>
        /// Resets this <see cref="IVeldridUniformBuffer"/>, bringing it to the initial state.
        /// </summary>
        void ResetCounters();
    }

    internal class VeldridUniformBuffer<TData> : IUniformBuffer<TData>, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        private VeldridUniformBufferStorage<TData> currentStorage => storages[currentStorageIndex];

        private readonly List<VeldridUniformBufferStorage<TData>> storages = new List<VeldridUniformBufferStorage<TData>>();
        private int currentStorageIndex;
        private TData? pendingData;

        private readonly VeldridRenderer renderer;

        public VeldridUniformBuffer(VeldridRenderer renderer)
        {
            this.renderer = renderer;
            storages.Add(new VeldridUniformBufferStorage<TData>(this.renderer));
        }

        public TData Data
        {
            get => pendingData ?? currentStorage.Data;
            set
            {
                if (value.Equals(Data))
                    return;

                // Flush the current draw call since the contents of the buffer will change.
                renderer.FlushCurrentBatch(FlushBatchSource.SetUniform);

                pendingData = value;
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            flushPendingData();
            return currentStorage.GetResourceSet(layout);
        }

        private void flushPendingData()
        {
            if (pendingData is not TData pending)
                return;

            pendingData = null;

            // Register this UBO to be reset in the next frame, but only once per frame.
            if (currentStorageIndex == 0)
                renderer.RegisterUniformBufferForReset(this);

            // Advance the storage index to hold the new data.
            if (++currentStorageIndex == storages.Count)
                storages.Add(new VeldridUniformBufferStorage<TData>(renderer));
            else
            {
                // If the new storage previously existed, then it may already contain the data.
                if (pending.Equals(currentStorage.Data))
                    return;
            }

            // Upload the data.
            currentStorage.Data = pending;

            FrameStatistics.Increment(StatisticsCounterType.UniformUpl);
        }

        public void ResetCounters()
        {
            currentStorageIndex = 0;
            pendingData = null;
        }

        ~VeldridUniformBuffer()
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

            foreach (var s in storages)
                s.Dispose();

            IsDisposed = true;
        }
    }
}
