// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Textures;
using osuTK;
using System;
using osu.Framework.Graphics.Primitives;
using osuTK.Graphics;
using osu.Framework.Extensions.MatrixExtensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;

namespace osu.Framework.Graphics.UserInterface
{
    public class CircularProgressDrawNode : TexturedShaderDrawNode
    {
        private const float arc_tolerance = 0.1f;

        private const float two_pi = MathF.PI * 2;

        protected new CircularProgress Source => (CircularProgress)base.Source;

        private IVertexBatch<TexturedVertex2D> halfCircleBatch;

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
            angle = (float)Source.Current.Value * two_pi;
            innerRadius = Source.InnerRadius;
        }

        private Vector2 pointOnCircle(float angle) => new Vector2(MathF.Sin(angle), -MathF.Cos(angle));
        private float angleToUnitInterval(float angle) => angle / two_pi + (angle >= 0 ? 0 : 1);

        // Gets colour at the localPos position in the unit square of our Colour gradient box.
        private Color4 colourAt(Vector2 localPos) => DrawColourInfo.Colour.HasSingleColour
            ? DrawColourInfo.Colour.TopLeft.Linear
            : DrawColourInfo.Colour.Interpolate(localPos).Linear;

        private static readonly Vector2 origin = new Vector2(0.5f, 0.5f);

        private void updateVertexBuffer(IRenderer renderer)
        {
            const float start_angle = 0;

            float dir = Math.Sign(angle);
            float radius = Math.Max(drawSize.X, drawSize.Y);

            // The amount of points are selected such that discrete curvature is smaller than the provided tolerance.
            // The exact angle required to meet the tolerance is: 2 * Math.Acos(1 - TOLERANCE / r)
            // The special case is for extremely small circles where the radius is smaller than the tolerance.
            int amountPoints = 2 * radius <= arc_tolerance ? 2 : Math.Max(2, (int)Math.Ceiling(Math.PI / Math.Acos(1 - arc_tolerance / radius)));

            if (halfCircleBatch == null || halfCircleBatch.Size < amountPoints * 2)
            {
                halfCircleBatch?.Dispose();

                // Amount of points is multiplied by 2 to account for each part requiring two vertices.
                halfCircleBatch = renderer.CreateLinearBatch<TexturedVertex2D>(amountPoints * 2, 1, PrimitiveTopology.TriangleStrip);
            }

            Matrix3 transformationMatrix = DrawInfo.Matrix;
            MatrixExtensions.ScaleFromLeft(ref transformationMatrix, drawSize);
            renderer.PushLocalMatrix(transformationMatrix);

            Vector2 current = origin + pointOnCircle(start_angle) * 0.5f;
            Color4 currentColour = colourAt(current);

            Color4 originColour = colourAt(origin);

            // Offset by 0.5 pixels inwards to ensure we never sample texels outside the bounds
            RectangleF texRect = texture.GetTextureRect(new RectangleF(0.5f, 0.5f, texture.Width - 1, texture.Height - 1));

            float prevOffset = dir >= 0 ? 0 : 1;

            // First center point
            halfCircleBatch.Add(new TexturedVertex2D
            {
                Position = Vector2.Lerp(current, origin, innerRadius),
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

            for (int i = 1; i < amountPoints; i++)
            {
                float fract = (float)i / (amountPoints - 1);

                // Clamps the angle so we don't overshoot.
                // dir is used so negative angles result in negative angularOffset.
                float angularOffset = Math.Min(fract * two_pi, dir * angle);
                float normalisedOffset = angularOffset / two_pi;

                if (dir < 0)
                    normalisedOffset += 1.0f;

                // Update `current`
                current = origin + pointOnCircle(start_angle + angularOffset) * 0.5f;
                currentColour = colourAt(current);

                // current center point
                halfCircleBatch.Add(new TexturedVertex2D
                {
                    Position = Vector2.Lerp(current, origin, innerRadius),
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

            renderer.PopLocalMatrix();
        }

        public override void Draw(IRenderer renderer)
        {
            base.Draw(renderer);

            if (texture?.Available != true)
                return;

            var shader = GetAppropriateShader(renderer);

            shader.Bind();

            texture.Bind();

            updateVertexBuffer(renderer);

            shader.Unbind();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            halfCircleBatch?.Dispose();
        }
    }
}
