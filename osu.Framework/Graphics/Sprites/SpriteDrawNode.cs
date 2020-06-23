// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using System;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;

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

        protected RectangleF TextureCoords { get; private set; }

        protected new Sprite Source => (Sprite)base.Source;

        protected Quad ConservativeScreenSpaceDrawQuad;

        private bool hasOpaqueInterior;

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

            TextureCoords = Source.DrawRectangle.RelativeIn(Source.DrawTextureRectangle);
            if (Texture != null)
                TextureCoords *= new Vector2(Texture.DisplayWidth, Texture.DisplayHeight);

            hasOpaqueInterior = DrawColourInfo.Colour.MinAlpha == 1
                                && DrawColourInfo.Blending == BlendingParameters.Mixture
                                && DrawColourInfo.Colour.HasSingleColour;

            if (CanDrawOpaqueInterior)
                ConservativeScreenSpaceDrawQuad = Source.ConservativeScreenSpaceDrawQuad;
        }

        protected virtual void Blit(Action<TexturedVertex2D> vertexAction)
        {
            DrawQuad(Texture, ScreenSpaceDrawQuad, DrawColourInfo.Colour, null, vertexAction,
                new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height),
                null, TextureCoords);
        }

        protected virtual void BlitOpaqueInterior(Action<TexturedVertex2D> vertexAction)
        {
            if (GLWrapper.IsMaskingActive)
                DrawClipped(ref ConservativeScreenSpaceDrawQuad, Texture, DrawColourInfo.Colour, vertexAction: vertexAction);
            else
                DrawQuad(Texture, ConservativeScreenSpaceDrawQuad, DrawColourInfo.Colour, vertexAction: vertexAction, textureCoords: TextureCoords);
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture?.Available != true)
                return;

            Shader.Bind();

            Blit(vertexAction);

            Shader.Unbind();
        }

        protected override bool RequiresRoundedShader => base.RequiresRoundedShader || InflationAmount != Vector2.Zero;

        protected override void DrawOpaqueInterior(Action<TexturedVertex2D> vertexAction)
        {
            base.DrawOpaqueInterior(vertexAction);

            if (Texture?.Available != true)
                return;

            TextureShader.Bind();

            BlitOpaqueInterior(vertexAction);

            TextureShader.Unbind();
        }

        protected internal override bool CanDrawOpaqueInterior => Texture?.Available == true && Texture.Opacity == Opacity.Opaque && hasOpaqueInterior;
    }
}
