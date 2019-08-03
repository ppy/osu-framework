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
using System;

namespace osu.Framework.Graphics.Lines
{
    /// <summary>
    /// Base class used for drawing 2D lines with line caps. For an implementation rendering one connected polyline, use <see cref="Path"/>.
    /// </summary>
    public abstract partial class Lines : Drawable, IBufferedDrawable
    {
        public IShader RoundedTextureShader { get; private set; }
        public IShader TextureShader { get; private set; }
        protected IShader pathShader;

        public Lines()
        {
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            pathShader = shaders.Load(VertexShaderDescriptor.TEXTURE_3, FragmentShaderDescriptor.TEXTURE);
        }

        public override Axes RelativeSizeAxes
        {
            get => base.RelativeSizeAxes;
            set
            {
                if ((AutoSizeAxes & value) != 0)
                    throw new InvalidOperationException("No axis can be relatively sized and automatically sized at the same time.");

                base.RelativeSizeAxes = value;
            }
        }

        private Axes autoSizeAxes;

        /// <summary>
        /// Controls which <see cref="Axes"/> are automatically sized w.r.t. the bounds of the vertices.
        /// It is not allowed to manually set <see cref="Size"/> (or <see cref="Width"/> / <see cref="Height"/>)
        /// on any <see cref="Axes"/> which are automatically sized.
        /// </summary>
        public virtual Axes AutoSizeAxes
        {
            get => autoSizeAxes;
            set
            {
                if (value == autoSizeAxes)
                    return;

                if ((RelativeSizeAxes & value) != 0)
                    throw new InvalidOperationException("No axis can be relatively sized and automatically sized at the same time.");

                autoSizeAxes = value;
                OnSizingChanged();
            }
        }

        public override float Width
        {
            get
            {
                if (AutoSizeAxes.HasFlag(Axes.X))
                    return base.Width = SegmentBounds.Width;

                return base.Width;
            }
            set
            {
                if ((AutoSizeAxes & Axes.X) != 0)
                    throw new InvalidOperationException($"The width of a {nameof(Path)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Width = value;
            }
        }

        public override float Height
        {
            get
            {
                if (AutoSizeAxes.HasFlag(Axes.Y))
                    return base.Height = SegmentBounds.Height;

                return base.Height;
            }
            set
            {
                if ((AutoSizeAxes & Axes.Y) != 0)
                    throw new InvalidOperationException($"The height of a {nameof(Path)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Height = value;
            }
        }

        public override Vector2 Size
        {
            get
            {
                if (AutoSizeAxes != Axes.None)
                    return base.Size = SegmentBounds.Size;

                return base.Size;
            }
            set
            {
                if ((AutoSizeAxes & Axes.Both) != 0)
                    throw new InvalidOperationException($"The Size of a {nameof(Path)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Size = value;
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

                sgmentBoundsCache.Invalidate();
                segmentsCache.Invalidate();

                Invalidate(Invalidation.DrawSize);
            }
        }

        private Cached<RectangleF> sgmentBoundsCache;

        public RectangleF SegmentBounds
        {
            get
            {
                if (sgmentBoundsCache.IsValid)
                    return sgmentBoundsCache.Value;

                float minX = 0;
                float minY = 0;
                float maxX = 0;
                float maxY = 0;

                foreach (var p in BoundingVertices)
                {
                    minX = Math.Min(minX, p.X - PathRadius);
                    minY = Math.Min(minY, p.Y - PathRadius);
                    maxX = Math.Max(maxX, p.X + PathRadius);
                    maxY = Math.Max(maxY, p.Y + PathRadius);
                }

                return sgmentBoundsCache.Value = new RectangleF(minX, minY, maxX - minX, maxY - minY);
            }
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            var localPos = ToLocalSpace(screenSpacePos);
            var pathRadiusSquared = PathRadius * PathRadius;

            foreach (var t in Segments)
                if (t.DistanceSquaredToPoint(localPos) <= pathRadiusSquared)
                    return true;

            return false;
        }

        public Vector2 PositionInBoundingBox(Vector2 pos) => pos - SegmentBounds.TopLeft;

        /// <summary>
        /// List of at least all vertices used to generate this Lines object making up the bounding (outermost) vertices.
        /// It may contain more vertices than needed and bounds will be computed as minimum and maximum from all vertices.
        /// </summary>
        protected abstract IEnumerable<Vector2> BoundingVertices
        {
            get;
        }

        private readonly List<Line> segmentsBacking = new List<Line>();
        private Cached segmentsCache = new Cached();
        public List<Line> Segments => segmentsCache.IsValid ? segmentsBacking : generateSegmentsImpl();

        protected abstract IEnumerable<Line> GenerateSegments();

        private List<Line> generateSegmentsImpl()
        {
            segmentsBacking.Clear();
            segmentsBacking.AddRange(GenerateSegments());
            segmentsCache.Validate();
            return segmentsBacking;
        }

        private Texture texture;

        protected Texture Texture
        {
            get => texture ?? Texture.WhitePixel;
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
        public override DrawColourInfo DrawColourInfo => new DrawColourInfo(Color4.White, base.DrawColourInfo.Blending);

        public Color4 BackgroundColour => new Color4(0, 0, 0, 0);

        private readonly BufferedDrawNodeSharedData sharedData = new BufferedDrawNodeSharedData(new[] { RenderbufferInternalFormat.DepthComponent16 });

        protected override DrawNode CreateDrawNode() => new BufferedDrawNode(this, new LinesDrawNode(this), sharedData);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            texture?.Dispose();
            texture = null;

            sharedData.Dispose();
        }

        protected void InvalidateSegments()
        {
            sgmentBoundsCache.Invalidate();
            segmentsCache.Invalidate();

            Invalidate(Invalidation.DrawSize);
        }
    }
}
