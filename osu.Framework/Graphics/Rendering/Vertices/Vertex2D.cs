// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Rendering.Vertices
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex2D : IEquatable<Vertex2D>, IVertex
    {
        [VertexMember(2, VertexAttribPointerType.Float)]
        public Vector2 Position;

        [VertexMember(4, VertexAttribPointerType.Float)]
        public Color4 Colour;

        public readonly bool Equals(Vertex2D other) => Position.Equals(other.Position) && Colour.Equals(other.Colour);
    }
}
