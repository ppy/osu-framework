// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// An arbitrary shape defined by a set of vertices forming a closed curve.
    /// </summary>
    public partial class ArbitraryShape : Drawable
    {
        public IShader RoundedTextureShader { get; private set; }
        public IShader TextureShader { get; private set; }

        [Resolved]
        private IRenderer renderer { get; set; }
        private ulong verticeInvalidationId = 1;

        public ArbitraryShape()
        {
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            RoundedTextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE_ROUNDED);
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
        }

        private FillRule fillRule = FillRule.NonZero;

        /// <summary>
        /// Defines how an <see cref="ArbitraryShape"/> should be rendered.
        /// The default is <see cref="FillRule.NonZero"/>.
        /// </summary>
        public FillRule FillRule
        {
            get => fillRule;
            set
            {
                if (value == fillRule)
                    return;

                fillRule = value;
                Invalidate(Invalidation.DrawNode);
            }
        }
        private readonly List<Vector2> vertices = new List<Vector2>();

        public IReadOnlyList<Vector2> Vertices
        {
            get => vertices;
            set
            {
                vertices.Clear();
                vertices.AddRange(value);

                vertexBoundsCache.Invalidate();

                verticeInvalidationId++;
                Invalidate(Invalidation.DrawSize);
            }
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
                if (AutoSizeAxes.HasFlagFast(Axes.X))
                    return base.Width = vertexBounds.Width;

                return base.Width;
            }
            set
            {
                if ((AutoSizeAxes & Axes.X) != 0)
                    throw new InvalidOperationException($"The width of a {nameof(ArbitraryShape)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Width = value;
            }
        }

        public override float Height
        {
            get
            {
                if (AutoSizeAxes.HasFlagFast(Axes.Y))
                    return base.Height = vertexBounds.Height;

                return base.Height;
            }
            set
            {
                if ((AutoSizeAxes & Axes.Y) != 0)
                    throw new InvalidOperationException($"The height of a {nameof(ArbitraryShape)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Height = value;
            }
        }

        public override Vector2 Size
        {
            get
            {
                if (AutoSizeAxes != Axes.None)
                    return base.Size = vertexBounds.Size;

                return base.Size;
            }
            set
            {
                if ((AutoSizeAxes & Axes.Both) != 0)
                    throw new InvalidOperationException($"The Size of a {nameof(ArbitraryShape)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Size = value;
            }
        }

        private readonly Cached<RectangleF> vertexBoundsCache = new Cached<RectangleF>();

        private RectangleF vertexBounds
        {
            get
            {
                if (vertexBoundsCache.IsValid)
                    return vertexBoundsCache.Value;

                if (vertices.Count > 0)
                {
                    float minX = 0;
                    float minY = 0;
                    float maxX = 0;
                    float maxY = 0;

                    foreach (var v in vertices)
                    {
                        minX = Math.Min(minX, v.X);
                        minY = Math.Min(minY, v.Y);
                        maxX = Math.Max(maxX, v.X);
                        maxY = Math.Max(maxY, v.Y);
                    }

                    return vertexBoundsCache.Value = new RectangleF(minX, minY, maxX - minX, maxY - minY);
                }

                return vertexBoundsCache.Value = new RectangleF(0, 0, 0, 0);
            }
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            //var localPos = ToLocalSpace(screenSpacePos);
            //float pathRadiusSquared = PathRadius * PathRadius;

            //foreach ( var t in segments )
            //{
            //    if ( t.DistanceSquaredToPoint(localPos) <= pathRadiusSquared )
            //        return true;
            //}

            return false;
        }

        public Vector2 PositionInBoundingBox(Vector2 pos) => pos - vertexBounds.TopLeft;

        public void ClearVertices()
        {
            if (vertices.Count == 0)
                return;

            vertices.Clear();

            vertexBoundsCache.Invalidate();

            verticeInvalidationId++;
            Invalidate(Invalidation.DrawSize);
        }

        public void AddVertex(Vector2 pos)
        {
            vertices.Add(pos);

            vertexBoundsCache.Invalidate();

            verticeInvalidationId++;
            Invalidate(Invalidation.DrawSize);
        }

        private Texture texture;

        public Texture Texture
        {
            get => texture ?? renderer?.WhitePixel;
            set
            {
                if (texture == value)
                    return;

                texture = value;

                Invalidate(Invalidation.DrawNode);
            }
        }

        protected override DrawNode CreateDrawNode() => new ArbitraryShapeDrawNode(this);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            texture?.Dispose();
            texture = null;
        }
    }

    /// <summary>
    /// Defines how a shape's fill should be rendered.
    /// </summary>
    public enum FillRule
    {
        /// <summary>
        /// A point is considered inside the shape if a ray from it intersects lines formed by the vertices so that
        /// the difference between clockwise and counterclockwise winding lines is non-zero. This requires a stencil buffer.
        /// </summary>
        NonZero,

        /// <summary>
        /// A point is considered inside the shape if a ray from it intersects lines formed by the vertices an odd number of times
        /// and outside if it intersects an even number. This requires a stencil buffer.
        /// </summary>
        EvenOdd,

        /// <summary>
        /// The shape is convex and rendered as a triangle fan.
        /// </summary>
        Fan
    }
}
