//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics.ES20;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Graphics.Sprites
{
    public class SpriteDrawNode : DrawNode
    {
        private static Shader shader;

        private Game game;
        private Texture texture;
        private Quad screenSpaceDrawQuad;
        private bool wrapTexture;

        public SpriteDrawNode(Game game, DrawInfo drawInfo, Texture texture, Quad screenSpaceDrawQuad, bool wrapTexture)
            : base(drawInfo)
        {
            this.game = game;
            this.texture = texture;
            this.screenSpaceDrawQuad = screenSpaceDrawQuad;
            this.wrapTexture = wrapTexture;
        }

        protected override void Draw()
        {
            base.Draw();

            if (texture == null || texture.IsDisposed)
                return;

            if (shader == null)
                shader = game.Shaders.Load(VertexShader.Texture2D, FragmentShader.Texture);

            shader.Bind();

            texture.TextureGL.WrapMode = wrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;
            texture.Draw(screenSpaceDrawQuad, DrawInfo.Colour, new RectangleF(0, 0, texture.DisplayWidth, texture.DisplayHeight));

            shader.Unbind();
        }
    }
}
