// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.OpenGL.Batches
{
    internal class GLLinearBatch<T> : GLVertexBatch<T>
        where T : unmanaged, IEquatable<T>, IVertex
    {
        private readonly PrimitiveTopology topology;

        public GLLinearBatch(GLRenderer renderer, int size, int maxBuffers, PrimitiveTopology topology)
            : base(renderer, size, maxBuffers)
        {
            this.topology = topology;
        }

        protected override GLVertexBuffer<T> CreateVertexBuffer(GLRenderer renderer) => new GLLinearBuffer<T>(renderer, Size, topology, BufferUsageHint.DynamicDraw);
    }
}
