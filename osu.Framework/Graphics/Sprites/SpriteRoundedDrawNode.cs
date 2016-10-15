// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES20;
using OpenTK;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteRoundedDrawNode : SpriteDrawNode
    {
        public float Radius;

        protected override void Draw()
        {
            if (Texture == null || Texture.IsDisposed)
                return;

            if (!Shader.Loaded) Shader.Compile();

            RectangleF texRect = Texture.GetTextureRect();

            Shader.GetUniform<Vector4>(@"g_TexRect").Value = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom);
            Shader.GetUniform<Vector2>(@"g_Radius").Value = Vector2.Divide(new Vector2(Radius), new Vector2(Texture.Width, Texture.Height));

            base.Draw();
        }
    }
}
