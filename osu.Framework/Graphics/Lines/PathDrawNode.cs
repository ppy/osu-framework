// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using OpenTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL;
using OpenTK;
using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Vertices;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.Lines
{
    public class PathDrawNodeSharedData
    {
        // We multiply the size param by 3 such that the amount of vertices is a multiple of the amount of vertices
        // per primitive (triangles in this case). Otherwise overflowing the batch will result in wrong
        // grouping of vertices into primitives.
        public LinearBatch<TexturedVertex3D> HalfCircleBatch = new LinearBatch<TexturedVertex3D>(PathDrawNode.MAXRES * 100 * 3, 10, PrimitiveType.Triangles);
        public QuadBatch<TexturedVertex3D> QuadBatch = new QuadBatch<TexturedVertex3D>(200, 10);
    }

    public class PathDrawNode : DrawNode
    {
        public const int MAXRES = 24;
        public List<Line> Segments;

        public Vector2 DrawSize;
        public float Width;
        public Texture Texture;

        public Shader TextureShader;
        public Shader RoundedTextureShader;

        public PathDrawNodeSharedData Shared;

        private bool needsRoundedShader => GLWrapper.IsMaskingActive;

        private Vector2 pointOnCircle(float angle) => new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));

        private Vector2 relativePosition(Vector2 localPos) => Vector2.Divide(localPos, DrawSize);

        private Color4 colourAt(Vector2 localPos) => DrawInfo.Colour.HasSingleColour
            ? (Color4)DrawInfo.Colour
            : DrawInfo.Colour.Interpolate(relativePosition(localPos)).Linear;

        private void addLineCap(Vector2 origin, float theta, float thetaDiff)
        {
            const float step = MathHelper.Pi / MAXRES;

            float dir = Math.Sign(thetaDiff);
            thetaDiff = dir * thetaDiff;

            int amountPoints = (int)Math.Ceiling(thetaDiff / step);

            if (dir < 0)
                theta += MathHelper.Pi;

            Vector2 current = origin + pointOnCircle(theta) * Width;
            Color4 currentColour = colourAt(current);
            current = Vector2Extensions.Transform(current, DrawInfo.Matrix);

            Vector2 screenOrigin = Vector2Extensions.Transform(origin, DrawInfo.Matrix);
            Color4 originColour = colourAt(origin);

            for (int i = 1; i <= amountPoints; i++)
            {
                // Center point
                Shared.HalfCircleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(screenOrigin.X, screenOrigin.Y, 1),
                    TexturePosition = new Vector2(1 - 1 / Texture.Width, 0),
                    Colour = originColour
                });

                // First outer point
                Shared.HalfCircleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(current.X, current.Y, 0),
                    TexturePosition = new Vector2(0, 0),
                    Colour = currentColour
                });

                float angularOffset = Math.Min(i * step, thetaDiff);
                current = origin + pointOnCircle(theta + dir * angularOffset) * Width;
                currentColour = colourAt(current);
                current = Vector2Extensions.Transform(current, DrawInfo.Matrix);

                // Second outer point
                Shared.HalfCircleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(current.X, current.Y, 0),
                    TexturePosition = new Vector2(0, 0),
                    Colour = currentColour
                });
            }
        }

        private void addLineQuads(Line line)
        {
            Vector2 ortho = line.OrthogonalDirection;
            Line lineLeft = new Line(line.StartPoint + ortho * Width, line.EndPoint + ortho * Width);
            Line lineRight = new Line(line.StartPoint - ortho * Width, line.EndPoint - ortho * Width);

            Line screenLineLeft = new Line(Vector2Extensions.Transform(lineLeft.StartPoint, DrawInfo.Matrix), Vector2Extensions.Transform(lineLeft.EndPoint, DrawInfo.Matrix));
            Line screenLineRight = new Line(Vector2Extensions.Transform(lineRight.StartPoint, DrawInfo.Matrix), Vector2Extensions.Transform(lineRight.EndPoint, DrawInfo.Matrix));
            Line screenLine = new Line(Vector2Extensions.Transform(line.StartPoint, DrawInfo.Matrix), Vector2Extensions.Transform(line.EndPoint, DrawInfo.Matrix));

            Shared.QuadBatch.Add(new TexturedVertex3D
            {
                Position = new Vector3(screenLineRight.EndPoint.X, screenLineRight.EndPoint.Y, 0),
                TexturePosition = new Vector2(0, 0),
                Colour = colourAt(lineRight.EndPoint)
            });
            Shared.QuadBatch.Add(new TexturedVertex3D
            {
                Position = new Vector3(screenLineRight.StartPoint.X, screenLineRight.StartPoint.Y, 0),
                TexturePosition = new Vector2(0, 0),
                Colour = colourAt(lineRight.StartPoint)
            });

            // Each "quad" of the slider is actually rendered as 2 quads, being split in half along the approximating line.
            // On this line the depth is 1 instead of 0, which is done properly handle self-overlap using the depth buffer.
            // Thus the middle vertices need to be added twice (once for each quad).
            Vector3 firstMiddlePoint = new Vector3(screenLine.StartPoint.X, screenLine.StartPoint.Y, 1);
            Vector3 secondMiddlePoint = new Vector3(screenLine.EndPoint.X, screenLine.EndPoint.Y, 1);
            Color4 firstMiddleColour = colourAt(line.StartPoint);
            Color4 secondMiddleColour = colourAt(line.EndPoint);

            for (int i = 0; i < 2; ++i)
            {
                Shared.QuadBatch.Add(new TexturedVertex3D
                {
                    Position = firstMiddlePoint,
                    TexturePosition = new Vector2(1 - 1 / Texture.Width, 0),
                    Colour = firstMiddleColour
                });
                Shared.QuadBatch.Add(new TexturedVertex3D
                {
                    Position = secondMiddlePoint,
                    TexturePosition = new Vector2(1 - 1 / Texture.Width, 0),
                    Colour = secondMiddleColour
                });
            }

            Shared.QuadBatch.Add(new TexturedVertex3D
            {
                Position = new Vector3(screenLineLeft.EndPoint.X, screenLineLeft.EndPoint.Y, 0),
                TexturePosition = new Vector2(0, 0),
                Colour = colourAt(lineLeft.EndPoint)
            });
            Shared.QuadBatch.Add(new TexturedVertex3D
            {
                Position = new Vector3(screenLineLeft.StartPoint.X, screenLineLeft.StartPoint.Y, 0),
                TexturePosition = new Vector2(0, 0),
                Colour = colourAt(lineLeft.StartPoint)
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


            foreach (Line segment in Segments)
                addLineQuads(segment);
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture == null || Texture.IsDisposed || Segments.Count == 0)
                return;

            GLWrapper.SetDepthTest(true);

            Shader shader = needsRoundedShader ? RoundedTextureShader : TextureShader;

            shader.Bind();

            Texture.TextureGL.WrapMode = TextureWrapMode.ClampToEdge;
            Texture.TextureGL.Bind();

            updateVertexBuffer();

            shader.Unbind();

            GLWrapper.SetDepthTest(false);
        }
    }
}
