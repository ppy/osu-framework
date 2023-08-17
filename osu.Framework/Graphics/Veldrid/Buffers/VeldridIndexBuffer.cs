// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    /// <summary>
    /// Represents a <see cref="ushort"/>-typed buffer used to control vertex indices during draw.
    /// </summary>
    internal class VeldridIndexBuffer : IDisposable
    {
        public const IndexFormat FORMAT = IndexFormat.UInt16;

        public DeviceBuffer Buffer { get; }
        public VeldridIndexLayout Layout { get; }

        /// <summary>
        /// The number of vertices this buffer can map.
        /// </summary>
        public int VertexCapacity { get; }

        public VeldridIndexBuffer(VeldridRenderer renderer, VeldridIndexLayout layout, int verticesCount)
        {
            Layout = layout;

            ushort[] indices = new ushort[TranslateToIndex(verticesCount)];

            switch (layout)
            {
                case VeldridIndexLayout.Linear:
                    for (ushort i = 0; i < indices.Length; i++)
                        indices[i] = i;

                    break;

                case VeldridIndexLayout.Quad:
                    for (ushort i = 0, j = 0; j < indices.Length; i += IRenderer.VERTICES_PER_QUAD, j += IRenderer.INDICES_PER_QUAD)
                    {
                        indices[j] = i;
                        indices[j + 1] = (ushort)(i + 1);
                        indices[j + 2] = (ushort)(i + 3);
                        indices[j + 3] = (ushort)(i + 2);
                        indices[j + 4] = (ushort)(i + 3);
                        indices[j + 5] = (ushort)(i + 1);
                    }

                    break;
            }

            Buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)indices.Length * sizeof(ushort), BufferUsage.IndexBuffer));
            VertexCapacity = verticesCount;

            renderer.BufferUpdateCommands.UpdateBuffer(Buffer, 0, indices);
        }

        public int TranslateToIndex(int vertexIndex)
        {
            switch (Layout)
            {
                default:
                case VeldridIndexLayout.Linear:
                    return vertexIndex;

                case VeldridIndexLayout.Quad:
                    return 3 * vertexIndex / 2;
            }
        }

        public void Dispose()
        {
            Buffer.Dispose();
        }
    }
}
