// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osuTK;

namespace osu.Framework.Graphics.Primitives
{
    public class SimpleConvexPolygon : IConvexPolygon
    {
        private readonly Vector2[] vertices;

        public SimpleConvexPolygon(Vector2[] vertices)
        {
            this.vertices = vertices;
        }

        public ReadOnlySpan<Vector2> GetAxisVertices() => vertices;

        public ReadOnlySpan<Vector2> GetVertices() => vertices;

        public int MaxClipVertices => vertices.Length * 2;

        public override string ToString() => $"{{ {string.Join(", ", vertices.Select(v => $"({v.X}, {v.Y})"))} }}";
    }
}
