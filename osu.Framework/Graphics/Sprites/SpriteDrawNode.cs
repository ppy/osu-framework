// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using osu.Framework.Graphics.Batches;
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

        protected virtual void Blit(QuadBatch<TexturedVertex2D> batch)
        {
            using (batch.BeginUsage(ref BatchUsage, this))
            {
                DrawQuad(Texture, ScreenSpaceDrawQuad, DrawColourInfo.Colour, ref BatchUsage, null,
                    new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height),
                    null, TextureCoords);
            }
        }

        protected virtual void BlitOpaqueInterior(QuadBatch<TexturedVertex2D> batch)
        {
            using (batch.BeginUsage(ref OpaqueInteriorBatchUsage, this))
            {
                if (GLWrapper.IsMaskingActive)
                    DrawClipped(ref ConservativeScreenSpaceDrawQuad, Texture, DrawColourInfo.Colour, ref OpaqueInteriorBatchUsage);
                else
                    DrawQuad(Texture, ConservativeScreenSpaceDrawQuad, DrawColourInfo.Colour, ref OpaqueInteriorBatchUsage, textureCoords: TextureCoords);
            }
        }

        public override void Draw(in DrawState drawState)
        {
            base.Draw(drawState);

            if (Texture?.Available != true)
                return;

            Shader.Bind();

            Blit(drawState.QuadBatch);

            Shader.Unbind();
        }

        protected override bool RequiresRoundedShader => base.RequiresRoundedShader || InflationAmount != Vector2.Zero;

        protected override void DrawOpaqueInterior(in DrawState drawState)
        {
            base.DrawOpaqueInterior(drawState);

            if (Texture?.Available != true)
                return;

            TextureShader.Bind();

            BlitOpaqueInterior(drawState.QuadBatch);

            TextureShader.Unbind();
        }

        protected internal override bool CanDrawOpaqueInterior => Texture?.Available == true && Texture.Opacity == Opacity.Opaque && hasOpaqueInterior;
    }
}
