// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL;
using OpenTK;
using System;
using osu.Framework.Graphics.Batches;
using OpenTK.Graphics;
using osu.Framework.Extensions.MatrixExtensions;

namespace osu.Framework.Graphics.UserInterface
{
    public class CircularProgressDrawNodeSharedData
    {
        // We multiply the size param by 3 such that the amount of vertices is a multiple of the amount of vertices
        // per primitive (triangles in this case). Otherwise overflowing the batch will result in wrong
        // grouping of vertices into primitives.
        public LinearBatch<TexturedVertex2D> HalfCircleBatch = new LinearBatch<TexturedVertex2D>(CircularProgressDrawNode.MAXRES * 100 * 3, 10, PrimitiveType.Triangles);
    }

    public class CircularProgressDrawNode : DrawNode
    {
        public const int MAXRES = 24;
        public float Angle;

        public Vector2 DrawSize;
        public Texture Texture;
        // If true, the circle will be coloured such that its angle/radius is mapped to the cartesian points on the colour gradient square.
        // In other words, you can define a certain angle to have a certain colour if you use GradientHorizontal,
        // or a certain distance from the centre to have a certain colour if you use GradientVertical (visually looking like a circular gradient).
        public bool UsePolarColourGradient { get; set; }

        public Shader TextureShader;
        public Shader RoundedTextureShader;

        public CircularProgressDrawNodeSharedData Shared;

        private bool needsRoundedShader => GLWrapper.IsMaskingActive;

        private Vector2 pointOnCircle(float angle) => new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));
        private float angleToUnitInterval(float angle) => angle / MathHelper.TwoPi + (angle >= 0 ? 0 : 1);

        // Gets colour at the localPos position in the unit square of our Colour gradient box.
        private Color4 linearColourAt(Vector2 localPos) => DrawInfo.Colour.HasSingleColour
            ? DrawInfo.Colour.Colour.Linear
            : DrawInfo.Colour.Interpolate(localPos).Linear;
        // Decide whether to use cartesian or polar coordinates to render our colours.
        private Color4 colourAt(Vector2 pos, float angle, float radius) => UsePolarColourGradient
            ? linearColourAt(new Vector2(angleToUnitInterval(angle), radius))
            : linearColourAt(pos);

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
            // Without this conditional, negative angles will have a discontinuity here,
            // where it tries to map using the positive end of the colour gradient rather than the positive,
            // as we start counting the angle downwards from 0, rather than from "-0".
            Color4 currentColour = dir > 0 ? colourAt(current, start_angle, 1) : colourAt(current, MathHelper.TwoPi, 1);
            current *= transformationMatrix;

            Vector2 screenOrigin = origin * transformationMatrix;

            for (int i = 1; i <= amountPoints; i++)
            {
                // Clamps the angle so we don't overshoot.
                // dir is used so negative angles result in negative angularOffset.
                float angularOffset = dir * Math.Min(i * step, dir * Angle);

                // Center point
                Color4 originColour = colourAt(origin, start_angle + angularOffset, 0);
                Shared.HalfCircleBatch.Add(new TexturedVertex2D
                {
                    Position = new Vector2(screenOrigin.X, screenOrigin.Y),
                    TexturePosition = new Vector2(1 - 1 / Texture.Width, 0),
                    Colour = originColour
                });

                // First outer point. (Note that this uses the same `current` as the second point of previous iteration)
                Shared.HalfCircleBatch.Add(new TexturedVertex2D
                {
                    Position = new Vector2(current.X, current.Y),
                    TexturePosition = new Vector2(0, 0),
                    Colour = currentColour
                });

                // Update `current`
                current = origin + pointOnCircle(start_angle + angularOffset) * 0.5f;
                currentColour = colourAt(current, start_angle + angularOffset, 1);
                current *= transformationMatrix;

                // Second outer point
                Shared.HalfCircleBatch.Add(new TexturedVertex2D
                {
                    Position = new Vector2(current.X, current.Y),
                    TexturePosition = new Vector2(0, 0),
                    Colour = currentColour
                });
            }
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture == null || Texture.IsDisposed)
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
