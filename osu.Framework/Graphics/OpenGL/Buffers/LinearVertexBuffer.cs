// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using OpenTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Buffers
{
    internal static class LinearIndexData
    {
        static LinearIndexData()
        {
            GL.GenBuffers(1, out EBO_ID);
        }

        public static readonly int EBO_ID;
        public static int MaxAmountIndices;
    }

    /// <summary>
    /// This type of vertex buffer lets the ith vertex be referenced by the ith index.
    /// </summary>
    public class LinearVertexBuffer<T> : VertexBuffer<T>
        where T : struct, IEquatable<T>, IVertex
    {
        public LinearVertexBuffer(int amountVertices, PrimitiveType type, BufferUsageHint usage)
            : base(amountVertices, usage)
        {
            Type = type;

            if (amountVertices > LinearIndexData.MaxAmountIndices)
            {
                ushort[] indices = new ushort[amountVertices];

                for (ushort i = 0; i < amountVertices; i++)
                    indices[i] = i;

                GLWrapper.BindBuffer(BufferTarget.ElementArrayBuffer, LinearIndexData.EBO_ID);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(amountVertices * sizeof(ushort)), indices, BufferUsageHint.StaticDraw);

                LinearIndexData.MaxAmountIndices = amountVertices;
            }
        }

        public override void Bind(bool forRendering)
        {
            base.Bind(forRendering);

            if (forRendering)
                GLWrapper.BindBuffer(BufferTarget.ElementArrayBuffer, LinearIndexData.EBO_ID);
        }

        protected override PrimitiveType Type { get; }
    }
}
