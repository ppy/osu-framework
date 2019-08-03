// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osuTK;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Lines
{
    /// <summary>
    /// Renders many polylines or polygonal paths as one Drawable with more room for optimization than separate lines.
    /// </summary>
    public class MultiPath : Lines
    {
        /// <summary>
        /// Immutable path part containing a list of vertices representing a single polyline when drawing.
        /// </summary>
        public class SinglePath
        {
            private readonly List<Vector2> vertices = new List<Vector2>();

            public SinglePath(IEnumerable<Vector2> vertices)
            {
                this.vertices.AddRange(vertices);
            }

            public IReadOnlyList<Vector2> Vertices => vertices;
        }

        private readonly List<SinglePath> paths = new List<SinglePath>();

        public IReadOnlyList<SinglePath> Paths
        {
            get => paths;
            set
            {
                paths.Clear();
                paths.AddRange(value);

                InvalidateSegments();
            }
        }

        public void ClearPaths()
        {
            if (paths.Count == 0)
                return;

            paths.Clear();

            InvalidateSegments();
        }

        public void AddPath(IEnumerable<Vector2> vertices)
        {
            paths.Add(new SinglePath(vertices));

            InvalidateSegments();
        }

        public void AddPath(SinglePath path)
        {
            paths.Add(path);

            InvalidateSegments();
        }

        protected override IEnumerable<Vector2> BoundingVertices => Paths.SelectMany(a => a.Vertices);

        protected override IEnumerable<Line> GenerateSegments()
        {
            foreach (var path in Paths)
            {
                if (path.Vertices.Count > 1)
                {
                    Vector2 offset = SegmentBounds.TopLeft;
                    for (int i = 0; i < path.Vertices.Count - 1; ++i)
                        yield return new Line(path.Vertices[i] - offset, path.Vertices[i + 1] - offset);
                }
            }
        }
    }
}