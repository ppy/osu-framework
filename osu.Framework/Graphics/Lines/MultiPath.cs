// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;
using System.Collections.Generic;
using osu.Framework.Caching;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using System.Linq;

namespace osu.Framework.Graphics.Lines
{
    /// <summary>
    /// Renders many polylines or polygonal paths as one Drawable with more room for optimization than separate lines.
    /// </summary>
    public class MultiPath : Lines
    {
        public class SinglePath
        {
            internal readonly List<Vector2> vertices = new List<Vector2>();

            public IReadOnlyList<Vector2> Vertices
            {
                get => vertices;
            }
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
            SinglePath path = new SinglePath();
            path.vertices.AddRange(vertices);
            paths.Add(path);

            InvalidateSegments();
        }

        public void AddPath(SinglePath path)
        {
            paths.Add(path);

            InvalidateSegments();
        }

        public void StartNewPath()
        {
            AddPath(new SinglePath());
        }

        public void AddVertex(Vector2 pos)
        {
            if (paths.Count == 0)
                throw new InvalidOperationException("Cannot add a vertex if no path has been started yet.");

            paths[paths.Count - 1].vertices.Add(pos);

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