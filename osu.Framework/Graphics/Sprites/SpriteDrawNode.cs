// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
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

        private IUniformBuffer<UniformBlockData> uniformBlockABuffer;
        private IUniformBuffer<UniformBlockData> uniformBlockBBuffer;

        public override void Draw(IRenderer renderer)
        {
            base.Draw(renderer);

            if (Texture?.Available != true)
                return;

            uniformBlockABuffer ??= renderer.CreateUniformBuffer<UniformBlockData>();
            uniformBlockBBuffer ??= renderer.CreateUniformBuffer<UniformBlockData>();
            uniformBlockABuffer.Data = new UniformBlockData(Vector2.One);
            uniformBlockBBuffer.Data = new UniformBlockData(new Vector2(-1, -1));

            var shader = TextureShader;

            shader.Bind();
            shader.AssignUniformBlock("g_UniformBlockA", uniformBlockABuffer);
            shader.AssignUniformBlock("g_UniformBlockB", uniformBlockBBuffer);

            Blit(renderer);

            shader.Unbind();
        }

        private readonly struct UniformBlockData : IEquatable<UniformBlockData>
        {
            public readonly Vector2 Offset;

            public UniformBlockData(Vector2 offset)
            {
                Offset = offset;
            }

            public bool Equals(UniformBlockData other)
            {
                return Offset == other.Offset;
            }

            public override bool Equals(object obj)
            {
                return obj is UniformBlockData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Offset.GetHashCode();
            }
        }

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
