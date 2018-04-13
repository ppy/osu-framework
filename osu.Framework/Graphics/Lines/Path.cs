// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using OpenTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Caching;

namespace osu.Framework.Graphics.Lines
{
    public class Path : Drawable
    {
        private List<Vector2> positions = new List<Vector2>();

        public List<Vector2> Positions
        {
            set
            {
                if (positions == value) return;

                positions = value;
                recomputeBounds();

                segmentsCache.Invalidate();
                Invalidate(Invalidation.DrawNode);
            }
        }

        public override bool ReceiveMouseInputAt(Vector2 screenSpacePos)
        {
            var localPos = ToLocalSpace(screenSpacePos);
            var pathWidthSquared = PathWidth * PathWidth;

            return segments.Any(s => s.DistanceSquaredToPoint(localPos) <= pathWidthSquared);
        }

        public Vector2 PositionInBoundingBox(Vector2 pos) => pos - new Vector2(minX, minY);

        public void ClearVertices()
        {
            if (positions.Count == 0)
                return;

            positions.Clear();
            resetBounds();

            if ((RelativeSizeAxes & Axes.X) == 0) Width = 0;
            if ((RelativeSizeAxes & Axes.Y) == 0) Height = 0;

            segmentsCache.Invalidate();
            Invalidate(Invalidation.DrawNode);
        }

        public void AddVertex(Vector2 pos)
        {
            positions.Add(pos);
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
            if ((RelativeSizeAxes & Axes.X) == 0) Width = b.Width;
            if ((RelativeSizeAxes & Axes.Y) == 0) Height = b.Height;
        }

        private void resetBounds()
        {
            minX = minY = maxX = maxY = 0;
        }

        private void recomputeBounds()
        {
            resetBounds();
            foreach (Vector2 pos in positions)
                expandBounds(pos);
        }

        private float pathWidth = 10f;

        public float PathWidth
        {
            get { return pathWidth; }
            set
            {
                if (pathWidth == value) return;

                pathWidth = value;
                recomputeBounds();

                segmentsCache.Invalidate();
                Invalidate(Invalidation.DrawNode);
            }
        }

        private readonly List<Line> segmentsBacking = new List<Line>();
        private Cached segmentsCache = new Cached();
        private List<Line> segments => segmentsCache.IsValid ? segmentsBacking : generateSegments();

        private List<Line> generateSegments()
        {
            segmentsBacking.Clear();

            if (positions.Count > 1)
            {
                Vector2 offset = new Vector2(minX, minY);
                for (int i = 0; i < positions.Count - 1; ++i)
                    segmentsBacking.Add(new Line(positions[i] - offset, positions[i + 1] - offset));
            }

            segmentsCache.Validate();
            return segmentsBacking;
        }

        private Shader roundedTextureShader;
        private Shader textureShader;

        private readonly PathDrawNodeSharedData pathDrawNodeSharedData = new PathDrawNodeSharedData();

        public bool CanDisposeTexture { get; protected set; }

        #region Disposal

        protected override void Dispose(bool isDisposing)
        {
            if (CanDisposeTexture)
            {
                texture?.Dispose();
                texture = null;
            }

            base.Dispose(isDisposing);
        }

        #endregion

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

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            roundedTextureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_3, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            textureShader = shaders?.Load(VertexShaderDescriptor.TEXTURE_3, FragmentShaderDescriptor.TEXTURE);
        }

        private Texture texture = Texture.WhitePixel;

        public Texture Texture
        {
            get { return texture; }
            set
            {
                if (value == texture)
                    return;

                if (texture != null && CanDisposeTexture)
                    texture.Dispose();

                texture = value;
                Invalidate(Invalidation.DrawNode);
            }
        }
    }
}
