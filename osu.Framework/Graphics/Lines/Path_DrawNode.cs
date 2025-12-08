// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osuTK;
using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Rendering.Vertices;
using osu.Framework.Graphics.Shaders;
using System.Diagnostics;
using System.Runtime.InteropServices;
using osu.Framework.Utils;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Lines
{
    public partial class Path
    {
        private class PathDrawNode : DrawNode
        {
            private const int max_res = 24;

            protected new Path Source => (Path)base.Source;

            private readonly List<Line> segments = new List<Line>();

            private float radius;
            private IShader? pathShader;

            private IVertexBatch<PathVertex>? triangleBatch;

            public PathDrawNode(Path source)
                : base(source)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();

                segments.Clear();
                segments.AddRange(Source.segments);

                radius = Source.PathRadius;
                pathShader = Source.pathShader;
            }

            protected override void Draw(IRenderer renderer)
            {
                base.Draw(renderer);

                if (segments.Count == 0 || pathShader == null || radius == 0f)
                    return;

                // We multiply the size args by 3 such that the amount of vertices is a multiple of the amount of vertices
                // per primitive (triangles in this case). Otherwise overflowing the batch will result in wrong
                // grouping of vertices into primitives.
                triangleBatch ??= renderer.CreateLinearBatch<PathVertex>(max_res * 200 * 3, 10, PrimitiveTopology.Triangles);

                renderer.PushLocalMatrix(DrawInfo.Matrix);

                renderer.SetBlend(new BlendingParameters
                {
                    Source = BlendingType.One,
                    Destination = BlendingType.One,
                    SourceAlpha = BlendingType.One,
                    DestinationAlpha = BlendingType.One,
                    RGBEquation = BlendingEquation.Max,
                    AlphaEquation = BlendingEquation.Max,
                });

                pathShader.Bind();

                updateVertexBuffer();

                pathShader.Unbind();

                renderer.PopLocalMatrix();
            }

            private void drawSegment(DrawableSegment segment)
            {
                drawQuad
                (
                    segment.DrawQuad,
                    new Quad(new Vector2(0, -1), new Vector2(0, -1), new Vector2(0, 1), new Vector2(0, 1))
                );
            }

            private void drawConnectionBetween(DrawableSegment segment, DrawableSegment prevSegment)
            {
                float thetaDiff = segment.Guide.Theta - prevSegment.Guide.Theta;

                if (Math.Abs(thetaDiff) > MathF.PI)
                    thetaDiff = -Math.Sign(thetaDiff) * 2 * MathF.PI + thetaDiff;

                if (thetaDiff == 0f)
                    return;

                // more than 90 degrees - draw previous segment end cap
                if (Math.Abs(thetaDiff) > Math.PI * 0.5)
                {
                    drawEndCap(prevSegment);
                    return;
                }

                Vector2 origin = segment.Guide.StartPoint;
                Line end = thetaDiff > 0f ? new Line(segment.BottomLeft, segment.TopLeft) : new Line(segment.TopLeft, segment.BottomLeft);
                Line start = thetaDiff > 0f ? new Line(prevSegment.TopRight, prevSegment.BottomRight) : new Line(prevSegment.BottomRight, prevSegment.TopRight);

                // position of a vertex which is located slightly below segments intersection
                Vector2 innerVertex = Vector2.Lerp(start.StartPoint, end.EndPoint, 0.5f);

                // at this small angle curvature of the connection isn't noticeable, we can get away with a single triangle
                if (Math.Abs(thetaDiff) < Math.PI / max_res)
                {
                    drawTriangle(new Triangle(start.EndPoint, innerVertex, end.StartPoint), origin);
                    return;
                }

                // 2 triangles for the remaining cases
                Vector2 middle1 = Vector2.Lerp(start.EndPoint, end.StartPoint, 0.5f);
                Vector2 outerVertex = Vector2.Lerp(origin, middle1, radius / (float)Math.Cos(Math.Abs(thetaDiff) * 0.5) / Vector2.Distance(origin, middle1));
                drawQuad(new Quad(start.EndPoint, outerVertex, innerVertex, end.StartPoint), origin);
            }

            private void drawTriangle(Triangle triangle, Vector2 origin)
            {
                drawTriangle
                (
                    triangle,
                    new Triangle
                    (
                        Vector2.Divide(triangle.P0 - origin, radius),
                        Vector2.Divide(triangle.P1 - origin, radius),
                        Vector2.Divide(triangle.P2 - origin, radius)
                    )
                );
            }

            private void drawTriangle(Triangle triangle, Triangle relativePos)
            {
                Debug.Assert(triangleBatch != null);

                triangleBatch.Add(new PathVertex
                {
                    Position = triangle.P0,
                    RelativePos = relativePos.P0
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = triangle.P1,
                    RelativePos = relativePos.P1
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = triangle.P2,
                    RelativePos = relativePos.P2
                });
            }

            private void drawQuad(Quad quad, Vector2 origin)
            {
                drawQuad
                (
                    quad,
                    new Quad
                    (
                        Vector2.Divide(quad.TopLeft - origin, radius),
                        Vector2.Divide(quad.TopRight - origin, radius),
                        Vector2.Divide(quad.BottomLeft - origin, radius),
                        Vector2.Divide(quad.BottomRight - origin, radius)
                    )
                );
            }

            private void drawQuad(Quad quad, Quad relativePos)
            {
                Debug.Assert(triangleBatch != null);

                triangleBatch.Add(new PathVertex
                {
                    Position = quad.TopLeft,
                    RelativePos = relativePos.TopLeft
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = quad.TopRight,
                    RelativePos = relativePos.TopRight
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = quad.BottomLeft,
                    RelativePos = relativePos.BottomLeft
                });

                triangleBatch.Add(new PathVertex
                {
                    Position = quad.BottomLeft,
                    RelativePos = relativePos.BottomLeft
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = quad.TopRight,
                    RelativePos = relativePos.TopRight
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = quad.BottomRight,
                    RelativePos = relativePos.BottomRight
                });
            }

            private void updateVertexBuffer()
            {
                Debug.Assert(segments.Count > 0);

                Line segmentToDraw = segments[0];

                SegmentStartLocation location = SegmentStartLocation.End;
                SegmentStartLocation modifiedLocation = SegmentStartLocation.Outside;
                SegmentStartLocation nextLocation = SegmentStartLocation.End;

                // We initialize "fake" initial segment before the 0'th one
                // so that on first connect() call with current SegmentStartLocation parameters path start cap will be drawn.
                DrawableSegment lastDrawnSegment = new DrawableSegment(segments[0], radius);

                for (int i = 1; i < segments.Count; i++)
                {
                    Vector2 dir = segmentToDraw.Direction;
                    float lengthSquared = dir.X * dir.X + dir.Y * dir.Y;
                    Vector2 nextVertex = segments[i].EndPoint;

                    // If segment is too short, make its end point equal start point of a new segment
                    if (lengthSquared < 0.01f)
                    {
                        segmentToDraw = new Line(segmentToDraw.StartPoint, nextVertex);
                        continue;
                    }

                    Vector2 dir2 = nextVertex - segmentToDraw.StartPoint;

                    // Expand segment if next end point is located within a line passing through it
                    if (Precision.AlmostEquals(Vector2.PerpDot(dir, dir2), 0, 0.03f))
                    {
                        nextLocation = SegmentStartLocation.StartOrMiddle;

                        Vector2.Dot(ref dir, ref dir2, out float dot);

                        // new vertex is located behind the segment start point, expand segment backwards
                        if (dot < 0)
                        {
                            segmentToDraw = new Line(nextVertex, segmentToDraw.EndPoint);
                            modifiedLocation = SegmentStartLocation.Outside;
                        }
                        else if (dot > lengthSquared) // new vertex is located in front of the end point, expand segment forward
                        {
                            segmentToDraw = new Line(segmentToDraw.StartPoint, nextVertex);
                            nextLocation = SegmentStartLocation.End;
                        }
                    }
                    else // Otherwise draw the expanded segment
                    {
                        DrawableSegment s = new DrawableSegment(segmentToDraw, radius);
                        drawSegment(s);
                        connect(s, lastDrawnSegment, location, modifiedLocation);

                        lastDrawnSegment = s;
                        segmentToDraw = segments[i];
                        location = modifiedLocation = nextLocation;
                        nextLocation = SegmentStartLocation.End;
                    }
                }

                // Finish drawing last segment
                DrawableSegment last = new DrawableSegment(segmentToDraw, radius);
                connect(last, lastDrawnSegment, location, modifiedLocation);
                drawSegment(last);
                drawEndCap(last);
            }

            /// <summary>
            /// Connects the start of the segment to the end of a previous one.
            /// </summary>
            private void connect(DrawableSegment segment, DrawableSegment prevSegment, SegmentStartLocation initialLocation, SegmentStartLocation modifiedLocation)
            {
                // Segment starts at the end of the previous one
                if (modifiedLocation == SegmentStartLocation.End)
                {
                    drawConnectionBetween(segment, prevSegment);
                    return; // when drawing connection, we are in the situation when caps aren't needed
                }

                // Segment initially starts at the start or the middle of the previous one, nothing is being connected to the end of the previous segment - end cap is required.
                if (initialLocation == SegmentStartLocation.StartOrMiddle)
                    drawEndCap(prevSegment);

                // Segment starts outside the previous one, nothing is being connected to the start of the segment - start cap is required.
                if (modifiedLocation == SegmentStartLocation.Outside)
                    drawStartCap(segment);
            }

            private void drawEndCap(DrawableSegment segment)
            {
                Vector2 topRight = segment.TopRight + segment.Direction * radius;
                Vector2 bottomRight = segment.BottomRight + segment.Direction * radius;

                drawQuad
                (
                    new Quad(segment.TopRight, topRight, segment.BottomRight, bottomRight),
                    new Quad(new Vector2(0, -1), new Vector2(1, -1), new Vector2(0, 1), new Vector2(1, 1))
                );
            }

            private void drawStartCap(DrawableSegment segment)
            {
                Vector2 topLeft = segment.TopLeft - segment.Direction * radius;
                Vector2 bottomLeft = segment.BottomLeft - segment.Direction * radius;

                drawQuad
                (
                    new Quad(topLeft, segment.TopLeft, bottomLeft, segment.BottomLeft),
                    new Quad(new Vector2(-1, -1), new Vector2(0, -1), new Vector2(-1, 1), new Vector2(0, 1))
                );
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                triangleBatch?.Dispose();
            }

            private enum SegmentStartLocation
            {
                StartOrMiddle,
                End,
                Outside
            }

            private readonly struct DrawableSegment
            {
                /// <summary>
                /// The line defining this <see cref="DrawableSegment"/>.
                /// </summary>
                public Line Guide { get; }

                /// <summary>
                /// The direction of the <see cref="Guide"/> of this <see cref="DrawableSegment"/>.
                /// </summary>
                public Vector2 Direction { get; }

                /// <summary>
                /// The draw quad of this <see cref="DrawableSegment"/>.
                /// </summary>
                public Quad DrawQuad { get; }

                /// <summary>
                /// The top-left position of the <see cref="DrawQuad"/> of this <see cref="DrawableSegment"/>.
                /// </summary>
                public Vector2 TopLeft => DrawQuad.TopLeft;

                /// <summary>
                /// The top-right position of the <see cref="DrawQuad"/> of this <see cref="DrawableSegment"/>.
                /// </summary>
                public Vector2 TopRight => DrawQuad.TopRight;

                /// <summary>
                /// The bottom-left position of the <see cref="DrawQuad"/> of this <see cref="DrawableSegment"/>.
                /// </summary>
                public Vector2 BottomLeft => DrawQuad.BottomLeft;

                /// <summary>
                /// The bottom-right position of the <see cref="DrawQuad"/> of this <see cref="DrawableSegment"/>.
                /// </summary>
                public Vector2 BottomRight => DrawQuad.BottomRight;

                /// <param name="guide">The line defining this <see cref="DrawableSegment"/>.</param>
                /// <param name="radius">The path radius.</param>
                public DrawableSegment(Line guide, float radius)
                {
                    Guide = guide;

                    Vector2 dir = guide.DirectionNormalized;

                    if (float.IsNaN(dir.X) || float.IsNaN(dir.Y))
                        dir = Vector2.UnitX;

                    Direction = dir;

                    Vector2 ortho = new Vector2(-dir.Y, dir.X);

                    DrawQuad = new Quad
                    (
                        Guide.StartPoint + ortho * radius,
                        Guide.EndPoint + ortho * radius,
                        Guide.StartPoint - ortho * radius,
                        Guide.EndPoint - ortho * radius
                    );
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct PathVertex : IEquatable<PathVertex>, IVertex
            {
                [VertexMember(2, VertexAttribPointerType.Float)]
                public Vector2 Position;

                /// <summary>
                /// A position of a vertex, where distance from that position to (0,0) defines it's colour.
                /// Distance 0 means white and 1 means black.
                /// This position is being interpolated between vertices and final colour is being applied in the fragment shader.
                /// </summary>
                [VertexMember(2, VertexAttribPointerType.Float)]
                public Vector2 RelativePos;

                public readonly bool Equals(PathVertex other) =>
                    Position.Equals(other.Position)
                    && RelativePos.Equals(other.RelativePos);
            }
        }
    }
}
