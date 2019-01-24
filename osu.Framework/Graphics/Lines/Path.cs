// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;

namespace osu.Framework.Graphics.Lines
{
    public class Path : Drawable
    {
        private Shader roundedTextureShader;
        private Shader textureShader;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            roundedTextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_3, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            textureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_3, FragmentShaderDescriptor.TEXTURE);
        }

        private readonly List<Vector2> vertices = new List<Vector2>();

        public IReadOnlyList<Vector2> Vertices
        {
            get => vertices;
            set
            {
                vertices.Clear();
                vertices.AddRange(value);

                recomputeBounds();

                segmentsCache.Invalidate();
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float pathWidth = 10f;

        public virtual float PathWidth
        {
            get => pathWidth;
            set
            {
                if (pathWidth == value) return;

                pathWidth = value;
                recomputeBounds();

                segmentsCache.Invalidate();
                Invalidate(Invalidation.DrawNode);
            }
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            var localPos = ToLocalSpace(screenSpacePos);
            var pathWidthSquared = PathWidth * PathWidth;

            foreach (var t in segments)
                if (t.DistanceSquaredToPoint(localPos) <= pathWidthSquared)
                    return true;
            return false;
        }

        public Vector2 PositionInBoundingBox(Vector2 pos) => pos - new Vector2(minX, minY);

        public void ClearVertices()
        {
            if (vertices.Count == 0)
                return;

            vertices.Clear();
            resetBounds();

            if (!RelativeSizeAxes.HasFlag(Axes.X)) Width = 0;
            if (!RelativeSizeAxes.HasFlag(Axes.Y)) Height = 0;

            segmentsCache.Invalidate();
            Invalidate(Invalidation.DrawNode);
        }

        public void AddVertex(Vector2 pos)
        {
            vertices.Add(pos);
            expandBounds(pos);

            segmentsCache.Invalidate();
            Invalidate(Invalidation.DrawNode);
        }

        private float minX;
        private float minY;
        private float maxX;
        private float maxY;

        private RectangleF bounds => new RectangleF(minX, minY, maxX - minX, maxY - minY);

        private void expandBounds(Vector2 pos)
        {
            if (pos.X - PathWidth < minX) minX = pos.X - PathWidth;
            if (pos.Y - PathWidth < minY) minY = pos.Y - PathWidth;
            if (pos.X + PathWidth > maxX) maxX = pos.X + PathWidth;
            if (pos.Y + PathWidth > maxY) maxY = pos.Y + PathWidth;

            RectangleF b = bounds;
            if (!RelativeSizeAxes.HasFlag(Axes.X)) Width = b.Width;
            if (!RelativeSizeAxes.HasFlag(Axes.Y)) Height = b.Height;
        }

        private void resetBounds()
        {
            minX = minY = maxX = maxY = 0;
        }

        private void recomputeBounds()
        {
            resetBounds();
            foreach (Vector2 pos in vertices)
                expandBounds(pos);
        }

        private readonly List<Line> segmentsBacking = new List<Line>();
        private Cached segmentsCache = new Cached();
        private List<Line> segments => segmentsCache.IsValid ? segmentsBacking : generateSegments();

        private List<Line> generateSegments()
        {
            segmentsBacking.Clear();

            if (vertices.Count > 1)
            {
                Vector2 offset = new Vector2(minX, minY);
                for (int i = 0; i < vertices.Count - 1; ++i)
                    segmentsBacking.Add(new Line(vertices[i] - offset, vertices[i + 1] - offset));
            }

            segmentsCache.Validate();
            return segmentsBacking;
        }

        private Texture texture = Texture.WhitePixel;

        protected Texture Texture
        {
            get => texture;
            set
            {
                if (texture == value)
                    return;

                texture?.Dispose();
                texture = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        private readonly PathDrawNodeSharedData pathDrawNodeSharedData = new PathDrawNodeSharedData();

        protected override DrawNode CreateDrawNode() => new PathDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            PathDrawNode n = (PathDrawNode)node;

            n.Texture = Texture;
            n.TextureShader = textureShader;
            n.RoundedTextureShader = roundedTextureShader;
            n.Width = PathWidth;
            n.DrawSize = DrawSize;

            n.Shared = pathDrawNodeSharedData;

            n.Segments = segments.ToList();

            base.ApplyDrawNode(node);
        }
    }
}
