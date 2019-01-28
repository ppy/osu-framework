﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL;
using osuTK;
using System;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics;
using osu.Framework.Extensions.MatrixExtensions;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.UserInterface
{
    public class CircularProgressDrawNodeSharedData
    {
        // We add 2 to the size param to account for the first triangle needing every vertex passed, subsequent triangles use the last two vertices of the previous triangle. MAXRES is being multiplied by 2 to account for each circle part needing 2 triangles
        // Otherwise overflowing the batch will result in wrong grouping of vertices into primitives.
        public LinearBatch<TexturedVertex2D> HalfCircleBatch = new LinearBatch<TexturedVertex2D>(CircularProgressDrawNode.MAXRES * 100 * 2 + 2, 10, PrimitiveType.TriangleStrip);
    }

    public class CircularProgressDrawNode : DrawNode
    {
        public const int MAXRES = 24;
        public float Angle;
        public float InnerRadius = 1;

        public Vector2 DrawSize;
        public Texture Texture;

        public Shader TextureShader;
        public Shader RoundedTextureShader;

        public CircularProgressDrawNodeSharedData Shared;

        private bool needsRoundedShader => GLWrapper.IsMaskingActive;

        private Vector2 pointOnCircle(float angle) => new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));
        private float angleToUnitInterval(float angle) => angle / MathHelper.TwoPi + (angle >= 0 ? 0 : 1);

        // Gets colour at the localPos position in the unit square of our Colour gradient box.
        private Color4 colourAt(Vector2 localPos) => DrawColourInfo.Colour.HasSingleColour
            ? (Color4)DrawColourInfo.Colour
            : DrawColourInfo.Colour.Interpolate(localPos).Linear;

        private static readonly Vector2 origin = new Vector2(0.5f, 0.5f);
        private void updateVertexBuffer()
        {
            const float start_angle = 0;
            const float step = MathHelper.Pi / MAXRES;

            float dir = Math.Sign(Angle);

            int amountPoints = (int)Math.Ceiling(Math.Abs(Angle) / step);

            Matrix3 transformationMatrix = DrawInfo.Matrix;
            MatrixExtensions.ScaleFromLeft(ref transformationMatrix, DrawSize);

            Vector2 current = origin + pointOnCircle(start_angle) * 0.5f;
            Color4 currentColour = colourAt(current);
            current = Vector2Extensions.Transform(current, transformationMatrix);

            Vector2 screenOrigin = Vector2Extensions.Transform(origin, transformationMatrix);
            Color4 originColour = colourAt(origin);

            // Offset by 0.5 pixels inwards to ensure we never sample texels outside the bounds
            RectangleF texRect = Texture.GetTextureRect(new RectangleF(0.5f, 0.5f, Texture.Width - 1, Texture.Height - 1));

            float prevOffset = dir >= 0 ? 0 : 1;

            // First center point
            Shared.HalfCircleBatch.Add(new TexturedVertex2D
            {
                Position = Vector2.Lerp(current, screenOrigin, InnerRadius),
                TexturePosition = new Vector2(dir >= 0 ? texRect.Left : texRect.Right, texRect.Top),
                Colour = originColour
            });

            // First outer point.
            Shared.HalfCircleBatch.Add(new TexturedVertex2D
            {
                Position = new Vector2(current.X, current.Y),
                TexturePosition = new Vector2(dir >= 0 ? texRect.Left : texRect.Right, texRect.Bottom),
                Colour = currentColour
            });

            for (int i = 1; i <= amountPoints; i++)
            {
                // Clamps the angle so we don't overshoot.
                // dir is used so negative angles result in negative angularOffset.
                float angularOffset = dir * Math.Min(i * step, dir * Angle);
                float normalisedOffset = angularOffset / MathHelper.TwoPi;
                if (dir < 0) {
                    normalisedOffset += 1.0f;
                }

                // Update `current`
                current = origin + pointOnCircle(start_angle + angularOffset) * 0.5f;
                currentColour = colourAt(current);
                current = Vector2Extensions.Transform(current, transformationMatrix);

                // current center point
                Shared.HalfCircleBatch.Add(new TexturedVertex2D
                {
                    Position = Vector2.Lerp(current, screenOrigin, InnerRadius),
                    TexturePosition = new Vector2(texRect.Left + (normalisedOffset + prevOffset) / 2 * texRect.Width, texRect.Top),
                    Colour = originColour
                });

                // current outer point
                Shared.HalfCircleBatch.Add(new TexturedVertex2D
                {
                    Position = new Vector2(current.X, current.Y),
                    TexturePosition = new Vector2(texRect.Left + normalisedOffset * texRect.Width, texRect.Bottom),
                    Colour = currentColour
                });

                prevOffset = normalisedOffset;
            }
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture?.Available != true)
                return;

            Shader shader = needsRoundedShader ? RoundedTextureShader : TextureShader;

            shader.Bind();

            Texture.TextureGL.WrapMode = TextureWrapMode.ClampToEdge;
            Texture.TextureGL.Bind();

            updateVertexBuffer();

            shader.Unbind();
        }
    }
}
