// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.Rendering;

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

        protected virtual void Blit(IRenderer renderer)
        {
            if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                return;

            renderer.DrawQuad(Texture, ScreenSpaceDrawQuad, DrawColourInfo.Colour, null, null,
                new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height),
                null, TextureCoords);
        }

        protected virtual void BlitOpaqueInterior(IRenderer renderer)
        {
            if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                return;

            if (renderer.IsMaskingActive)
                renderer.DrawClipped(ref ConservativeScreenSpaceDrawQuad, Texture, DrawColourInfo.Colour);
            else
                renderer.DrawQuad(Texture, ConservativeScreenSpaceDrawQuad, DrawColourInfo.Colour, textureCoords: TextureCoords);
        }

        public override void Draw(IRenderer renderer)
        {
            base.Draw(renderer);

            if (Texture?.Available != true)
                return;

            var shader = GetAppropriateShader(renderer);

            shader.Bind();

            Blit(renderer);

            shader.Unbind();
        }

        protected override bool RequiresRoundedShader(IRenderer renderer) => base.RequiresRoundedShader(renderer) || InflationAmount != Vector2.Zero;

        protected override void DrawOpaqueInterior(IRenderer renderer)
        {
            base.DrawOpaqueInterior(renderer);

            if (Texture?.Available != true)
                return;

            TextureShader.Bind();

            BlitOpaqueInterior(renderer);

            TextureShader.Unbind();
        }

        protected internal override bool CanDrawOpaqueInterior => Texture?.Available == true && Texture.Opacity == Opacity.Opaque && hasOpaqueInterior;
    }
}
