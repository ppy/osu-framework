// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Rendering.Vertices;
using Veldrid;
using BufferUsage = Veldrid.BufferUsage;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal static class VeldridLinearIndexData
    {
        private static int maxAmountIndices;

        public static int MaxAmountIndices
        {
            get => maxAmountIndices;
            set
            {
                if (value == maxAmountIndices)
                    return;

                maxAmountIndices = value;

                indexBuffer?.Dispose();
                indexBuffer = null;
            }
        }

        private static DeviceBuffer indexBuffer;

        // todo: uhhhhhhhhhh....
        public static DeviceBuffer IndexBuffer => indexBuffer ??= Vd.Factory.CreateBuffer(new BufferDescription((uint)(MaxAmountIndices * sizeof(ushort)), BufferUsage.IndexBuffer));
    }

    /// <summary>
    /// This type of vertex buffer lets the ith vertex be referenced by the ith index.
    /// </summary>
    internal class VeldridLinearBuffer<T> : VeldridVertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly VeldridRenderer renderer;
        private readonly int amountVertices;

        internal VeldridLinearBuffer(VeldridRenderer renderer, int amountVertices, PrimitiveTopology type, BufferUsage usage)
            : base(renderer, amountVertices, usage)
        {
            this.renderer = renderer;
            this.amountVertices = amountVertices;
            Type = type;
        }

        protected override void Initialise()
        {
            base.Initialise();

            if (amountVertices > VeldridLinearIndexData.MaxAmountIndices)
            {
                VeldridLinearIndexData.MaxAmountIndices = amountVertices;

                ushort[] indices = new ushort[amountVertices];

                for (ushort i = 0; i < amountVertices; i++)
                    indices[i] = i;

                renderer.Commands.UpdateBuffer(VeldridLinearIndexData.IndexBuffer, 0, indices);
            }
        }

        public override void Bind()
        {
            base.Bind();
            renderer.BindIndexBuffer(VeldridLinearIndexData.IndexBuffer, IndexFormat.UInt16);
        }

        protected override PrimitiveTopology Type { get; }
    }
}
