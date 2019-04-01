// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL;
using osuTK;
using System;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Draw node containing all necessary information to draw a <see cref="Sprite"/>.
    /// </summary>
    public class SpriteDrawNode : DrawNode
    {
        protected Texture Texture { get; private set; }
        protected Quad ScreenSpaceDrawQuad { get; private set; }

        protected RectangleF DrawRectangle { get; private set; }
        protected Vector2 InflationAmount { get; private set; }

        private bool wrapTexture;

        private IShader textureShader;
        private IShader roundedTextureShader;

        public override void ApplyFromDrawable(Drawable source)
        {
            base.ApplyFromDrawable(source);

            var sprite = (Sprite)source;

            ScreenSpaceDrawQuad = sprite.ScreenSpaceDrawQuad;
            DrawRectangle = sprite.DrawRectangle;
            Texture = sprite.Texture;
            wrapTexture = sprite.WrapTexture;
            InflationAmount = sprite.InflationAmount;
            textureShader = sprite.TextureShader;
            roundedTextureShader = sprite.RoundedTextureShader;
        }

        private bool needsRoundedShader => GLWrapper.IsMaskingActive || InflationAmount != Vector2.Zero;

        protected virtual void Blit(Action<TexturedVertex2D> vertexAction)
        {
            Texture.DrawQuad(ScreenSpaceDrawQuad, DrawColourInfo.Colour, null, vertexAction,
                new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height));
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture?.Available != true)
                return;

            IShader shader = needsRoundedShader ? roundedTextureShader : textureShader;

            shader.Bind();

            Texture.TextureGL.WrapMode = wrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;

            Blit(vertexAction);

            shader.Unbind();
        }
    }
}
