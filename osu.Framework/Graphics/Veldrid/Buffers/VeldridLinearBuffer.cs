// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Rendering.Vertices;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    /// <summary>
    /// This type of vertex buffer lets the ith vertex be referenced by the ith index.
    /// </summary>
    internal class VeldridLinearBuffer<T> : VeldridVertexBuffer<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly VeldridRenderer renderer;
        private readonly int amountVertices;

        internal VeldridLinearBuffer(VeldridRenderer renderer, int amountVertices, PrimitiveTopology type)
            : base(renderer, amountVertices)
        {
            this.renderer = renderer;
            this.amountVertices = amountVertices;
            Type = type;
        }

        protected override void Initialise()
        {
            base.Initialise();

            if (amountVertices > renderer.SharedLinearIndex.Capacity)
            {
                renderer.SharedLinearIndex.Capacity = amountVertices;

                ushort[] indices = new ushort[amountVertices];

                for (ushort i = 0; i < amountVertices; i++)
                    indices[i] = i;

                var staging = renderer.GetFreeStagingBuffer(renderer.SharedLinearIndex.Buffer.SizeInBytes);
                renderer.Device.UpdateBuffer(staging, 0, indices);
                renderer.BufferUpdateCommands.CopyBuffer(staging, 0, renderer.SharedLinearIndex.Buffer, 0, staging.SizeInBytes);
            }
        }

        public override void Bind()
        {
            base.Bind();
            renderer.BindIndexBuffer(renderer.SharedLinearIndex.Buffer, IndexFormat.UInt16);
        }

        protected override PrimitiveTopology Type { get; }
    }
}
