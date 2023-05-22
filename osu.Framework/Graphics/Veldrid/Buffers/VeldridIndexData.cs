// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    // todo: this feels too janky
    internal class VeldridIndexData
    {
        private readonly VeldridRenderer renderer;

        private int capacity;

        public int Capacity
        {
            get => capacity;
            set
            {
                if (value == capacity)
                    return;

                capacity = value;

                buffer?.Dispose();
                buffer = null;
            }
        }

        public VeldridIndexData(VeldridRenderer renderer)
        {
            this.renderer = renderer;
        }

        private DeviceBuffer? buffer;

        public DeviceBuffer Buffer => buffer ??= renderer.Factory.CreateBuffer(new BufferDescription((uint)(Capacity * sizeof(ushort)), BufferUsage.IndexBuffer));
    }
}
