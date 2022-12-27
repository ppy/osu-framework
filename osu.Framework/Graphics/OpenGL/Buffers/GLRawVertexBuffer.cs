// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Statistics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLRawVertexBuffer<TVertex> : GLRawBuffer<TVertex>, IRawVertexBuffer<TVertex> where TVertex : unmanaged, IVertex
    {
        public GLRawVertexBuffer(GLRenderer renderer) : base(renderer, BufferTarget.ArrayBuffer)
        {
        }

        public void SetLayout()
        {
            GLVertexUtils<TVertex>.SetLayout();
        }

        public void SetLayout(ReadOnlySpan<int> layoutPositions)
        {
            GLVertexUtils<TVertex>.SetLayout(layoutPositions);
        }

        public void Draw(PrimitiveTopology topology, int count, int offset = 0)
        {
            FrameStatistics.Add(StatisticsCounterType.VerticesDraw, count);
            FrameStatistics.Increment(StatisticsCounterType.DrawCalls);
            GL.DrawArrays(GLUtils.ToPrimitiveType(topology), offset, count);
        }
    }
}
