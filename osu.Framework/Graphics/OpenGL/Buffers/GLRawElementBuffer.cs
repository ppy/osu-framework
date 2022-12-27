// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal class GLRawElementBuffer<TIndex> : GLRawBuffer<TIndex>, IRawElementBuffer<TIndex> where TIndex : unmanaged, IConvertible
    {
        public GLRawElementBuffer(GLRenderer renderer) : base(renderer, BufferTarget.ElementArrayBuffer)
        {
        }

        public void Draw(PrimitiveTopology topology, int count, int offset = 0)
        {
            var drawOffset = (IntPtr)(offset * STRIDE);
            GL.DrawElements(GLUtils.ToPrimitiveType(topology), count, GLUtils.ToDrawElementsType(IRawElementBuffer<TIndex>.INDEX_TYPE), drawOffset);
        }
    }
}
