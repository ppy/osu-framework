// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLRawVertexArray : IRawVertexArray
    {
        protected int Handle { get; private set; }
        protected readonly GLRenderer Renderer;
        public GLRawVertexArray(GLRenderer renderer)
        {
            Handle = GL.GenVertexArray();
            Renderer = renderer;
        }

        public void Bind()
        {
            FrameStatistics.Increment(StatisticsCounterType.VArrayBinds);
            GL.BindVertexArray(Handle);
        }

        public void Unbind()
        {
            GL.BindVertexArray(0);
        }

        protected bool IsDisposed => Handle == -1;
        public void Dispose()
        {
            if (IsDisposed)
                return;

            GL.DeleteVertexArray(Handle);
            Handle = -1;
            GC.SuppressFinalize(this);
        }

        ~GLRawVertexArray()
        {
            Renderer.ScheduleDisposal(v => v.Dispose(), this);
        }
    }
}
