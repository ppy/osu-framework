// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES20;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteDrawNode : ShadedDrawNode
    {
        public Texture Texture;
        public Quad ScreenSpaceDrawQuad;
        public bool WrapTexture;

        protected override void Draw()
        {
            base.Draw();

            if (Texture == null || Texture.IsDisposed)
                return;

            Shader.Bind();

            Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;
            Texture.Draw(ScreenSpaceDrawQuad, DrawInfo.Colour);

            Shader.Unbind();
        }
    }
}
