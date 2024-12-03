// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLShaderStorageBufferObject<TData> : IShaderStorageBufferObject<TData>, IGLShaderStorageBufferObject
        where TData : unmanaged, IEquatable<TData>
    {
        public int Size { get; }

        public int Id { get; }

        private readonly TData[] data;
        private readonly int elementSize;

        public GLShaderStorageBufferObject(GLRenderer renderer, int uboSize, int ssboSize)
        {
            Trace.Assert(ThreadSafety.IsDrawThread);

            Id = GL.GenBuffer();
            Size = renderer.UseStructuredBuffers ? ssboSize : uboSize;
            data = new TData[Size];
            elementSize = Marshal.SizeOf(default(TData));

            GL.BindBuffer(BufferTarget.UniformBuffer, Id);
            GL.BufferData(BufferTarget.UniformBuffer, elementSize * Size, ref data[0], BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);
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
                        Flush();
                        changeBeginIndex = index;
                    }
                }

                changeCount++;
            }
        }

        public void Flush()
        {
            if (changeBeginIndex == -1)
                return;

            GL.BindBuffer(BufferTarget.UniformBuffer, Id);
            GL.BufferSubData(BufferTarget.UniformBuffer, changeBeginIndex * elementSize, elementSize * changeCount, ref data[changeBeginIndex]);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);

            changeBeginIndex = -1;
            changeCount = 0;
        }

        public void Dispose()
        {
            GL.DeleteBuffer(Id);
        }
    }
}
