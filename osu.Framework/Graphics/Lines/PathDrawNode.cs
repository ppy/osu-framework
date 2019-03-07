// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics.ES30;
using osu.Framework.Graphics.OpenGL;
using osuTK;
using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Batches;
using osu.Framework.Graphics.OpenGL.Vertices;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Lines
{
    public class PathDrawNode : DrawNode
    {
        public const int MAX_RES = 24;

        public List<Line> Segments;

        public Vector2 DrawSize;
        public float Radius;
        public Texture Texture;

        public IShader TextureShader;
        public IShader RoundedTextureShader;

        // We multiply the size param by 3 such that the amount of vertices is a multiple of the amount of vertices
        // per primitive (triangles in this case). Otherwise overflowing the batch will result in wrong
        // grouping of vertices into primitives.
        private readonly LinearBatch<TexturedVertex3D> halfCircleBatch = new LinearBatch<TexturedVertex3D>(MAX_RES * 100 * 3, 10, PrimitiveType.Triangles);
        private readonly QuadBatch<TexturedVertex3D> quadBatch = new QuadBatch<TexturedVertex3D>(200, 10);

        private bool needsRoundedShader => GLWrapper.IsMaskingActive;

        private Vector2 pointOnCircle(float angle) => new Vector2((float)Math.Sin(angle), -(float)Math.Cos(angle));

        private Vector2 relativePosition(Vector2 localPos) => Vector2.Divide(localPos, DrawSize);

        private Color4 colourAt(Vector2 localPos) => DrawColourInfo.Colour.HasSingleColour
            ? (Color4)DrawColourInfo.Colour
            : DrawColourInfo.Colour.Interpolate(relativePosition(localPos)).Linear;

        private void addLineCap(Vector2 origin, float theta, float thetaDiff, RectangleF texRect)
        {
            const float step = MathHelper.Pi / MAX_RES;

            float dir = Math.Sign(thetaDiff);
            thetaDiff = dir * thetaDiff;

            int amountPoints = (int)Math.Ceiling(thetaDiff / step);

            if (dir < 0)
                theta += MathHelper.Pi;

            Vector2 current = origin + pointOnCircle(theta) * Radius;
            Color4 currentColour = colourAt(current);
            current = Vector2Extensions.Transform(current, DrawInfo.Matrix);

            Vector2 screenOrigin = Vector2Extensions.Transform(origin, DrawInfo.Matrix);
            Color4 originColour = colourAt(origin);

            for (int i = 1; i <= amountPoints; i++)
            {
                // Center point
                halfCircleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(screenOrigin.X, screenOrigin.Y, 1),
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = originColour
                });

                // First outer point
                halfCircleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(current.X, current.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = currentColour
                });

                float angularOffset = Math.Min(i * step, thetaDiff);
                current = origin + pointOnCircle(theta + dir * angularOffset) * Radius;
                currentColour = colourAt(current);
                current = Vector2Extensions.Transform(current, DrawInfo.Matrix);

                // Second outer point
                halfCircleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(current.X, current.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = currentColour
                });
            }
        }

        private void addLineQuads(Line line, RectangleF texRect)
        {
            Vector2 ortho = line.OrthogonalDirection;
            Line lineLeft = new Line(line.StartPoint + ortho * Radius, line.EndPoint + ortho * Radius);
            Line lineRight = new Line(line.StartPoint - ortho * Radius, line.EndPoint - ortho * Radius);

            Line screenLineLeft = new Line(Vector2Extensions.Transform(lineLeft.StartPoint, DrawInfo.Matrix), Vector2Extensions.Transform(lineLeft.EndPoint, DrawInfo.Matrix));
            Line screenLineRight = new Line(Vector2Extensions.Transform(lineRight.StartPoint, DrawInfo.Matrix), Vector2Extensions.Transform(lineRight.EndPoint, DrawInfo.Matrix));
            Line screenLine = new Line(Vector2Extensions.Transform(line.StartPoint, DrawInfo.Matrix), Vector2Extensions.Transform(line.EndPoint, DrawInfo.Matrix));

            quadBatch.Add(new TexturedVertex3D
            {
                Position = new Vector3(screenLineRight.EndPoint.X, screenLineRight.EndPoint.Y, 0),
                TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                Colour = colourAt(lineRight.EndPoint)
            });
            quadBatch.Add(new TexturedVertex3D
            {
                Position = new Vector3(screenLineRight.StartPoint.X, screenLineRight.StartPoint.Y, 0),
                TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
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
                quadBatch.Add(new TexturedVertex3D
                {
                    Position = firstMiddlePoint,
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = firstMiddleColour
                });
                quadBatch.Add(new TexturedVertex3D
                {
                    Position = secondMiddlePoint,
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = secondMiddleColour
                });
            }

            quadBatch.Add(new TexturedVertex3D
            {
                Position = new Vector3(screenLineLeft.EndPoint.X, screenLineLeft.EndPoint.Y, 0),
                TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                Colour = colourAt(lineLeft.EndPoint)
            });
            quadBatch.Add(new TexturedVertex3D
            {
                Position = new Vector3(screenLineLeft.StartPoint.X, screenLineLeft.StartPoint.Y, 0),
                TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                Colour = colourAt(lineLeft.StartPoint)
            });
        }

        private void updateVertexBuffer()
        {
            Line line = Segments[0];
            float theta = line.Theta;

            // Offset by 0.5 pixels inwards to ensure we never sample texels outside the bounds
            RectangleF texRect = Texture.GetTextureRect(new RectangleF(0.5f, 0.5f, Texture.Width - 1, Texture.Height - 1));
            addLineCap(line.StartPoint, theta + MathHelper.Pi, MathHelper.Pi, texRect);

            for (int i = 1; i < Segments.Count; ++i)
            {
                Line nextLine = Segments[i];
                float nextTheta = nextLine.Theta;
                addLineCap(line.EndPoint, theta, nextTheta - theta, texRect);

                line = nextLine;
                theta = nextTheta;
            }

            addLineCap(line.EndPoint, theta, MathHelper.Pi, texRect);


            foreach (Line segment in Segments)
                addLineQuads(segment, texRect);
        }

        public override void Draw(Action<TexturedVertex2D> vertexAction)
        {
            base.Draw(vertexAction);

            if (Texture?.Available != true || Segments.Count == 0)
                return;

            GLWrapper.SetDepthTest(true);

            IShader shader = needsRoundedShader ? RoundedTextureShader : TextureShader;

            shader.Bind();

            Texture.TextureGL.WrapMode = TextureWrapMode.ClampToEdge;
            Texture.TextureGL.Bind();

            updateVertexBuffer();

            shader.Unbind();

            GLWrapper.SetDepthTest(false);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            halfCircleBatch.Dispose();
            quadBatch.Dispose();
        }
    }
}
