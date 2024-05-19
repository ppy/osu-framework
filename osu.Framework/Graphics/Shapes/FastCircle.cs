// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osuTK;

namespace osu.Framework.Graphics.Shapes
{
    public partial class FastCircle : Drawable
    {
        private IShader shader = null!;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "FastCircle");
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
            private IShader shader = null!;

            public override void ApplyState()
            {
                base.ApplyState();

                screenSpaceDrawQuad = Source.ScreenSpaceDrawQuad;
                drawRectangle = new Vector4(0, 0, screenSpaceDrawQuad.Width, screenSpaceDrawQuad.Height);
                shader = Source.shader;
            }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                shader.Bind();

                var vertexAction = renderer.DefaultQuadBatch.AddAction;

                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = screenSpaceDrawQuad.BottomLeft,
                    TexturePosition = new Vector2(0, screenSpaceDrawQuad.Height),
                    TextureRect = drawRectangle,
                    BlendRange = Vector2.Zero,
                    Colour = DrawColourInfo.Colour.BottomLeft.SRGB,
                });
                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = screenSpaceDrawQuad.BottomRight,
                    TexturePosition = new Vector2(screenSpaceDrawQuad.Width, screenSpaceDrawQuad.Height),
                    TextureRect = drawRectangle,
                    BlendRange = Vector2.Zero,
                    Colour = DrawColourInfo.Colour.BottomRight.SRGB,
                });
                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = screenSpaceDrawQuad.TopRight,
                    TexturePosition = new Vector2(screenSpaceDrawQuad.Width, 0),
                    TextureRect = drawRectangle,
                    BlendRange = Vector2.Zero,
                    Colour = DrawColourInfo.Colour.TopRight.SRGB,
                });
                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = screenSpaceDrawQuad.TopLeft,
                    TexturePosition = new Vector2(0, 0),
                    TextureRect = drawRectangle,
                    BlendRange = Vector2.Zero,
                    Colour = DrawColourInfo.Colour.TopLeft.SRGB,
                });

                shader.Unbind();
            }
        }
    }
}
