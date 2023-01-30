// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal interface IVeldridUniformBuffer
    {
        DeviceBuffer Buffer { get; }
    }

    internal class VeldridUniformBuffer<TData> : IUniformBuffer<TData>, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        public DeviceBuffer Buffer { get; }

        private readonly VeldridRenderer renderer;
        private TData data;

        public VeldridUniformBuffer(VeldridRenderer renderer)
        {
            this.renderer = renderer;
            Buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(default(TData)), BufferUsage.UniformBuffer));
        }

        public TData Data
        {
            get => data;
            set
            {
                if (value.Equals(data))
                    return;

                data = value;

                renderer.Commands.UpdateBuffer(Buffer, 0, ref data);
            }
        }

        public void Dispose()
        {
            Buffer.Dispose();
        }
    }
}
