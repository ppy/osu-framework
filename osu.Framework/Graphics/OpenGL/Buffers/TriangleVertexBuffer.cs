using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;
using System;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class TriangleIndexData
    {
        static TriangleIndexData()
        {
            GL.GenBuffers(1, out EBO_ID);
        }

        public static readonly int EBO_ID;
        public static int MaxAmountIndices;
    }

    public class TriangleVertexBuffer<T> : VertexBuffer<T>
        where T : struct, IEquatable<T>, IVertex
    {
        private readonly int amountTriangles;

        internal TriangleVertexBuffer(int amountTriangles, BufferUsageHint usage)
            : base(amountTriangles * TextureGLSingle.VERTICES_PER_TRIANGLE, usage)
        {
            this.amountTriangles = amountTriangles;
        }

        protected override void Initialise()
        {
            base.Initialise();

            int amountIndices = amountTriangles * 6;

            if (amountIndices > TriangleIndexData.MaxAmountIndices)
            {
                ushort[] indices = new ushort[amountIndices];

                for (ushort i = 0, j = 0; j < amountIndices; i += TextureGLSingle.VERTICES_PER_TRIANGLE, j += 6)
                {
                    indices[j] = i;
                    indices[j + 1] = (ushort)(i + 1);
                    indices[j + 2] = (ushort)(i + 2);
                    indices[j + 3] = i;
                    indices[j + 4] = (ushort)(i + 2);
                    indices[j + 5] = (ushort)(i + 3);
                }

            }
        }

        public override void Bind(bool forRendering)
        {
            base.Bind(forRendering);

            if (forRendering)
                GLWrapper.BindBuffer(BufferTarget.ElementArrayBuffer, TriangleIndexData.EBO_ID);
        }

        protected override int ToElements(int vertices) => 3 * vertices / 2;

        protected override int ToElementIndex(int vertexIndex) => 3 * vertexIndex / 2;

        protected override PrimitiveType Type => PrimitiveType.Triangles;
    }
}
