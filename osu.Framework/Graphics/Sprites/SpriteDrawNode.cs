// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL;
using OpenTK;
using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Draw node containing all necessary information to draw a <see cref="Sprite"/>.
    /// </summary>
    public class SpriteDrawNode : DrawNode
    {
        public Texture Texture;
        public Quad ScreenSpaceDrawQuad;
        public RectangleF DrawRectangle;
        public Vector2 InflationAmount;
        public bool WrapTexture;

        public Shader TextureShader;
        public Shader RoundedTextureShader;

        private bool needsRoundedShader => GLWrapper.IsMaskingActive || InflationAmount != Vector2.Zero;

        protected virtual void Blit(Action<TexturedVertex2D> vertexAction)
        {
            Texture.DrawQuad(ScreenSpaceDrawQuad, DrawInfo.Colour, null, vertexAction,
                new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height));
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture == null || Texture.IsDisposed)
                return;

            Shader shader = needsRoundedShader ? RoundedTextureShader : TextureShader;

            shader.Bind();

            Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;

            Blit(vertexAction);

            shader.Unbind();
        }
    }
}
