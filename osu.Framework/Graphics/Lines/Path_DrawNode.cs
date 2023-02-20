// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
using System.Diagnostics;

namespace osu.Framework.Graphics.Lines
{
    public partial class Path
    {
        private class PathDrawNode : DrawNode
        {
            private const int max_res = 24;

            protected new Path Source => (Path)base.Source;

            private readonly List<Line> segments = new List<Line>();

            private Texture? texture;
            private Vector2 drawSize;
            private float radius;
            private IShader? pathShader;

            private IVertexBatch<TexturedVertex3D>? triangleBatch;

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

            public override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (texture?.Available != true || segments.Count == 0 || pathShader == null)
                    return;

                // We multiply the size args by 3 such that the amount of vertices is a multiple of the amount of vertices
                // per primitive (triangles in this case). Otherwise overflowing the batch will result in wrong
                // grouping of vertices into primitives.
                triangleBatch ??= renderer.CreateLinearBatch<TexturedVertex3D>(max_res * 200 * 3, 10, PrimitiveTopology.Triangles);

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

            private Vector2 pointOnCircle(float angle) => new Vector2(MathF.Cos(angle), MathF.Sin(angle));

            private Vector2 relativePosition(Vector2 localPos) => Vector2.Divide(localPos, drawSize);

            private Color4 colourAt(Vector2 localPos) => DrawColourInfo.Colour.TryExtractSingleColour(out SRGBColour colour)
                ? colour.Linear
                : DrawColourInfo.Colour.Interpolate(relativePosition(localPos)).Linear;

            private void addSegmentQuads(Line segment, Line segmentLeft, Line segmentRight, RectangleF texRect)
            {
                Debug.Assert(triangleBatch != null);

                // Each segment of the path is actually rendered as 2 quads, being split in half along the approximating line.
                // On this line the depth is 1 instead of 0, which is done in order to properly handle self-overlap using the depth buffer.
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

            private void addSegmentCaps(float thetaDiff, Line segmentLeft, Line segmentRight, Line prevSegmentLeft, Line prevSegmentRight, RectangleF texRect)
            {
                Debug.Assert(triangleBatch != null);

                if (Math.Abs(thetaDiff) > MathF.PI)
                    thetaDiff = -Math.Sign(thetaDiff) * 2 * MathF.PI + thetaDiff;

                if (thetaDiff == 0f)
                    return;

                Vector2 origin = (segmentLeft.StartPoint + segmentRight.StartPoint) / 2;

                // Use segment end points instead of calculating start/end via theta to guarantee
                // that the vertices have the exact same position as the quads, which prevents
                // possible pixel gaps during rasterization.
                Vector2 current = thetaDiff > 0f ? prevSegmentRight.EndPoint : prevSegmentLeft.EndPoint;
                Vector2 end = thetaDiff > 0f ? segmentRight.StartPoint : segmentLeft.StartPoint;

                Line start = thetaDiff > 0f ? new Line(prevSegmentLeft.EndPoint, prevSegmentRight.EndPoint) : new Line(prevSegmentRight.EndPoint, prevSegmentLeft.EndPoint);
                float theta0 = start.Theta;
                float thetaStep = Math.Sign(thetaDiff) * MathF.PI / max_res;
                int stepCount = (int)MathF.Ceiling(thetaDiff / thetaStep);

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
                // Explanation of the terms "left" and "right":
                // "Left" and "right" are used here in terms of a typical (Cartesian) coordinate system.
                // So "left" corresponds to positive angles (anti-clockwise), and "right" corresponds
                // to negative angles (clockwise).
                //
                // Note that this is not the same as the actually used coordinate system, in which the
                // y-axis is flipped. In this system, "left" corresponds to negative angles (clockwise)
                // and "right" corresponds to positive angles (anti-clockwise).
                //
                // Using a Cartesian system makes the calculations more consistent with typical math,
                // such as in angle<->coordinate conversions and ortho vectors. For example, the x-unit
                // vector (1, 0) has the orthogonal y-unit vector (0, 1). This would be "left" in the
                // Cartesian system. But in the actual system, it's "right" and clockwise. Where
                // this becomes confusing is during debugging, because OpenGL uses a Cartesian system.
                // So to make debugging a bit easier (i.e. w/ RenderDoc or Nsight), this code uses terms
                // that make sense in the realm of OpenGL, rather than terms which  are technically
                // accurate in the actually used "flipped" system.

                Debug.Assert(texture != null);
                Debug.Assert(segments.Count > 0);

                RectangleF texRect = texture.GetTextureRect(new RectangleF(0.5f, 0.5f, texture.Width - 1, texture.Height - 1));

                Line? prevSegmentLeft = null;
                Line? prevSegmentRight = null;

                for (int i = 0; i < segments.Count; i++)
                {
                    Line currSegment = segments[i];

                    Vector2 ortho = currSegment.OrthogonalDirection;
                    if (float.IsNaN(ortho.X) || float.IsNaN(ortho.Y))
                        ortho = Vector2.UnitY;

                    Line currSegmentLeft = new Line(currSegment.StartPoint + ortho * radius, currSegment.EndPoint + ortho * radius);
                    Line currSegmentRight = new Line(currSegment.StartPoint - ortho * radius, currSegment.EndPoint - ortho * radius);

                    addSegmentQuads(currSegment, currSegmentLeft, currSegmentRight, texRect);

                    if (prevSegmentLeft is Line psLeft && prevSegmentRight is Line psRight)
                    {
                        Debug.Assert(i > 0);

                        // Connection/filler caps between segment quads
                        float thetaDiff = currSegment.Theta - segments[i - 1].Theta;
                        addSegmentCaps(thetaDiff, currSegmentLeft, currSegmentRight, psLeft, psRight, texRect);
                    }

                    // Explanation of semi-circle caps:
                    // Semi-circles are essentially 180 degree caps. So to create these caps, we
                    // can simply "fake" a segment that's 180 degrees flipped. This works because
                    // we are taking advantage of the fact that a path which makes a 180 degree
                    // bend would have a semi-circle cap.

                    if (i == 0)
                    {
                        // Path start cap (semi-circle);
                        Line flippedLeft = new Line(currSegmentRight.EndPoint, currSegmentRight.StartPoint);
                        Line flippedRight = new Line(currSegmentLeft.EndPoint, currSegmentLeft.StartPoint);

                        addSegmentCaps(MathF.PI, currSegmentLeft, currSegmentRight, flippedLeft, flippedRight, texRect);
                    }

                    if (i == segments.Count - 1)
                    {
                        // Path end cap (semi-circle)
                        Line flippedLeft = new Line(currSegmentRight.EndPoint, currSegmentRight.StartPoint);
                        Line flippedRight = new Line(currSegmentLeft.EndPoint, currSegmentLeft.StartPoint);

                        addSegmentCaps(MathF.PI, flippedLeft, flippedRight, currSegmentLeft, currSegmentRight, texRect);
                    }

                    prevSegmentLeft = currSegmentLeft;
                    prevSegmentRight = currSegmentRight;
                }
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                triangleBatch?.Dispose();
            }
        }
    }
}
