﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
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
        private readonly VertexGroup<TexturedVertex2D> vertices = new VertexGroup<TexturedVertex2D>();
        private readonly VertexGroup<TexturedVertex2D> opaqueVertices = new VertexGroup<TexturedVertex2D>();

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

        protected virtual void Blit(in VertexGroupUsage<TexturedVertex2D> usage)
        {
            if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                return;

            DrawQuad(usage, Texture,
                ScreenSpaceDrawQuad,
                DrawColourInfo.Colour, null, new Vector2(InflationAmount.X / DrawRectangle.Width, InflationAmount.Y / DrawRectangle.Height), null, TextureCoords);
        }

        protected virtual void BlitOpaqueInterior(in VertexGroupUsage<TexturedVertex2D> usage)
        {
            if (DrawRectangle.Width == 0 || DrawRectangle.Height == 0)
                return;

            if (GLWrapper.IsMaskingActive)
                DrawClipped(usage, ref ConservativeScreenSpaceDrawQuad, Texture, DrawColourInfo.Colour);
            else
                DrawQuad(usage, Texture, ConservativeScreenSpaceDrawQuad, DrawColourInfo.Colour, textureCoords: TextureCoords);
        }

        public override void Draw(IRenderer renderer)
        {
            base.Draw(renderer);

            if (Texture?.Available != true)
                return;

            Shader.Bind();

            using (var usage = renderer.BeginQuads(this, vertices))
                Blit(usage);

            Shader.Unbind();
        }

        protected override bool RequiresRoundedShader => base.RequiresRoundedShader || InflationAmount != Vector2.Zero;

        protected override void DrawOpaqueInterior(IRenderer renderer)
        {
            base.DrawOpaqueInterior(renderer);

            if (Texture?.Available != true)
                return;

            TextureShader.Bind();

            using (var usage = renderer.BeginQuads(this, opaqueVertices))
                BlitOpaqueInterior(usage);

            TextureShader.Unbind();
        }

        protected internal override bool CanDrawOpaqueInterior => Texture?.Available == true && Texture.Opacity == Opacity.Opaque && hasOpaqueInterior;
    }
}
