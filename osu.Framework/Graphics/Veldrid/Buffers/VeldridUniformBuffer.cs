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
        private bool hasPendingData;
        private TData data;

        private readonly VeldridRenderer renderer;

        public VeldridUniformBuffer(VeldridRenderer renderer)
        {
            this.renderer = renderer;
            storages.Add(new VeldridUniformBufferStorage<TData>(this.renderer));
        }

        public TData Data
        {
            get => data;
            set
            {
                data = value;
                hasPendingData = true;

                renderer.RegisterUniformBufferForReset(this);
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            flushData();
            return currentStorage.GetResourceSet(layout);
        }

        /// <summary>
        /// Writes the data of this UBO to the underlying storage.
        /// </summary>
        private void flushData()
        {
            if (!hasPendingData)
                return;

            hasPendingData = false;

            // If the contents of the UBO changed this frame...
            if (Data.Equals(currentStorage.Data))
                return;

            // Advance to a new target to hold the new data.
            // Note: It is illegal for a previously-drawn UBO to be updated with new data since UBOs are uploaded ahead of time in the frame.
            if (++currentStorageIndex == storages.Count)
                storages.Add(new VeldridUniformBufferStorage<TData>(renderer));
            else
            {
                // If we advanced to an existing target (from a previous frame), and since the target is always advanced before data is set,
                // the new target may already contain the same data from the previous frame.
                if (Data.Equals(currentStorage.Data))
                    return;
            }

            // Upload the data.
            currentStorage.Data = Data;

            FrameStatistics.Increment(StatisticsCounterType.UniformUpl);
        }

        public void ResetCounters()
        {
            currentStorageIndex = 0;
            data = default;
            hasPendingData = false;
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
