// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL;
using OpenTK;
using System;
using osu.Framework.Graphics.OpenGL.Buffers;
using System.Collections.Generic;
using osu.Framework.Graphics.Batches;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Sprites
{
    public class PathDrawNodeSharedData
    {
        public LinearBatch<TexturedVertex3D> HalfCircleBatch = new LinearBatch<TexturedVertex3D>(PathDrawNode.MAXRES * 100 * 3, 10, PrimitiveType.Triangles);
        public QuadBatch<TexturedVertex3D> QuadBatch = new QuadBatch<TexturedVertex3D>(200, 10);
    }

    public class PathDrawNode : DrawNode
    {
        public const int MAXRES = 24;
        public List<Line> Segments = new List<Line>();

        public float Width;
        public Texture Texture;

        public Shader TextureShader;
        public Shader RoundedTextureShader;

        public PathDrawNodeSharedData Shared;

        private bool NeedsRoundedShader => GLWrapper.IsMaskingActive;


        private Vector2 pointOnCircle(float angle)
        {
            return new Vector2((float)(Math.Sin(angle)), -(float)(Math.Cos(angle)));
        }

        private void addLineCap(Vector2 origin, float theta, float thetaDiff)
        {
            float step = MathHelper.Pi / MAXRES;

            float dir = Math.Sign(thetaDiff);
            thetaDiff = dir * thetaDiff;

            int amountPoints = (int)Math.Ceiling(thetaDiff / step);

            if (dir < 0)
                theta += MathHelper.Pi;

            Vector2 current = (origin + pointOnCircle(theta) * Width) * DrawInfo.Matrix;
            Vector2 screenOrigin = origin * DrawInfo.Matrix;

            for (int i = 1; i <= amountPoints; i++)
            {
                // Center point
                Shared.HalfCircleBatch.Add(new TexturedVertex3D()
                {
                    Position = new Vector3(screenOrigin.X, screenOrigin.Y, 1),
                    TexturePosition = new Vector2(1 - 1 / Texture.Width, 0),
                    Colour = DrawInfo.Colour.Colour.Linear
                });

                // First outer point
                Shared.HalfCircleBatch.Add(new TexturedVertex3D()
                {
                    Position = new Vector3(current.X, current.Y, 0),
                    TexturePosition = new Vector2(0, 0),
                    Colour = DrawInfo.Colour.Colour.Linear
                });

                float angularOffset = Math.Min(i * step, thetaDiff);
                current = (origin + pointOnCircle(theta + dir * angularOffset) * Width) * DrawInfo.Matrix;

                // Second outer point
                Shared.HalfCircleBatch.Add(new TexturedVertex3D()
                {
                    Position = new Vector3(current.X, current.Y, 0),
                    TexturePosition = new Vector2(0, 0),
                    Colour = DrawInfo.Colour.Colour.Linear
                });
            }
        }
        private void addLineQuads(Line line)
        {
            Vector2 ortho = line.OrthogonalDirection;
            Line lineLeft = new Line((line.StartPoint + ortho * Width) * DrawInfo.Matrix, (line.EndPoint + ortho * Width) * DrawInfo.Matrix);
            Line lineRight = new Line((line.StartPoint - ortho * Width) * DrawInfo.Matrix, (line.EndPoint - ortho * Width) * DrawInfo.Matrix);
            line = new Line(line.StartPoint * DrawInfo.Matrix, line.EndPoint * DrawInfo.Matrix);

            Shared.QuadBatch.Add(new TexturedVertex3D()
            {
                Position = new Vector3(lineRight.EndPoint.X, lineRight.EndPoint.Y, 0),
                TexturePosition = new Vector2(0, 0),
                Colour = DrawInfo.Colour.Colour.Linear
            });
            Shared.QuadBatch.Add(new TexturedVertex3D()
            {
                Position = new Vector3(lineRight.StartPoint.X, lineRight.StartPoint.Y, 0),
                TexturePosition = new Vector2(0, 0),
                Colour = DrawInfo.Colour.Colour.Linear
            });

            // Each "quad" of the slider is actually rendered as 2 quads, being split in half along the approximating line.
            // On this line the depth is 1 instead of 0, which is done properly handle self-overlap using the depth buffer.
            // Thus the middle vertices need to be added twice (once for each quad).
            Vector3 firstMiddlePoint = new Vector3(line.StartPoint.X, line.StartPoint.Y, 1);
            Vector3 secondMiddlePoint = new Vector3(line.EndPoint.X, line.EndPoint.Y, 1);

            for (int i = 0; i < 2; ++i)
            {
                Shared.QuadBatch.Add(new TexturedVertex3D()
                {
                    Position = firstMiddlePoint,
                    TexturePosition = new Vector2(1 - 1 / Texture.Width, 0),
                    Colour = DrawInfo.Colour.Colour.Linear
                });
                Shared.QuadBatch.Add(new TexturedVertex3D()
                {
                    Position = secondMiddlePoint,
                    TexturePosition = new Vector2(1 - 1 / Texture.Width, 0),
                    Colour = DrawInfo.Colour.Colour.Linear
                });
            }

            Shared.QuadBatch.Add(new TexturedVertex3D()
            {
                Position = new Vector3(lineLeft.EndPoint.X, lineLeft.EndPoint.Y, 0),
                TexturePosition = new Vector2(0, 0),
                Colour = DrawInfo.Colour.Colour.Linear
            });
            Shared.QuadBatch.Add(new TexturedVertex3D()
            {
                Position = new Vector3(lineLeft.StartPoint.X, lineLeft.StartPoint.Y, 0),
                TexturePosition = new Vector2(0, 0),
                Colour = DrawInfo.Colour.Colour.Linear
            });
        }

        private void updateVertexBuffer()
        {
            Line line = Segments[0];
            float theta = line.Theta;
            addLineCap(line.StartPoint, theta + MathHelper.Pi, MathHelper.Pi);

            for (int i = 1; i < Segments.Count; ++i)
            {
                Line nextLine = Segments[i];
                float nextTheta = nextLine.Theta;
                addLineCap(line.EndPoint, theta, nextTheta - theta);

                line = nextLine;
                theta = nextTheta;
            }

            addLineCap(line.EndPoint, theta, MathHelper.Pi);


            for (int i = 0; i < Segments.Count; ++i)
                addLineQuads(Segments[i]);
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture == null || Texture.IsDisposed || Segments.Count == 0)
                return;

            Shader shader = NeedsRoundedShader ? RoundedTextureShader : TextureShader;

            shader.Bind();

            Texture.TextureGL.WrapMode = TextureWrapMode.ClampToEdge;
            Texture.TextureGL.Bind();

            updateVertexBuffer();

            shader.Unbind();
        }
    }
}
