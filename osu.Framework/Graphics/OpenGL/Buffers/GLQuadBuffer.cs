// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class GLQuadIndexData
    {
        static GLQuadIndexData()
        {
            GL.GenBuffers(1, out EBO_ID);
        }

        public static readonly int EBO_ID;
        public static int MaxAmountIndices;
    }

    internal class GLQuadBuffer<T> : GLVertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly int amountIndices;

        public GLQuadBuffer(GLRenderer renderer, int amountQuads, BufferUsageHint usage)
            : base(renderer, amountQuads * IRenderer.VERTICES_PER_QUAD, usage)
        {
            amountIndices = amountQuads * IRenderer.INDICES_PER_QUAD;
            Debug.Assert(amountIndices <= IRenderer.MAX_VERTICES);
        }

        protected override void Initialise()
        {
            base.Initialise();

            if (amountIndices > GLQuadIndexData.MaxAmountIndices)
            {
                ushort[] indices = new ushort[amountIndices];

                for (int i = 0, j = 0; j < amountIndices; i += IRenderer.VERTICES_PER_QUAD, j += IRenderer.INDICES_PER_QUAD)
                {
                    indices[j] = (ushort)i;
                    indices[j + 1] = (ushort)(i + 1);
                    indices[j + 2] = (ushort)(i + 3);
                    indices[j + 3] = (ushort)(i + 2);
                    indices[j + 4] = (ushort)(i + 3);
                    indices[j + 5] = (ushort)(i + 1);
                }

                Renderer.BindBuffer(BufferTarget.ElementArrayBuffer, GLQuadIndexData.EBO_ID);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(amountIndices * sizeof(ushort)), indices, BufferUsageHint.StaticDraw);

                GLQuadIndexData.MaxAmountIndices = amountIndices;
            }
        }

        public override void Bind(bool forRendering)
        {
            base.Bind(forRendering);

            if (forRendering)
                Renderer.BindBuffer(BufferTarget.ElementArrayBuffer, GLQuadIndexData.EBO_ID);
        }

        protected override int ToElements(int vertices) => 3 * vertices / 2;

        protected override int ToElementIndex(int vertexIndex) => 3 * vertexIndex / 2;

        protected override PrimitiveType Type => PrimitiveType.Triangles;
    }
}
