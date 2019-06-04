// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A simple rectangular box. Can be colored using the <see cref="Drawable.Colour"/> property.
    /// </summary>
    public class Box : Sprite
    {
        private Quad conservativeScreenSpaceDrawQuad;

        public Box()
        {
            Texture = Texture.WhitePixel;
        }

        protected override DrawNode CreateDrawNode() => new BoxDrawNode(this);

        protected override Quad ComputeScreenSpaceDrawQuad()
        {
            conservativeScreenSpaceDrawQuad = ToScreenSpace(DrawRectangle);
            return base.ComputeScreenSpaceDrawQuad();
        }

        protected class BoxDrawNode : SpriteDrawNode
        {
            protected new Box Source => (Box)base.Source;

            private Quad conservativeScreenSpaceDrawQuad;

            public BoxDrawNode(Box source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                conservativeScreenSpaceDrawQuad = Source.conservativeScreenSpaceDrawQuad;
            }

            protected override void DrawHull(Action<TexturedVertex2D> vertexAction)
            {
                base.DrawHull(vertexAction);

                TextureShader.Bind();
                Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;

                if (GLWrapper.IsMaskingActive)
                    DrawClipped(ref conservativeScreenSpaceDrawQuad, Texture, DrawColourInfo.Colour);
                else
                    Blit(conservativeScreenSpaceDrawQuad, vertexAction);

                TextureShader.Unbind();
            }

            protected override bool CanDrawHull =>
                Texture?.Available == true
                && DrawColourInfo.Colour.MinAlpha == 1
                && DrawColourInfo.Blending.RGBEquation == BlendEquationMode.FuncAdd
                && DrawColourInfo.Colour.HasSingleColour;
        }
    }
}
