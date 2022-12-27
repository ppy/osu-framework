// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal abstract class GLRawBuffer<TData> : IRawBuffer<TData> where TData : unmanaged
    {
        public static readonly int STRIDE;
        static GLRawBuffer()
        {
            STRIDE = Marshal.SizeOf<TData>();
        }

        protected int Handle { get; private set; }
        protected readonly GLRenderer Renderer;
        protected readonly BufferTarget Target;
        public GLRawBuffer(GLRenderer renderer, BufferTarget target)
        {
            Handle = GL.GenBuffer();
            Renderer = renderer;
            Target = target;
        }

        public bool Bind()
        {
            return Renderer.BindBuffer(Target, Handle);
        }

        public void Unbind()
        {
            Renderer.BindBuffer(Target, 0);
        }

        public void SetCapacity(int size, Rendering.BufferUsageHint usageHint)
        {
            GL.BufferData(Target, size * STRIDE, IntPtr.Zero, GLUtils.ToBufferUsageHint(usageHint));
        }

        public void BufferData(ReadOnlySpan<TData> data, Rendering.BufferUsageHint usageHint)
        {
            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, data.Length);
            ref TData dataPointer = ref MemoryMarshal.GetReference(data);
            GL.BufferData(Target, data.Length * STRIDE, ref dataPointer, GLUtils.ToBufferUsageHint(usageHint));
        }

        public void UpdateRange(ReadOnlySpan<TData> data, int offset = 0)
        {
            FrameStatistics.Add(StatisticsCounterType.VerticesUpl, data.Length);
            var dataOffset = (IntPtr)(offset * STRIDE);
            ref TData dataPointer = ref MemoryMarshal.GetReference(data);
            GL.BufferSubData(Target, dataOffset, data.Length * STRIDE, ref dataPointer);
        }

        protected bool IsDisposed => Handle == -1;
        public void Dispose()
        {
            if (IsDisposed)
                return;

            GL.DeleteBuffer(Handle);
            Handle = -1;
            GC.SuppressFinalize(this);
        }

        ~GLRawBuffer()
        {
            Renderer.ScheduleDisposal(v => v.Dispose(), this);
        }
    }
}
