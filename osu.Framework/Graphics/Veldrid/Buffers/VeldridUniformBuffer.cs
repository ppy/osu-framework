// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal interface IVeldridUniformBuffer : IDisposable
    {
        ResourceSet GetResourceSet(ResourceLayout layout);

        void Reset();
    }

    internal class VeldridUniformBuffer<TData> : IUniformBuffer<TData>, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly List<VeldridUniformBufferStorage<TData>> storages = new List<VeldridUniformBufferStorage<TData>>();
        private int currentStorageIndex;

        private readonly VeldridRenderer renderer;

        public VeldridUniformBuffer(VeldridRenderer renderer)
        {
            this.renderer = renderer;
            storages.Add(new VeldridUniformBufferStorage<TData>(this.renderer));
        }

        public TData Data
        {
            get => storages[currentStorageIndex].Data;
            set
            {
                renderer.FlushCurrentBatch(FlushBatchSource.SetUniform);

                ++currentStorageIndex;
                while (currentStorageIndex >= storages.Count)
                    storages.Add(new VeldridUniformBufferStorage<TData>(renderer));

                storages[currentStorageIndex].Data = value;

                renderer.RegisterUniformBufferForReset(this);
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout) => storages[currentStorageIndex].GetResourceSet(layout);

        public void Reset()
        {
            currentStorageIndex = 0;
        }

        public void Dispose()
        {
            foreach (var s in storages)
                s.Dispose();
        }
    }
}
