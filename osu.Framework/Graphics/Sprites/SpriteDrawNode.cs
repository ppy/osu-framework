// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// Draw node containing all necessary information to draw a <see cref="Sprite"/>.
    /// </summary>
    public class SpriteDrawNode : TexturedShaderDrawNode
    {
        protected Texture Texture { get; private set; }
        protected Quad ScreenSpaceDrawQuad { get; private set; }

        protected RectangleF DrawRectangle { get; private set; }
        protected Vector2 InflationAmount { get; private set; }

        protected bool WrapTexture { get; private set; }

        protected new Sprite Source => (Sprite)base.Source;

        public SpriteDrawNode(Sprite source)
            : base(source)
        {
        }

        public override void ApplyState()
        {
            base.ApplyState();

            Texture = Source.Texture;
            ScreenSpaceDrawQuad = Source.ScreenSpaceDrawQuad;
            DrawRectangle = Source.DrawRectangle;
            InflationAmount = Source.InflationAmount;
            WrapTexture = Source.WrapTexture;
        }

        protected virtual void Blit(Action<TexturedVertex2D> vertexAction)
        {
            DrawQuad(Texture, ScreenSpaceDrawQuad, DrawColourInfo.Colour, null, vertexAction,
                new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height));
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture?.Available != true)
                return;

            Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;

            Shader.Bind();

            Blit(vertexAction);

            Shader.Unbind();
        }

        protected override bool RequiresRoundedShader => base.RequiresRoundedShader || InflationAmount != Vector2.Zero;
    }
}
