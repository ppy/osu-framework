// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Graphics.Shapes
{
    public partial class FastCircle : Sprite
    {
        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders, IRenderer renderer)
        {
            TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, "FastCircle");
            Texture = renderer.WhitePixel;
        }

        protected override DrawNode CreateDrawNode() => new FastCircleDrawNode(this);

        private class FastCircleDrawNode : SpriteDrawNode
        {
            public FastCircleDrawNode(FastCircle source)
                : base(source)
            {
            }

            private Vector2 drawSize;

            public override void ApplyState()
            {
                base.ApplyState();
                drawSize = Source.DrawSize;
            }

            protected override void Blit(IRenderer renderer)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                if (!renderer.BindTexture(Texture))
                    return;

                var vertexAction = renderer.DefaultQuadBatch.AddAction;

                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = ScreenSpaceDrawQuad.BottomLeft,
                    TexturePosition = new Vector2(0, drawSize.Y),
                    TextureRect = new Vector4(0, 0, drawSize.X, drawSize.Y),
                    BlendRange = Vector2.Zero,
                    Colour = DrawColourInfo.Colour.BottomLeft.SRGB,
                });
                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = ScreenSpaceDrawQuad.BottomRight,
                    TexturePosition = new Vector2(drawSize.X, drawSize.Y),
                    TextureRect = new Vector4(0, 0, drawSize.X, drawSize.Y),
                    BlendRange = Vector2.Zero,
                    Colour = DrawColourInfo.Colour.BottomRight.SRGB,
                });
                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = ScreenSpaceDrawQuad.TopRight,
                    TexturePosition = new Vector2(drawSize.X, 0),
                    TextureRect = new Vector4(0, 0, drawSize.X, drawSize.Y),
                    BlendRange = Vector2.Zero,
                    Colour = DrawColourInfo.Colour.TopRight.SRGB,
                });
                vertexAction(new TexturedVertex2D(renderer)
                {
                    Position = ScreenSpaceDrawQuad.TopLeft,
                    TexturePosition = new Vector2(0, 0),
                    TextureRect = new Vector4(0, 0, drawSize.X, drawSize.Y),
                    BlendRange = Vector2.Zero,
                    Colour = DrawColourInfo.Colour.TopLeft.SRGB,
                });
            }
        }
    }
}
