// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Development;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLUniformBuffer<TData> : IUniformBuffer<TData>, IGLUniformBuffer
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly GLRenderer renderer;
        private readonly int size;

        private TData data;
        private int uboId;

        public GLUniformBuffer(GLRenderer renderer)
        {
            Trace.Assert(ThreadSafety.IsDrawThread);

            this.renderer = renderer;

            size = Marshal.SizeOf(default(TData));

            GL.GenBuffers(1, out uboId);

            // Initialise the buffer with the default data.
            setData(ref data);
        }

        public TData Data
        {
            get => data;
            set
            {
                if (value.Equals(data))
                    return;

                setData(ref value);
            }
        }

        private void setData(ref TData data)
        {
            this.data = data;

            GL.BindBuffer(BufferTarget.UniformBuffer, uboId);
            GL.BufferData(BufferTarget.UniformBuffer, size, ref data, BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.UniformBuffer, 0);

            FrameStatistics.Increment(StatisticsCounterType.UniformUpl);
        }

        #region Disposal

        ~GLUniformBuffer()
        {
            renderer.ScheduleDisposal(b => b.Dispose(false), this);
        }

        public void Dispose()
        {
            renderer.ScheduleDisposal(v => v.Dispose(true), this);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (uboId == -1)
                return;

            GL.DeleteBuffer(uboId);
            uboId = -1;
        }

        #endregion

        public int Id => uboId;

        public void Flush()
        {
        }
    }
}
