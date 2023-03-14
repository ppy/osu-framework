// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using Veldrid;
using BufferUsage = Veldrid.BufferUsage;
using PrimitiveTopology = Veldrid.PrimitiveTopology;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridQuadBuffer<T> : VeldridVertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly VeldridRenderer renderer;
        private readonly int amountIndices;

        private const int indices_per_quad = IRenderer.VERTICES_PER_QUAD + 2;

        /// <summary>
        /// The maximum number of quads supported by this buffer.
        /// </summary>
        public const int MAX_QUADS = ushort.MaxValue / indices_per_quad;

        internal VeldridQuadBuffer(VeldridRenderer renderer, int amountQuads, BufferUsage usage)
            : base(renderer, amountQuads * IRenderer.VERTICES_PER_QUAD, usage)
        {
            this.renderer = renderer;
            amountIndices = amountQuads * indices_per_quad;
            Debug.Assert(amountIndices <= ushort.MaxValue);
        }

        protected override void Initialise()
        {
            base.Initialise();

            if (amountIndices > renderer.SharedQuadIndex.Capacity)
            {
                renderer.SharedQuadIndex.Capacity = amountIndices;

                ushort[] indices = new ushort[amountIndices];

                for (ushort i = 0, j = 0; j < amountIndices; i += IRenderer.VERTICES_PER_QUAD, j += indices_per_quad)
                {
                    indices[j] = i;
                    indices[j + 1] = (ushort)(i + 1);
                    indices[j + 2] = (ushort)(i + 3);
                    indices[j + 3] = (ushort)(i + 2);
                    indices[j + 4] = (ushort)(i + 3);
                    indices[j + 5] = (ushort)(i + 1);
                }

                renderer.Commands.UpdateBuffer(renderer.SharedQuadIndex.Buffer, 0, indices);
            }
        }

        public override void Bind()
        {
            base.Bind();
            renderer.BindIndexBuffer(renderer.SharedQuadIndex.Buffer, IndexFormat.UInt16);
        }

        protected override int ToElements(int vertices) => 3 * vertices / 2;

        protected override int ToElementIndex(int vertexIndex) => 3 * vertexIndex / 2;

        protected override PrimitiveTopology Type => PrimitiveTopology.TriangleList;
    }
}
