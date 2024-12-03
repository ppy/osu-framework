// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridShaderStorageBufferObject<TData> : IShaderStorageBufferObject<TData>, IVeldridUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        public int Size { get; }

        private readonly TData[] data;
        private readonly DeviceBuffer buffer;
        private readonly VeldridRenderer renderer;
        private readonly uint elementSize;

        public VeldridShaderStorageBufferObject(VeldridRenderer renderer, int uboSize, int ssboSize)
        {
            Trace.Assert(ThreadSafety.IsDrawThread);

            this.renderer = renderer;

            elementSize = (uint)Marshal.SizeOf(default(TData));

            if (renderer.UseStructuredBuffers)
            {
                Size = ssboSize;
                buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(elementSize * Size), BufferUsage.StructuredBufferReadOnly | BufferUsage.Dynamic, elementSize, true));
            }
            else
            {
                Size = uboSize;
                buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(elementSize * Size), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            }

            data = new TData[Size];
        }

        private int changeBeginIndex = -1;
        private int changeCount;

        public TData this[int index]
        {
            get => data[index];
            set
            {
                if (data[index].Equals(value))
                    return;

                data[index] = value;

                if (changeBeginIndex == -1)
                {
                    // If this is the first upload, nothing more needs to be done.
                    changeBeginIndex = index;
                }
                else
                {
                    // If this is not the first upload, then we need to check if this index is contiguous with the previous changes.
                    if (index != changeBeginIndex + changeCount)
                    {
                        // This index is not contiguous. Flush the current uploads and start a new change set.
                        flushChanges();
                        changeBeginIndex = index;
                    }
                }

                changeCount++;
            }
        }

        private void flushChanges()
        {
            if (changeBeginIndex == -1)
                return;

            renderer.BufferUpdateCommands.UpdateBuffer(buffer, (uint)(changeBeginIndex * elementSize), data.AsSpan().Slice(changeBeginIndex, changeCount));

            changeBeginIndex = -1;
            changeCount = 0;
        }

        public ResourceSet GetResourceSet(ResourceLayout layout)
        {
            flushChanges();
            return renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));
        }

        public void ResetCounters()
        {
        }

        public void Dispose()
        {
            buffer.Dispose();
        }
    }
}
