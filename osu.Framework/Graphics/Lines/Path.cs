// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osuTK;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Lines
{
    /// <summary>
    /// Renders one continuous polyline or polygonal path.
    /// </summary>
    public class Path : Lines
    {
        private readonly List<Vector2> vertices = new List<Vector2>();

        public IReadOnlyList<Vector2> Vertices
        {
            get => vertices;
            set
            {
                vertices.Clear();
                vertices.AddRange(value);

                InvalidateSegments();
            }
        }

        public void ClearVertices()
        {
            if (vertices.Count == 0)
                return;

            vertices.Clear();

            InvalidateSegments();
        }

        public void AddVertex(Vector2 pos)
        {
            vertices.Add(pos);

            InvalidateSegments();
        }

        protected override IEnumerable<Vector2> BoundingVertices => vertices;

        protected override IEnumerable<Line> GenerateSegments()
        {
            if (vertices.Count > 1)
            {
                Vector2 offset = SegmentBounds.TopLeft;
                for (int i = 0; i < vertices.Count - 1; ++i)
                    yield return new Line(vertices[i] - offset, vertices[i + 1] - offset);
            }
        }
    }
}
