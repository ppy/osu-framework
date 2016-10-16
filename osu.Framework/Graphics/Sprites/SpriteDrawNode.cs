// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES20;
using OpenTK;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteDrawNode : DrawNode
    {
        public Shader Shader;
        public Texture Texture;
        public Quad ScreenSpaceDrawQuad;
        public bool WrapTexture;
        public float Radius;
        public Vector2 Size;

        protected override void Draw()
        {
            base.Draw();

            if (Texture == null || Texture.IsDisposed)
                return;

            if (!Shader.Loaded) Shader.Compile();

            RectangleF texRect = Texture.GetTextureRect();

            Shader.GetUniform<Vector4>(@"g_TexRect").Value = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom);
            Shader.GetUniform<Vector2>(@"g_TexSize").Value = Vector2.Multiply(Texture.TextureGL.Native.Size, Size);
            Shader.GetUniform<float>(@"g_Radius").Value = Radius;

            Shader.Bind();

            Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;
            Texture.Draw(ScreenSpaceDrawQuad, DrawInfo.Colour);

            Shader.Unbind();
        }
    }
}
