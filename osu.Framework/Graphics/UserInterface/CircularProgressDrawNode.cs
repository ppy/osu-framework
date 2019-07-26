// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using osuTK;
using System;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics;
using osu.Framework.Extensions.MatrixExtensions;
using osu.Framework.Graphics.OpenGL.Vertices;

namespace osu.Framework.Graphics.UserInterface
{
    public class CircularProgressDrawNode : TexturedShaderDrawNode
    {
        public const int MAX_RES = 24;

        protected new CircularProgress Source => (CircularProgress)base.Source;

        private float angle;
        private float innerRadius = 1;

        private Vector2 drawSize;
        private Texture texture;

        public CircularProgressDrawNode(CircularProgress source)
            : base(source)
        {
        }

        public override void ApplyState()
        {
            base.ApplyState();

            texture = Source.Texture;
            drawSize = Source.DrawSize;
            angle = (float)Source.Current.Value * MathHelper.TwoPi;
            innerRadius = Source.InnerRadius;
        }

        // We add 2 to the size param to account for the first triangle needing every vertex passed, subsequent triangles use the last two vertices of the previous triangle.
        // MAX_RES refers to a half-circle, so it is multiplied by 2 for the full circle and by 2 again to account for each circle part requiring two vertices.
        private readonly LinearBatch<TexturedVertex2D> halfCircleBatch = new LinearBatch<TexturedVertex2D>(MAX_RES * 4 + 2, 1, PrimitiveType.TriangleStrip);

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
            const float step = MathHelper.Pi / MAX_RES;

            float dir = Math.Sign(angle);

            int amountPoints = (int)Math.Ceiling(Math.Abs(angle) / step);

            Matrix3 transformationMatrix = DrawInfo.Matrix;
            MatrixExtensions.ScaleFromLeft(ref transformationMatrix, drawSize);

            Vector2 current = origin + pointOnCircle(start_angle) * 0.5f;
            Color4 currentColour = colourAt(current);
            current = Vector2Extensions.Transform(current, transformationMatrix);

            Vector2 screenOrigin = Vector2Extensions.Transform(origin, transformationMatrix);
            Color4 originColour = colourAt(origin);

            // Offset by 0.5 pixels inwards to ensure we never sample texels outside the bounds
            RectangleF texRect = texture.GetTextureRect(new RectangleF(0.5f, 0.5f, texture.Width - 1, texture.Height - 1));

            float prevOffset = dir >= 0 ? 0 : 1;

            // First center point
            halfCircleBatch.Add(new TexturedVertex2D
            {
                Position = Vector2.Lerp(current, screenOrigin, innerRadius),
                TexturePosition = new Vector2(dir >= 0 ? texRect.Left : texRect.Right, texRect.Top),
                Colour = originColour
            });

            // First outer point.
            halfCircleBatch.Add(new TexturedVertex2D
            {
                Position = new Vector2(current.X, current.Y),
                TexturePosition = new Vector2(dir >= 0 ? texRect.Left : texRect.Right, texRect.Bottom),
                Colour = currentColour
            });

            for (int i = 1; i <= amountPoints; i++)
            {
                // Clamps the angle so we don't overshoot.
                // dir is used so negative angles result in negative angularOffset.
                float angularOffset = dir * Math.Min(i * step, dir * angle);
                float normalisedOffset = angularOffset / MathHelper.TwoPi;

                if (dir < 0)
                {
                    normalisedOffset += 1.0f;
                }

                // Update `current`
                current = origin + pointOnCircle(start_angle + angularOffset) * 0.5f;
                currentColour = colourAt(current);
                current = Vector2Extensions.Transform(current, transformationMatrix);

                // current center point
                halfCircleBatch.Add(new TexturedVertex2D
                {
                    Position = Vector2.Lerp(current, screenOrigin, innerRadius),
                    TexturePosition = new Vector2(texRect.Left + (normalisedOffset + prevOffset) / 2 * texRect.Width, texRect.Top),
                    Colour = originColour
                });

                // current outer point
                halfCircleBatch.Add(new TexturedVertex2D
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

            if (texture?.Available != true)
                return;

            Shader.Bind();

            texture.TextureGL.WrapMode = TextureWrapMode.ClampToEdge;
            texture.TextureGL.Bind();

            updateVertexBuffer();

            Shader.Unbind();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            halfCircleBatch.Dispose();
        }
    }
}
