// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct DepthWrappingVertex<TVertex> : IVertex, IEquatable<DepthWrappingVertex<TVertex>>
        where TVertex : struct, IVertex, IEquatable<TVertex>
    {
        public TVertex Vertex;

        [VertexMember(1, VertexAttribPointerType.Float)]
        public float BackbufferDrawDepth;

        public readonly bool Equals(DepthWrappingVertex<TVertex> other)
            => Vertex.Equals(other.Vertex)
               && BackbufferDrawDepth.Equals(other.BackbufferDrawDepth);
    }
}
