// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;
using System.Collections.Generic;
using osu.Framework.Caching;
using osuTK.Graphics;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Lines
{
    public partial class Path : Drawable, IBufferedDrawable
    {
        public IShader RoundedTextureShader { get; private set; }
        public IShader TextureShader { get; private set; }
        private IShader pathShader;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            pathShader = shaders.Load(VertexShaderDescriptor.TEXTURE_3, FragmentShaderDescriptor.TEXTURE);
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

        private float pathRadius = 10f;

        /// <summary>
        /// How wide this path is on each side of the line.
        /// </summary>
        /// <remarks>
        /// The actual width of the path is twice the PathRadius.
        /// </remarks>
        public virtual float PathRadius
        {
            get => pathRadius;
            set
            {
                if (pathRadius == value) return;

                pathRadius = value;
                recomputeBounds();

                segmentsCache.Invalidate();
                Invalidate(Invalidation.DrawNode);
            }
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            var localPos = ToLocalSpace(screenSpacePos);
            var pathRadiusSquared = PathRadius * PathRadius;

            foreach (var t in segments)
                if (t.DistanceSquaredToPoint(localPos) <= pathRadiusSquared)
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

        /// <summary>
        /// Adjust the height and width of this Path depending on the position of the vertices and its radius.
        /// </summary>
        /// <remarks>
        /// Keep in mind that the height will factor in PathRadius twice, once at the top and once on the bottom of the rectangle.
        /// </remarks>
        private void expandBounds(Vector2 pos)
        {
            if (pos.X - PathRadius < minX) minX = pos.X - PathRadius;
            if (pos.Y - PathRadius < minY) minY = pos.Y - PathRadius;
            if (pos.X + PathRadius > maxX) maxX = pos.X + PathRadius;
            if (pos.Y + PathRadius > maxY) maxY = pos.Y + PathRadius;

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

        public DrawColourInfo? FrameBufferDrawColour => base.DrawColourInfo;

        // The path should not receive the true colour to avoid colour doubling when the frame-buffer is rendered to the back-buffer.
        // Removal of blending allows for correct blending between the wedges of the path.
        public override DrawColourInfo DrawColourInfo => new DrawColourInfo(Color4.White, new BlendingInfo(BlendingMode.None));

        public Color4 BackgroundColour => new Color4(0, 0, 0, 0);

        private readonly BufferedDrawNodeSharedData sharedData = new BufferedDrawNodeSharedData();

        protected override DrawNode CreateDrawNode() => new BufferedDrawNode(this, new PathDrawNode(this), sharedData, new[] { RenderbufferInternalFormat.DepthComponent16 });
    }
}
