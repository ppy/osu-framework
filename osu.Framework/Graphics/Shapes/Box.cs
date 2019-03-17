// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Caching;
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
        public Box()
        {
            Texture = Texture.WhitePixel;
        }

        private Cached<Quad> conservativeScreenSpaceDrawQuadBacking;

        private Quad conservativeScreenSpaceDrawQuad => conservativeScreenSpaceDrawQuadBacking.IsValid
            ? conservativeScreenSpaceDrawQuadBacking.Value
            : conservativeScreenSpaceDrawQuadBacking.Value = Quad.FromRectangle(DrawRectangle) * DrawInfo.Matrix;

        protected override void ApplyDrawNode(DrawNode node)
        {
            var n = (BoxDrawNode)node;

            n.ConservativeScreenSpaceDrawQuad = conservativeScreenSpaceDrawQuad;

            base.ApplyDrawNode(node);
        }

        protected override DrawNode CreateDrawNode() => new BoxDrawNode();

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
            public Quad ConservativeScreenSpaceDrawQuad;

            public override void DrawHull(Action<TexturedVertex2D> vertexAction, DepthValue depthValue)
            {
                base.DrawHull(vertexAction, depthValue);

                if (Texture?.Available != true)
                    return;

                if (DrawColourInfo.Colour.MinAlpha != 1 || DrawColourInfo.Blending.RGBEquation != BlendEquationMode.FuncAdd || !DrawColourInfo.Colour.HasSingleColour)
                    return;

                TextureShader.Bind();
                Texture.TextureGL.WrapMode = WrapTexture ? TextureWrapMode.Repeat : TextureWrapMode.ClampToEdge;

                if (GLWrapper.IsMaskingActive)
                {
                    var clipper = new ConvexPolygonClipper(ConservativeScreenSpaceDrawQuad, GLWrapper.CurrentMaskingInfo.ConservativeScreenSpaceQuad);

                    Span<Vector2> buffer = stackalloc Vector2[clipper.GetBufferSize()];
                    Span<Vector2> clippedRegion = clipper.Clip(buffer);

                    for (int i = 2; i < clippedRegion.Length; i++)
                        Texture.DrawTriangle(new Primitives.Triangle(clippedRegion[0], clippedRegion[i - 1], clippedRegion[i]), Depth, DrawColourInfo.Colour);
                }
                else
                    Blit(ConservativeScreenSpaceDrawQuad, vertexAction);

                TextureShader.Unbind();

                depthValue.Increment();
            }
        }
    }
}
