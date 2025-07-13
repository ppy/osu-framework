﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Allocation;
using System.Collections.Generic;
using osu.Framework.Caching;
using osu.Framework.Extensions.EnumExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Layout;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Lines
{
    public partial class Path : Drawable, IBufferedDrawable
    {
        public IShader TextureShader { get; private set; }
        private IShader pathShader;

        [Resolved]
        private IRenderer renderer { get; set; }

        public Path()
        {
            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
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

                bbhCache.Invalidate();

                Invalidate(Invalidation.DrawSize);
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

                bbhCache.Invalidate();

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
                    throw new InvalidOperationException($"The width of a {nameof(Path)} with {nameof(AutoSizeAxes)} can not be set manually.");

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
                    throw new InvalidOperationException($"The height of a {nameof(Path)} with {nameof(AutoSizeAxes)} can not be set manually.");

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
                    throw new InvalidOperationException($"The Size of a {nameof(Path)} with {nameof(AutoSizeAxes)} can not be set manually.");

                base.Size = value;
            }
        }

        public float StartProgress
        {
            get => bbh.StartProgress;
            set
            {
                bbh.StartProgress = value;
                Invalidate(Invalidation.DrawSize);
            }
        }

        public float EndProgress
        {
            get => bbh.EndProgress;
            set
            {
                bbh.EndProgress = value;
                Invalidate(Invalidation.DrawSize);
            }
        }

        private RectangleF vertexBounds => bbh.VertexBounds;

        public Vector2 CurvePositionAt(float progress) => bbh.CurvePositionAt(progress)?.position ?? Vector2.Zero;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            var localPos = ToLocalSpace(screenSpacePos);
            return bbh.Contains(localPos);
        }

        public Vector2 PositionInBoundingBox(Vector2 pos) => pos - vertexBounds.TopLeft;

        public void ClearVertices()
        {
            if (vertices.Count == 0)
                return;

            vertices.Clear();

            bbhCache.Invalidate();

            Invalidate(Invalidation.DrawSize);
        }

        public void AddVertex(Vector2 pos)
        {
            vertices.Add(pos);

            bbhCache.Invalidate();

            Invalidate(Invalidation.DrawSize);
        }

        public void ReplaceVertex(int index, Vector2 pos)
        {
            vertices[index] = pos;

            bbhCache.Invalidate();

            Invalidate(Invalidation.DrawSize);
        }

        private readonly PathBBH bbhBacking = new PathBBH();
        private readonly Cached bbhCache = new Cached();

        private PathBBH bbh => bbhCache.IsValid ? bbhBacking : computeBBH();

        private PathBBH computeBBH()
        {
            bbhBacking.SetVertices(vertices, pathRadius);
            bbhCache.Validate();
            return bbhBacking;
        }

        private Texture texture;

        protected Texture Texture
        {
            get => texture ?? renderer?.WhitePixel;
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

        public Vector2 FrameBufferScale { get; } = Vector2.One;

        // The path should not receive the true colour to avoid colour doubling when the frame-buffer is rendered to the back-buffer.
        public override DrawColourInfo DrawColourInfo => new DrawColourInfo(Color4.White, base.DrawColourInfo.Blending);

        private Color4 backgroundColour = new Color4(0, 0, 0, 0);

        /// <summary>
        /// The background colour to be used for the frame buffer this path is rendered to.
        /// </summary>
        public virtual Color4 BackgroundColour
        {
            get => backgroundColour;
            set
            {
                backgroundColour = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        public long PathInvalidationID { get; private set; }

        protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
        {
            bool result = base.OnInvalidate(invalidation, source);

            // Colour is being applied to the buffer instead of the actual drawable, thus removing the need to redraw the path on colour invalidation.
            invalidation &= ~Invalidation.Colour;

            if (invalidation != Invalidation.None)
                PathInvalidationID++;

            return result;
        }

        public List<RectangleF> BoundingBoxes()
        {
            List<RectangleF> boxes = new List<RectangleF>();
            bbh.CollectBoundingBoxes(boxes);
            return boxes;
        }

        private readonly BufferedDrawNodeSharedData sharedData = new BufferedDrawNodeSharedData(new[] { RenderBufferFormat.D16 }, clipToRootNode: true);

        protected override DrawNode CreateDrawNode() => new PathBufferedDrawNode(this, new PathDrawNode(this), sharedData);

        private class PathBufferedDrawNode : BufferedDrawNode
        {
            protected new Path Source => (Path)base.Source;

            public PathBufferedDrawNode(Path source, PathDrawNode child, BufferedDrawNodeSharedData sharedData)
                : base(source, child, sharedData)
            {
            }

            private long pathInvalidationID = -1;

            public override void ApplyState()
            {
                base.ApplyState();
                pathInvalidationID = Source.PathInvalidationID;
            }

            protected override long GetDrawVersion() => pathInvalidationID;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            texture?.Dispose();
            texture = null;

            sharedData.Dispose();
            bbhBacking?.FreeArray();
        }
    }
}
