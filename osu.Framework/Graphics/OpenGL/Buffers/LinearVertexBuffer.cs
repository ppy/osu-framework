// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics.ES30;

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
        private readonly int capacity;

        internal LinearVertexBuffer(int capacity, PrimitiveType type, BufferUsageHint usage)
            : base(capacity, usage)
        {
            this.capacity = capacity;
            Type = type;
        }

        protected override void Initialise()
        {
            base.Initialise();

            if (capacity > LinearIndexData.MaxAmountIndices)
            {
                ushort[] indices = new ushort[capacity];

                for (ushort i = 0; i < capacity; i++)
                    indices[i] = i;

                GLWrapper.BindBuffer(BufferTarget.ElementArrayBuffer, LinearIndexData.EBO_ID);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(capacity * sizeof(ushort)), indices, BufferUsageHint.StaticDraw);

                LinearIndexData.MaxAmountIndices = capacity;
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
