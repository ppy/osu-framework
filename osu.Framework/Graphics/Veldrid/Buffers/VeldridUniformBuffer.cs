// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal interface IVeldridUniformBuffer : IDisposable
    {
        ResourceSet GetResourceSet(ResourceLayout layout);
    }

    internal class VeldridUniformBuffer<TData> : IUniformBuffer<TData>, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly VeldridRenderer renderer;
        private readonly DeviceBuffer buffer;

        private ResourceSet? set;
        private TData data;

        public VeldridUniformBuffer(VeldridRenderer renderer)
        {
            this.renderer = renderer;
            buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(default(TData)), BufferUsage.UniformBuffer));
        }

        public TData Data
        {
            get => data;
            set
            {
                if (value.Equals(data))
                    return;

                data = value;

                renderer.Commands.UpdateBuffer(buffer, 0, ref data);
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout) => set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));

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

            buffer.Dispose();
            set?.Dispose();

            IsDisposed = true;
        }
    }
}
