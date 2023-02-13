// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGLCore.Batches;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Statistics;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGLCore
{
    internal class GLCoreRenderer : GLRenderer
    {
        private int lastBoundVertexArray;

        protected internal override void BeginFrame(Vector2 windowSize)
        {
            lastBoundVertexArray = 0;
            base.BeginFrame(windowSize);
        }

        public bool BindVertexArray(int vaoId)
        {
            if (lastBoundVertexArray == vaoId)
                return false;

            lastBoundVertexArray = vaoId;
            GL.BindVertexArray(vaoId);

            FrameStatistics.Increment(StatisticsCounterType.VBufBinds);
            return true;
        }

        protected override IVertexBatch<TVertex> CreateLinearBatch<TVertex>(int size, int maxBuffers, PrimitiveTopology topology)
            => new GLCoreLinearBatch<TVertex>(this, size, maxBuffers, GLUtils.ToPrimitiveType(topology));

        protected override IVertexBatch<TVertex> CreateQuadBatch<TVertex>(int size, int maxBuffers) => new GLCoreQuadBatch<TVertex>(this, size, maxBuffers);
    }
}
