// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Numerics;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Veldrid.Buffers;
using osu.Framework.Graphics.Veldrid.Pipelines;
using osu.Framework.Graphics.Veldrid.Vertices;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid
{
    internal class VeldridMesh : Mesh
    {
        public VeldridIndexBuffer IndexBuffer;
        public DeviceBuffer VertexBuffer;
        private VeldridRenderer renderer;
        public VeldridMesh(VeldridRenderer renderer, Assimp.Mesh mesh) : base(mesh)
        {
            this.renderer = renderer;

            Size = Vertices.Length;

            VertexBuffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)(Size * VeldridVertexUtils<TexturedMeshVertex>.STRIDE), BufferUsage.VertexBuffer));
            renderer.BufferUpdateCommands.UpdateBuffer(VertexBuffer, 0, Vertices);

            IndexBuffer = renderer.CreateIndexBuffer(mesh.GetUnsignedIndices());


        }

        public int Size { get; }

        public override void Draw()
        {

            renderer.DrawMesh(this);

        }
        public override void Dispose()
        {

            VertexBuffer.Dispose();
            IndexBuffer.Dispose();
            base.Dispose();
        }
    }
}
