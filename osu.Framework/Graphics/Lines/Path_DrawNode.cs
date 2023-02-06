// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;
using System;
using System.Collections.Generic;
using osuTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;

namespace osu.Framework.Graphics.Lines
{
    public partial class Path
    {
        private class PathDrawNode : DrawNode
        {
            public const int MAX_RES = 24;
            public const float MIN_SEGMENT_LENGTH = 1e-5f;

            protected new Path Source => (Path)base.Source;

            private readonly List<Line> segments = new List<Line>();

            private Texture texture;
            private Vector2 drawSize;
            private float radius;
            private IShader pathShader;

            private IVertexBatch<TexturedVertex3D> triangleBatch;

            public PathDrawNode(Path source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                segments.Clear();
                segments.AddRange(Source.segments);

                texture = Source.Texture;
                drawSize = Source.DrawSize;
                radius = Source.PathRadius;
                pathShader = Source.pathShader;
            }

            private Vector2 pointOnCircle(float angle) => new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            private Vector2 relativePosition(Vector2 localPos) => Vector2.Divide(localPos, drawSize);

            private Color4 colourAt(Vector2 localPos) => DrawColourInfo.Colour.TryExtractSingleColour(out SRGBColour colour)
                ? colour.Linear
                : DrawColourInfo.Colour.Interpolate(relativePosition(localPos)).Linear;

            private void addSegmentQuads(Line segment, Line segmentLeft, Line segmentRight, RectangleF texRect)
            {
                // Each segment of the slider is actually rendered as 2 quads, being split in half along the approximating line.
                // On this line the depth is 1 instead of 0, which is done properly handle self-overlap using the depth buffer.
                Vector3 firstMiddlePoint = new Vector3(segment.StartPoint.X, segment.StartPoint.Y, 1);
                Vector3 secondMiddlePoint = new Vector3(segment.EndPoint.X, segment.EndPoint.Y, 1);
                Color4 firstMiddleColour = colourAt(segment.StartPoint);
                Color4 secondMiddleColour = colourAt(segment.EndPoint);

                // Each of the quads (mentioned above) is rendered as 2 triangles:
                // Outer quad, triangle 1
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segmentRight.EndPoint.X, segmentRight.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segmentRight.EndPoint)
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segmentRight.StartPoint.X, segmentRight.StartPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segmentRight.StartPoint)
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = firstMiddlePoint,
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = firstMiddleColour
                });

                // Outer quad, triangle 2
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = firstMiddlePoint,
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = firstMiddleColour
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = secondMiddlePoint,
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = secondMiddleColour
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segmentRight.EndPoint.X, segmentRight.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segmentRight.EndPoint)
                });

                // Inner quad, triangle 1
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = firstMiddlePoint,
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = firstMiddleColour
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = secondMiddlePoint,
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = secondMiddleColour
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segmentLeft.EndPoint.X, segmentLeft.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segmentLeft.EndPoint)
                });

                // Inner quad, triangle 2
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segmentLeft.EndPoint.X, segmentLeft.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segmentLeft.EndPoint)
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segmentLeft.StartPoint.X, segmentLeft.StartPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segmentLeft.StartPoint)
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = firstMiddlePoint,
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = firstMiddleColour
                });
            }

            private void addSegmentTriangles(Line segmentLeft, Line segmentRight, Line prevSegmentLeft, Line prevSegmentRight, RectangleF texRect)
            {
                float thetaDiff = segmentLeft.Theta - prevSegmentLeft.Theta;
                float thetaOffset = -Math.Sign(thetaDiff) * MathF.PI / 2;

                if (Math.Abs(thetaDiff) > MathF.PI)
                {
                    thetaDiff = -Math.Sign(thetaDiff) * 2 * MathF.PI + thetaDiff;
                    thetaOffset = -thetaOffset;
                }

                if (thetaDiff == 0)
                    return;

                float theta0 = prevSegmentLeft.Theta + thetaOffset;
                float thetaStep = Math.Sign(thetaDiff) * MathF.PI / MAX_RES;
                int stepCount = (int)(thetaDiff / thetaStep) + 1;

                Vector2 origin = (segmentLeft.StartPoint + segmentRight.StartPoint) / 2;

                // Use segment end points instead of calculating start/end via theta to guarantee
                // that the vertices have the exact same position as the quads, which prevents
                // possible pixel gaps during rasterization.
                Vector2 current = thetaDiff > 0 ? prevSegmentRight.EndPoint : prevSegmentLeft.EndPoint;
                Vector2 end = thetaDiff > 0 ? segmentRight.StartPoint : segmentLeft.StartPoint;

                Color4 originColour = colourAt(origin);
                Color4 currentColour = colourAt(current);

                for (int i = 1; i <= stepCount; i++)
                {
                    // Center point
                    triangleBatch.Add(new TexturedVertex3D
                    {
                        Position = new Vector3(origin.X, origin.Y, 1),
                        TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                        Colour = originColour
                    });

                    // First outer point
                    triangleBatch.Add(new TexturedVertex3D
                    {
                        Position = new Vector3(current.X, current.Y, 0),
                        TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                        Colour = currentColour
                    });

                    current = i < stepCount ? origin + pointOnCircle(theta0 + i * thetaStep) * radius : end;
                    currentColour = colourAt(current);

                    // Second outer point
                    triangleBatch.Add(new TexturedVertex3D
                    {
                        Position = new Vector3(current.X, current.Y, 0),
                        TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                        Colour = currentColour
                    });
                }
            }

            private void updateVertexBuffer()
            {
                RectangleF texRect = texture.GetTextureRect(new RectangleF(0.5f, 0.5f, texture.Width - 1, texture.Height - 1));

                // Segments with extremely small (i.e. 0) lengths can mess up angle calculations
                int firstIndex = segments.FindIndex(l => l.Rho >= MIN_SEGMENT_LENGTH);
                int lastIndex = segments.FindLastIndex(l => l.Rho >= MIN_SEGMENT_LENGTH);

                Line? prevSegmentLeft = null;
                Line? prevSegmentRight = null;

                for (int i = firstIndex; i <= lastIndex; i++)
                {
                    Line segment = segments[i];

                    if (segment.Rho < MIN_SEGMENT_LENGTH)
                        continue;

                    Vector2 ortho = segment.OrthogonalDirection;
                    Line segmentLeft = new Line(segment.StartPoint + ortho * radius, segment.EndPoint + ortho * radius);
                    Line segmentRight = new Line(segment.StartPoint - ortho * radius, segment.EndPoint - ortho * radius);

                    addSegmentQuads(segment, segmentLeft, segmentRight, texRect);

                    if (prevSegmentLeft is Line psLeft && prevSegmentRight is Line psRight)
                    {
                        // Connection/filler triangles between segment quads
                        addSegmentTriangles(segmentLeft, segmentRight, psLeft, psRight, texRect);
                    }

                    if (i == firstIndex)
                    {
                        // Path start cap (semi-circle)
                        Line flippedLeft = new Line(segmentRight.EndPoint, segmentRight.StartPoint);
                        Line flippedRight = new Line(segmentLeft.EndPoint, segmentLeft.StartPoint);

                        addSegmentTriangles(segmentLeft, segmentRight, flippedLeft, flippedRight, texRect);
                    }

                    if (i == lastIndex)
                    {
                        // Path end cap (semi-circle)
                        Line flippedLeft = new Line(segmentRight.EndPoint, segmentRight.StartPoint);
                        Line flippedRight = new Line(segmentLeft.EndPoint, segmentLeft.StartPoint);

                        addSegmentTriangles(flippedLeft, flippedRight, segmentLeft, segmentRight, texRect);
                    }

                    prevSegmentLeft = segmentLeft;
                    prevSegmentRight = segmentRight;
                }
            }

            public override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (texture?.Available != true || segments.Count == 0)
                    return;

                // We multiply the size args by 3 such that the amount of vertices is a multiple of the amount of vertices
                // per primitive (triangles in this case). Otherwise overflowing the batch will result in wrong
                // grouping of vertices into primitives.
                triangleBatch ??= renderer.CreateLinearBatch<TexturedVertex3D>(MAX_RES * 200 * 3, 10, PrimitiveTopology.Triangles);

                renderer.PushLocalMatrix(DrawInfo.Matrix);
                renderer.PushDepthInfo(DepthInfo.Default);

                // Blending is removed to allow for correct blending between the wedges of the path.
                renderer.SetBlend(BlendingParameters.None);

                pathShader.Bind();

                texture.Bind();

                updateVertexBuffer();

                pathShader.Unbind();

                renderer.PopDepthInfo();
                renderer.PopLocalMatrix();
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                triangleBatch?.Dispose();
            }
        }
    }
}
