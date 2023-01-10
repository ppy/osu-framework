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

            private void addLineCap(Vector2 origin, float theta, float thetaDiff, RectangleF texRect)
            {
                float thetaStep = Math.Sign(thetaDiff) * MathF.PI / MAX_RES;
                int amountPoints = (int)(thetaDiff / thetaStep) + 1;

                Vector2 current = origin + pointOnCircle(theta) * radius;
                Color4 currentColour = colourAt(current);
                Color4 originColour = colourAt(origin);

                for (int i = 1; i <= amountPoints; i++)
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

                    float angularOffset = MathF.MinMagnitude(i * thetaStep, thetaDiff);
                    current = origin + pointOnCircle(theta + angularOffset) * radius;
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

            private void addLineQuads(Line line, RectangleF texRect)
            {
                Vector2 ortho = line.OrthogonalDirection;
                Line lineLeft = new Line(line.StartPoint + ortho * radius, line.EndPoint + ortho * radius);
                Line lineRight = new Line(line.StartPoint - ortho * radius, line.EndPoint - ortho * radius);

                // Each segment of the slider is actually rendered as 2 quads, being split in half along the approximating line.
                // On this line the depth is 1 instead of 0, which is done properly handle self-overlap using the depth buffer.
                Vector3 firstMiddlePoint = new Vector3(line.StartPoint.X, line.StartPoint.Y, 1);
                Vector3 secondMiddlePoint = new Vector3(line.EndPoint.X, line.EndPoint.Y, 1);
                Color4 firstMiddleColour = colourAt(line.StartPoint);
                Color4 secondMiddleColour = colourAt(line.EndPoint);

                // Each of the quads (mentioned above) is rendered as 2 triangles:
                // Outer quad, triangle 1
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(lineRight.EndPoint.X, lineRight.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(lineRight.EndPoint)
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(lineRight.StartPoint.X, lineRight.StartPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(lineRight.StartPoint)
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
                    Position = new Vector3(lineRight.EndPoint.X, lineRight.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(lineRight.EndPoint)
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
                    Position = new Vector3(lineLeft.EndPoint.X, lineLeft.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(lineLeft.EndPoint)
                });

                // Inner quad, triangle 2
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(lineLeft.EndPoint.X, lineLeft.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(lineLeft.EndPoint)
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(lineLeft.StartPoint.X, lineLeft.StartPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(lineLeft.StartPoint)
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = firstMiddlePoint,
                    TexturePosition = new Vector2(texRect.Right, texRect.Centre.Y),
                    Colour = firstMiddleColour
                });
            }

            private void updateVertexBuffer()
            {
                int firstIndex = segments.FindIndex(l => l.Rho >= MIN_SEGMENT_LENGTH);
                int lastIndex = segments.FindLastIndex(l => l.Rho >= MIN_SEGMENT_LENGTH);

                Line currentSegment = segments[firstIndex];
                float currentTheta = currentSegment.Theta;

                // Offset by 0.5 pixels inwards to ensure we never sample texels outside the bounds
                RectangleF texRect = texture.GetTextureRect(new RectangleF(0.5f, 0.5f, texture.Width - 1, texture.Height - 1));

                addLineCap(currentSegment.StartPoint, currentTheta + MathF.PI / 2, MathF.PI, texRect);

                for (int i = firstIndex + 1; i <= lastIndex; ++i)
                {
                    Line nextSegment = segments[i];
                    float nextTheta = nextSegment.Theta;

                    if (nextSegment.Rho >= MIN_SEGMENT_LENGTH)
                    {
                        float deltaTheta = nextTheta - currentTheta;
                        float offsetTheta = -Math.Sign(deltaTheta) * MathF.PI / 2;

                        if (Math.Abs(deltaTheta) > MathF.PI)
                        {
                            deltaTheta = -Math.Sign(deltaTheta) * (2 * MathF.PI - Math.Abs(deltaTheta));
                            offsetTheta = -offsetTheta;
                        }

                        addLineCap(currentSegment.EndPoint, currentTheta + offsetTheta, deltaTheta, texRect);

                        currentSegment = nextSegment;
                        currentTheta = nextTheta;
                    }
                }

                addLineCap(currentSegment.EndPoint, currentTheta - MathF.PI / 2, MathF.PI, texRect);

                for (int i = firstIndex; i <= lastIndex; i++)
                {
                    if (segments[i].Rho >= MIN_SEGMENT_LENGTH)
                        addLineQuads(segments[i], texRect);
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
