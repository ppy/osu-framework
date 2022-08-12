// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;
using osuTK;
using System.Collections.Generic;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics;
using System;
using osu.Framework.Graphics.Colour;
using System.Linq;

namespace osu.Framework.Graphics.Shapes
{
    public partial class ArbitraryShape
    {
        private class ArbitraryShapeDrawNode : DrawNode
        {
            protected new ArbitraryShape Source => (ArbitraryShape)base.Source;

            private ulong verticeInvalidationId;
            private readonly List<Vector2> vertices = new List<Vector2>();

            private Quad screenSpaceQuad;
            private Vector2 drawSize;

            private IShader shader;
            private IShader roundedShader;
            private Texture texture;

            private FillRule fillRule;

            public ArbitraryShapeDrawNode(ArbitraryShape source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                if (verticeInvalidationId != Source.verticeInvalidationId)
                {
                    vertices.Clear();
                    vertices.AddRange(Source.vertices.Select(x => x - Source.vertexBounds.TopLeft));

                    verticeInvalidationId = Source.verticeInvalidationId;
                }

                screenSpaceQuad = Source.ScreenSpaceDrawQuad;
                drawSize = Source.DrawSize;

                texture = Source.Texture;
                shader = Source.TextureShader;
                roundedShader = Source.RoundedTextureShader;
                fillRule = Source.fillRule;
            }

            public override void Draw(IRenderer renderer)
            {
                if (vertices.Count < 3)
                    return;

                base.Draw(renderer);

                if (fillRule == FillRule.NonZero)
                    drawNonZero(renderer);
                else if (fillRule == FillRule.EvenOdd)
                    drawEvenOdd(renderer);
                else
                    drawFan(renderer);
            }

            private void drawNonZero(IRenderer renderer)
            {
                var activeShader = renderer.IsMaskingActive ? roundedShader : shader;
                shader.Bind();

                // clear stencil where we will draw
                renderer.PushStencilInfo(new StencilInfo(stencilTest: true, DepthStencilFunction.Always, 0));
                renderer.DrawQuad(renderer.WhitePixel, screenSpaceQuad, Color4.Transparent);
                renderer.PopStencilInfo();

                // we are counting the amount of times a clockwise and counterclockwise triangle was
                // drawn over a pixel
                int winding = 0;
                void setWinding()
                {
                    if (winding < 0)
                    {
                        renderer.PushStencilInfo(new StencilInfo(
                            true, DepthStencilFunction.Always, 1,
                            passed: StencilOperation.DecreaseWrap
                        ));
                    }
                    else
                    {
                        renderer.PushStencilInfo(new StencilInfo(
                            true, DepthStencilFunction.Always, 1,
                            passed: StencilOperation.IncreaseWrap
                        ));
                    }
                }
                renderer.PushLocalMatrix(DrawInfo.Matrix);
                float lastAngle = angleBetween(vertices[0], vertices[1]);
                for (int i = 2; i < vertices.Count; i++)
                {
                    float angle = angleBetween(vertices[0], vertices[i]);
                    int nextWinding = getWinding(lastAngle, angle);
                    if (nextWinding != winding)
                    {
                        if (winding != 0)
                            renderer.PopStencilInfo();
                        winding = nextWinding;
                        setWinding();
                    }
                    lastAngle = angle;

                    renderer.DrawTriangle(renderer.WhitePixel, new Primitives.Triangle(
                        vertices[0],
                        vertices[i - 1],
                        vertices[i]
                    ), Color4.Transparent);
                }

                renderer.PopStencilInfo();
                renderer.PopLocalMatrix();

                shader.Unbind();
                activeShader.Bind();

                // draw over the stencil
                renderer.PushStencilInfo(new StencilInfo(stencilTest: true, DepthStencilFunction.NotEqual, 0));
                renderer.DrawQuad(texture, screenSpaceQuad, DrawColourInfo.Colour);
                renderer.PopStencilInfo();

                activeShader.Unbind();
            }

            private void drawEvenOdd(IRenderer renderer)
            {
                var activeShader = renderer.IsMaskingActive ? roundedShader : shader;
                shader.Bind();

                // clear stencil where we will draw
                renderer.PushStencilInfo(new StencilInfo(stencilTest: true, DepthStencilFunction.Always, 0));
                renderer.DrawQuad(renderer.WhitePixel, screenSpaceQuad, Color4.Transparent);
                renderer.PopStencilInfo();

                // even-odd will filp the stencil value each time we draw over the pixel
                renderer.PushStencilInfo(new StencilInfo(
                    true, DepthStencilFunction.Always, 1,
                    passed: StencilOperation.Invert
                ));
                renderer.PushLocalMatrix(DrawInfo.Matrix);
                for (int i = 2; i < vertices.Count; i++)
                {
                    renderer.DrawTriangle(renderer.WhitePixel, new Primitives.Triangle(
                        vertices[0],
                        vertices[i - 1],
                        vertices[i]
                    ), Color4.Transparent);
                }

                renderer.PopLocalMatrix();
                renderer.PopStencilInfo();

                shader.Unbind();
                activeShader.Bind();

                // draw over the stencil
                renderer.PushStencilInfo(new StencilInfo(stencilTest: true, DepthStencilFunction.NotEqual, 0));
                renderer.DrawQuad(texture, screenSpaceQuad, DrawColourInfo.Colour);
                renderer.PopStencilInfo();

                activeShader.Unbind();
            }

            private Vector2 relativePosition(Vector2 localPos) => Vector2.Divide(localPos, drawSize);

            private Color4 colourAt(Vector2 localPos) => DrawColourInfo.Colour.HasSingleColour
                ? ((SRGBColour)DrawColourInfo.Colour).Linear
                : DrawColourInfo.Colour.Interpolate(relativePosition(localPos)).Linear;

            private void drawFan(IRenderer renderer)
            {
                var activeShader = renderer.IsMaskingActive ? roundedShader : shader;
                activeShader.Bind();

                renderer.PushLocalMatrix(DrawInfo.Matrix);
                texture.Bind();
                var vertexAction = renderer.DefaultQuadBatch.AddAction;
                RectangleF texRect = texture.GetTextureRect();
                Vector4 texVec = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom);
                for (int i = 2; i < vertices.Count; i++)
                {
                    var v0 = vertices[0];
                    var v1 = vertices[i - 1];
                    var v2 = vertices[i];
                    vertexAction(new TexturedVertex2D
                    {
                        Position = v0,
                        TexturePosition = texRect.Location + texRect.Size * relativePosition(v0),
                        TextureRect = texVec,
                        Colour = colourAt(v0)
                    });
                    vertexAction(new TexturedVertex2D
                    {
                        Position = v1,
                        TexturePosition = texRect.Location + texRect.Size * relativePosition(v1),
                        TextureRect = texVec,
                        Colour = colourAt(v1)
                    });
                    vertexAction(new TexturedVertex2D
                    {
                        Position = v1,
                        TexturePosition = texRect.Location + texRect.Size * relativePosition(v1),
                        TextureRect = texVec,
                        Colour = colourAt(v1)
                    });
                    vertexAction(new TexturedVertex2D
                    {
                        Position = v2,
                        TexturePosition = texRect.Location + texRect.Size * relativePosition(v2),
                        TextureRect = texVec,
                        Colour = colourAt(v2)
                    });
                }

                renderer.PopLocalMatrix();
                activeShader.Unbind();
            }
        }
    }
}
