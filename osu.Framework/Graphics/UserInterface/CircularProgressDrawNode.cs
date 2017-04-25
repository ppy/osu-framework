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
        private static Vector2 origin = new Vector2(0.5f, 0.5f);

        public Vector2 DrawSize;
        public Texture Texture;

        public Shader TextureShader;
        public Shader RoundedTextureShader;

        public CircularProgressDrawNodeSharedData Shared;

        private bool needsRoundedShader => GLWrapper.IsMaskingActive;

        private Vector2 pointOnCircle(float angle) => new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));

        private Color4 colourAt(Vector2 localPos) => DrawInfo.Colour.HasSingleColour
            ? DrawInfo.Colour.Colour.Linear
            : DrawInfo.Colour.Interpolate(localPos).Linear;

        private void updateVertexBuffer()
        {
            const float start_angle = 0;
            const float step = MathHelper.Pi / MAXRES;

            float dir = Math.Sign(Angle);

            int amountPoints = (int)Math.Ceiling(Math.Abs(Angle) / step);

            Matrix3 transformationMatrix = Matrix3.CreateScale(DrawSize.X, DrawSize.Y, 1.0f) * DrawInfo.Matrix;

            Vector2 current = origin + pointOnCircle(start_angle) * 0.5f;
            Color4 currentColour = colourAt(current);
            current *= transformationMatrix;

            Vector2 screenOrigin = origin * DrawSize * DrawInfo.Matrix;
            Color4 originColour = colourAt(origin);

            for (int i = 1; i <= amountPoints; i++)
            {
                // Center point
                Shared.HalfCircleBatch.Add(new TexturedVertex2D
                {
                    Position = new Vector2(screenOrigin.X, screenOrigin.Y),
                    TexturePosition = new Vector2(1 - 1 / Texture.Width, 0),
                    Colour = originColour
                });

                // First outer point
                Shared.HalfCircleBatch.Add(new TexturedVertex2D
                {
                    Position = new Vector2(current.X, current.Y),
                    TexturePosition = new Vector2(0, 0),
                    Colour = currentColour
                });

                float angularOffset = dir * Math.Min(i * step, dir * Angle);
                current = origin + pointOnCircle(start_angle + angularOffset) * 0.5f;
                currentColour = colourAt(current);
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
