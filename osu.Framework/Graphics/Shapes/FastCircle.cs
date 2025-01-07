// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osuTK;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A circle that is rendered directly to the screen using a specialised shader.
    /// This behaves slightly differently from <see cref="Circle"/> but offers
    /// higher performance in scenarios where many circles are drawn at once.
    /// </summary>
    public partial class FastCircle : Drawable
    {
        private float edgeSmoothness = 1f;

        public float EdgeSmoothness
        {
            get => edgeSmoothness;
            set
            {
                if (edgeSmoothness == value)
                    return;

                edgeSmoothness = value;

                if (IsLoaded)
                    Invalidate(Invalidation.DrawNode);
            }
        }

        private IShader shader = null!;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "FastCircle");
        }

        public float Radius => MathF.Min(DrawSize.X, DrawSize.Y) * 0.5f;

        public override bool Contains(Vector2 screenSpacePos)
        {
            if (!base.Contains(screenSpacePos))
                return false;

            float cRadius = Radius;
            return DrawRectangle.Shrink(cRadius).DistanceExponentiated(ToLocalSpace(screenSpacePos), 2f) <= cRadius * cRadius;
        }

        protected override DrawNode CreateDrawNode() => new FastCircleDrawNode(this);

        private class FastCircleDrawNode : DrawNode
        {
            protected new FastCircle Source => (FastCircle)base.Source;

            public FastCircleDrawNode(FastCircle source)
                : base(source)
            {
            }

            private Quad screenSpaceDrawQuad;
            private Vector4 drawRectangle;
            private Vector2 blend;
            private IShader shader = null!;

            public override void ApplyState()
            {
                base.ApplyState();

                screenSpaceDrawQuad = Source.ScreenSpaceDrawQuad;
                drawRectangle = new Vector4(0, 0, Source.DrawWidth, Source.DrawHeight);
                shader = Source.shader;
                blend = new Vector2(Source.edgeSmoothness * Math.Min(Source.DrawWidth, Source.DrawHeight) / Math.Min(screenSpaceDrawQuad.Width, screenSpaceDrawQuad.Height));
            }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (!renderer.BindTexture(renderer.WhitePixel))
                    return;

                shader.Bind();

                var vertexAction = renderer.DefaultQuadBatch.AddAction;

                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = screenSpaceDrawQuad.BottomLeft,
                    TexturePosition = new Vector2(0, drawRectangle.W),
                    TextureRect = drawRectangle,
                    BlendRange = blend,
                    Colour = DrawColourInfo.Colour.BottomLeft.SRGB,
                });
                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = screenSpaceDrawQuad.BottomRight,
                    TexturePosition = new Vector2(drawRectangle.Z, drawRectangle.W),
                    TextureRect = drawRectangle,
                    BlendRange = blend,
                    Colour = DrawColourInfo.Colour.BottomRight.SRGB,
                });
                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = screenSpaceDrawQuad.TopRight,
                    TexturePosition = new Vector2(drawRectangle.Z, 0),
                    TextureRect = drawRectangle,
                    BlendRange = blend,
                    Colour = DrawColourInfo.Colour.TopRight.SRGB,
                });
                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = screenSpaceDrawQuad.TopLeft,
                    TexturePosition = Vector2.Zero,
                    TextureRect = drawRectangle,
                    BlendRange = blend,
                    Colour = DrawColourInfo.Colour.TopLeft.SRGB,
                });

                shader.Unbind();
            }
        }
    }
}
