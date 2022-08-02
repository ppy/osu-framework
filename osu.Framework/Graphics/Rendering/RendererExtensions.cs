// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Buffers;
using osu.Framework.Graphics.OpenGL.Vertices;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osu.Framework.Statistics;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Rendering
{
    public static class RendererExtensions
    {
        public static void DrawTriangle(this IRenderer renderer, Texture texture, Triangle vertexTriangle, ColourInfo drawColour, RectangleF? textureRect = null,
                                        Action<TexturedVertex2D>? vertexAction = null, Vector2? inflationPercentage = null, RectangleF? textureCoords = null)
        {
            if (!texture.Available)
                throw new ObjectDisposedException(texture.ToString(), "Can not draw a triangle with a disposed texture.");

            if (!renderer.BindTexture(texture))
                return;

            RectangleF texRect = texture.GetTextureRect(textureRect);
            Vector2 inflationAmount = inflationPercentage.HasValue ? new Vector2(inflationPercentage.Value.X * texRect.Width, inflationPercentage.Value.Y * texRect.Height) : Vector2.Zero;

            // If clamp to edge is active, allow the texture coordinates to penetrate by half the repeated atlas margin width
            if (renderer.CurrentWrapModeS == WrapMode.ClampToEdge || renderer.CurrentWrapModeT == WrapMode.ClampToEdge)
            {
                Vector2 inflationVector = Vector2.Zero;

                const int mipmap_padding_requirement = (1 << IRenderer.MAX_MIPMAP_LEVELS) / 2;

                if (renderer.CurrentWrapModeS == WrapMode.ClampToEdge)
                    inflationVector.X = mipmap_padding_requirement / (float)texture.Width;
                if (renderer.CurrentWrapModeT == WrapMode.ClampToEdge)
                    inflationVector.Y = mipmap_padding_requirement / (float)texture.Height;
                texRect = texRect.Inflate(inflationVector);
            }

            RectangleF coordRect = texture.GetTextureRect(textureCoords ?? textureRect);
            RectangleF inflatedCoordRect = coordRect.Inflate(inflationAmount);

            vertexAction ??= GLWrapper.DefaultQuadBatch.AddAction;

            // We split the triangle into two, such that we can obtain smooth edges with our
            // texture coordinate trick. We might want to revert this to drawing a single
            // triangle in case we ever need proper texturing, or if the additional vertices
            // end up becoming an overhead (unlikely).
            SRGBColour topColour = (drawColour.TopLeft + drawColour.TopRight) / 2;
            SRGBColour bottomColour = (drawColour.BottomLeft + drawColour.BottomRight) / 2;

            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P0,
                TexturePosition = new Vector2((inflatedCoordRect.Left + inflatedCoordRect.Right) / 2, inflatedCoordRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = topColour.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P1,
                TexturePosition = new Vector2(inflatedCoordRect.Left, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = drawColour.BottomLeft.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = (vertexTriangle.P1 + vertexTriangle.P2) / 2,
                TexturePosition = new Vector2((inflatedCoordRect.Left + inflatedCoordRect.Right) / 2, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = bottomColour.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexTriangle.P2,
                TexturePosition = new Vector2(inflatedCoordRect.Right, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = inflationAmount,
                Colour = drawColour.BottomRight.Linear,
            });

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexTriangle.Area);
        }

        public static void DrawQuad(this IRenderer renderer, Texture texture, Quad vertexQuad, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D>? vertexAction = null,
                                    Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null, RectangleF? textureCoords = null)
        {
            if (!texture.Available)
                throw new ObjectDisposedException(texture.ToString(), "Can not draw a quad with a disposed texture.");

            if (!renderer.BindTexture(texture))
                return;

            RectangleF texRect = texture.GetTextureRect(textureRect);
            Vector2 inflationAmount = inflationPercentage.HasValue ? new Vector2(inflationPercentage.Value.X * texRect.Width, inflationPercentage.Value.Y * texRect.Height) : Vector2.Zero;

            // If clamp to edge is active, allow the texture coordinates to penetrate by half the repeated atlas margin width
            if (renderer.CurrentWrapModeS == WrapMode.ClampToEdge || renderer.CurrentWrapModeT == WrapMode.ClampToEdge)
            {
                Vector2 inflationVector = Vector2.Zero;

                const int mipmap_padding_requirement = (1 << IRenderer.MAX_MIPMAP_LEVELS) / 2;

                if (renderer.CurrentWrapModeS == WrapMode.ClampToEdge)
                    inflationVector.X = mipmap_padding_requirement / (float)texture.Width;
                if (renderer.CurrentWrapModeT == WrapMode.ClampToEdge)
                    inflationVector.Y = mipmap_padding_requirement / (float)texture.Height;
                texRect = texRect.Inflate(inflationVector);
            }

            RectangleF coordRect = texture.GetTextureRect(textureCoords ?? textureRect);
            RectangleF inflatedCoordRect = coordRect.Inflate(inflationAmount);
            Vector2 blendRange = blendRangeOverride ?? inflationAmount;

            vertexAction ??= GLWrapper.DefaultQuadBatch.AddAction;

            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.BottomLeft,
                TexturePosition = new Vector2(inflatedCoordRect.Left, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.BottomLeft.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.BottomRight,
                TexturePosition = new Vector2(inflatedCoordRect.Right, inflatedCoordRect.Bottom),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.BottomRight.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.TopRight,
                TexturePosition = new Vector2(inflatedCoordRect.Right, inflatedCoordRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.TopRight.Linear,
            });
            vertexAction(new TexturedVertex2D
            {
                Position = vertexQuad.TopLeft,
                TexturePosition = new Vector2(inflatedCoordRect.Left, inflatedCoordRect.Top),
                TextureRect = new Vector4(texRect.Left, texRect.Top, texRect.Right, texRect.Bottom),
                BlendRange = blendRange,
                Colour = drawColour.TopLeft.Linear,
            });

            FrameStatistics.Add(StatisticsCounterType.Pixels, (long)vertexQuad.Area);
        }

        /// <summary>
        /// Clips a <see cref="IConvexPolygon"/> to the current masking area and draws the resulting triangles to the screen using the specified texture.
        /// </summary>
        /// <param name="renderer">The renderer to draw the texture with.</param>
        /// <param name="polygon">The polygon to draw.</param>
        /// <param name="texture">The texture to fill the triangle with.</param>
        /// <param name="textureRect">The texture rectangle.</param>
        /// <param name="drawColour">The vertex colour.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that <paramref name="textureRect"/> should be inflated.</param>
        /// <param name="textureCoords">The texture coordinates of the polygon's vertices (translated from the corresponding quad's rectangle).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawClipped<T>(this IRenderer renderer, ref T polygon, Texture texture, ColourInfo drawColour, RectangleF? textureRect = null, Action<TexturedVertex2D>? vertexAction = null,
                                          Vector2? inflationPercentage = null, RectangleF? textureCoords = null)
            where T : IConvexPolygon
        {
            var maskingQuad = GLWrapper.CurrentMaskingInfo.ConservativeScreenSpaceQuad;

            var clipper = new ConvexPolygonClipper<Quad, T>(ref maskingQuad, ref polygon);
            Span<Vector2> buffer = stackalloc Vector2[clipper.GetClipBufferSize()];
            Span<Vector2> clippedRegion = clipper.Clip(buffer);

            for (int i = 2; i < clippedRegion.Length; i++)
                renderer.DrawTriangle(texture, new Triangle(clippedRegion[0], clippedRegion[i - 1], clippedRegion[i]), drawColour, textureRect, vertexAction, inflationPercentage, textureCoords);
        }

        /// <summary>
        /// Draws a <see cref="FrameBuffer"/> to the screen.
        /// </summary>
        /// <param name="renderer">The renderer to draw the framebuffer with.</param>
        /// <param name="frameBuffer">The <see cref="FrameBuffer"/> to draw.</param>
        /// <param name="vertexQuad">The destination vertices.</param>
        /// <param name="drawColour">The colour to draw the <paramref name="frameBuffer"/> with.</param>
        /// <param name="vertexAction">An action that adds vertices to a <see cref="VertexBatch{T}"/>.</param>
        /// <param name="inflationPercentage">The percentage amount that the frame buffer area  should be inflated.</param>
        /// <param name="blendRangeOverride">The range over which the edges of the frame buffer should be blended.</param>
        public static void DrawFrameBuffer(this IRenderer renderer, IFrameBuffer frameBuffer, Quad vertexQuad, ColourInfo drawColour, Action<TexturedVertex2D>? vertexAction = null,
                                           Vector2? inflationPercentage = null, Vector2? blendRangeOverride = null)
        {
            // The strange Y coordinate and Height are a result of OpenGL coordinate systems having Y grow upwards and not downwards.
            RectangleF textureRect = new RectangleF(0, frameBuffer.Texture.Height, frameBuffer.Texture.Width, -frameBuffer.Texture.Height);

            renderer.DrawQuad(frameBuffer.Texture, vertexQuad, drawColour, textureRect, vertexAction, inflationPercentage, blendRangeOverride);
        }
    }
}
