// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Caching;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.MathUtils.Clipping;
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
            Texture = Texture.WhitePixel;
        }

        private Cached<Quad> conservativeScreenSpaceDrawQuadBacking;

        private Quad conservativeScreenSpaceDrawQuad => conservativeScreenSpaceDrawQuadBacking.IsValid
            ? conservativeScreenSpaceDrawQuadBacking.Value
            : conservativeScreenSpaceDrawQuadBacking.Value = Quad.FromRectangle(DrawRectangle) * DrawInfo.Matrix;

        protected override DrawNode CreateDrawNode() => new BoxDrawNode(this);

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            bool alreadyInvalidated = base.Invalidate(invalidation, source, shallPropagate);

            // Either ScreenSize OR ScreenPosition OR Presence
            if ((invalidation & (Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit | Invalidation.Presence)) > 0)
                alreadyInvalidated &= !conservativeScreenSpaceDrawQuadBacking.Invalidate();

            return !alreadyInvalidated;
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
                {
                    var maskingQuad = GLWrapper.CurrentMaskingInfo.ConservativeScreenSpaceQuad;

                    var clipper = new ConvexPolygonClipper<Quad, Quad>(ref conservativeScreenSpaceDrawQuad, ref maskingQuad);
                    Span<Vector2> buffer = stackalloc Vector2[clipper.GetClipBufferSize()];
                    Span<Vector2> clippedRegion = clipper.Clip(buffer);

                    for (int i = 2; i < clippedRegion.Length; i++)
                        DrawTriangle(Texture, new Primitives.Triangle(clippedRegion[0], clippedRegion[i - 1], clippedRegion[i]), DrawColourInfo.Colour);
                }
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
