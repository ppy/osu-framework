// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGLCore.Buffers
{
    /// <summary>
    /// This type of vertex buffer lets the ith vertex be referenced by the ith index.
    /// </summary>
    internal class GLCoreLinearBuffer<T> : GLCoreVertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly int amountVertices;

        public GLCoreLinearBuffer(GLCoreRenderer renderer, int amountVertices, PrimitiveType type, BufferUsageHint usage)
            : base(renderer, amountVertices, usage)
        {
            this.amountVertices = amountVertices;
            Type = type;

            Debug.Assert(amountVertices <= IRenderer.MAX_VERTICES);
        }

        protected override void Initialise()
        {
            base.Initialise();

            // Must be outside the conditional below as it needs to be added to the VAO
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, GLLinearIndexData.EBO_ID);

            if (amountVertices > GLLinearIndexData.MaxAmountIndices)
            {
                ushort[] indices = new ushort[amountVertices];

                for (int i = 0; i < amountVertices; i++)
                    indices[i] = (ushort)i;

                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(amountVertices * sizeof(ushort)), indices, BufferUsageHint.StaticDraw);

                GLLinearIndexData.MaxAmountIndices = amountVertices;
            }
        }

        protected override PrimitiveType Type { get; }
    }
}
