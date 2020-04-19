// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Layout;
using osuTK;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Shapes
{
    /// <summary>
    /// A simple rectangular box. Can be colored using the <see cref="Drawable.Colour"/> property.
    /// </summary>
    public class Box : Sprite
    {
        public Box()
        {
            base.Texture = Texture.WhitePixel;

            AddLayout(conservativeScreenSpaceDrawQuadBacking);
        }

        public override Texture Texture
        {
            get => base.Texture;
            set => throw new InvalidOperationException($"The texture of a {nameof(Box)} cannot be set.");
        }

        protected override DrawNode CreateDrawNode() => new BoxDrawNode(this);

        // Matches the invalidation types of Drawable.screenSpaceDrawQuadBacking
        private readonly LayoutValue<Quad> conservativeScreenSpaceDrawQuadBacking = new LayoutValue<Quad>(Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit | Invalidation.Presence);

        private Quad conservativeScreenSpaceDrawQuad => conservativeScreenSpaceDrawQuadBacking.IsValid
            ? conservativeScreenSpaceDrawQuadBacking
            : conservativeScreenSpaceDrawQuadBacking.Value = ComputeConservativeScreenSpaceDrawQuad();

        protected virtual Quad ComputeConservativeScreenSpaceDrawQuad()
        {
            if (EdgeSmoothness == Vector2.Zero)
                return ScreenSpaceDrawQuad;

            return ToScreenSpace(DrawRectangle);
        }

        protected class BoxDrawNode : SpriteDrawNode
        {
            protected new Box Source => (Box)base.Source;

            private Quad conservativeScreenSpaceDrawQuad;
            private bool hasOpaqueInterior;

            public BoxDrawNode(Box source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                conservativeScreenSpaceDrawQuad = Source.conservativeScreenSpaceDrawQuad;

                hasOpaqueInterior = DrawColourInfo.Colour.MinAlpha == 1
                                    && DrawColourInfo.Blending == BlendingParameters.Mixture
                                    && DrawColourInfo.Colour.HasSingleColour;
            }

            protected override void DrawOpaqueInterior(Action<TexturedVertex2D> vertexAction)
            {
                base.DrawOpaqueInterior(vertexAction);

                TextureShader.Bind();
                Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;

                if (GLWrapper.IsMaskingActive)
                    DrawClipped(ref conservativeScreenSpaceDrawQuad, Texture, DrawColourInfo.Colour, vertexAction: vertexAction);
                else
                {
                    ReadOnlySpan<Vector2> vertices = conservativeScreenSpaceDrawQuad.GetVertices();

                    for (int i = 2; i < vertices.Length; i++)
                        DrawTriangle(Texture, new Primitives.Triangle(vertices[0], vertices[i - 1], vertices[i]), DrawColourInfo.Colour, vertexAction: vertexAction);
                }

                TextureShader.Unbind();
            }

            protected internal override bool CanDrawOpaqueInterior => Texture?.Available == true && hasOpaqueInterior;
        }
    }
}
