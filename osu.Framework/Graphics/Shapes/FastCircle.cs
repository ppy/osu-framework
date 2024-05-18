// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics.Rendering;
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

            protected override void Blit(IRenderer renderer)
            {
                if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                    return;

                renderer.DrawQuad(Texture, ScreenSpaceDrawQuad, DrawColourInfo.Colour, null, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height),
                    null, TextureCoords);
            }
        }
    }
}
