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

            private void addCap(Line cap)
            {
                // The provided line is perpendicular to the end/start of a segment.
                // To get the remaining quad positions we are expanding said segment by the path radius.
                Vector2 ortho = cap.OrthogonalDirection;
                if (float.IsNaN(ortho.X) || float.IsNaN(ortho.Y))
                    ortho = Vector2.UnitY;

                Vector2 v2 = cap.StartPoint + ortho * radius;
                Vector2 v3 = cap.EndPoint + ortho * radius;

                drawQuad
                (
                    new Quad(cap.StartPoint, v2, cap.EndPoint, v3),
                    new Quad(new Vector2(0, -1), new Vector2(1, -1), new Vector2(0, 1), Vector2.One)
                );
            }

            private void addSegmentQuad(DrawableSegment segment)
            {
                drawQuad
                (
                    segment.DrawQuad,
                    new Quad(new Vector2(0, -1), new Vector2(0, -1), new Vector2(0, 1), new Vector2(0, 1))
                );
            }

            private void addConnectionBetween(DrawableSegment segment, DrawableSegment prevSegment)
            {
                float thetaDiff = segment.Guide.Theta - prevSegment.Guide.Theta;

                if (Math.Abs(thetaDiff) > MathF.PI)
                    thetaDiff = -Math.Sign(thetaDiff) * 2 * MathF.PI + thetaDiff;

                if (thetaDiff == 0f)
                    return;

                // more than 90 degrees - add end cap to the previous segment
                if (Math.Abs(thetaDiff) > Math.PI * 0.5)
                {
                    addEndCap(prevSegment);
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

                Line? segmentToDraw = null;
                SegmentStartLocation location = SegmentStartLocation.Outside;
                SegmentStartLocation modifiedLocation = SegmentStartLocation.Outside;
                SegmentStartLocation nextLocation = SegmentStartLocation.End;
                DrawableSegment? lastDrawnSegment = null;

                for (int i = 0; i < segments.Count; i++)
                {
                    if (segmentToDraw.HasValue)
                    {
                        float segmentToDrawLength = segmentToDraw.Value.Rho;

                        // If segment is too short, make its end point equal start point of a new segment
                        if (segmentToDrawLength < 1f)
                        {
                            segmentToDraw = new Line(segmentToDraw.Value.StartPoint, segments[i].EndPoint);
                            continue;
                        }

                        float progress = progressFor(segmentToDraw.Value, segmentToDrawLength, segments[i].EndPoint);
                        Vector2 closest = segmentToDraw.Value.At(progress);

                        // Expand segment if next end point is located within a line passing through it
                        if (Precision.AlmostEquals(closest, segments[i].EndPoint, 0.01f))
                        {
                            if (progress < 0)
                            {
                                // expand segment backwards
                                segmentToDraw = new Line(segments[i].EndPoint, segmentToDraw.Value.EndPoint);
                                modifiedLocation = SegmentStartLocation.Outside;
                                nextLocation = SegmentStartLocation.Start;
                            }
                            else if (progress > 1)
                            {
                                // or forward
                                segmentToDraw = new Line(segmentToDraw.Value.StartPoint, segments[i].EndPoint);
                                nextLocation = SegmentStartLocation.End;
                            }
                            else
                            {
                                nextLocation = SegmentStartLocation.Middle;
                            }
                        }
                        else // Otherwise draw the expanded segment
                        {
                            DrawableSegment s = new DrawableSegment(segmentToDraw.Value, radius, location, modifiedLocation);
                            addSegmentQuad(s);
                            connect(s, lastDrawnSegment);

                            lastDrawnSegment = s;
                            segmentToDraw = segments[i];
                            location = modifiedLocation = nextLocation;
                            nextLocation = SegmentStartLocation.End;
                        }
                    }
                    else
                    {
                        segmentToDraw = segments[i];
                    }
                }

                // Finish drawing last segment (if exists)
                if (segmentToDraw.HasValue)
                {
                    DrawableSegment s = new DrawableSegment(segmentToDraw.Value, radius, location, modifiedLocation);
                    addSegmentQuad(s);
                    connect(s, lastDrawnSegment);
                    addEndCap(s);
                }
            }

            /// <summary>
            /// Connects the start of the segment to the end of a previous one.
            /// </summary>
            private void connect(DrawableSegment segment, DrawableSegment? prevSegment)
            {
                if (!prevSegment.HasValue)
                {
                    // Nothing to connect to - add start cap
                    addStartCap(segment);
                    return;
                }

                switch (segment.ModifiedStartLocation)
                {
                    default:
                    case SegmentStartLocation.End:
                        // Segment starts at the end of the previous one
                        addConnectionBetween(segment, prevSegment.Value);
                        break;

                    case SegmentStartLocation.Start:
                    case SegmentStartLocation.Middle:
                        // Segment starts at the start or the middle of the previous one - add end cap to the previous segment
                        addEndCap(prevSegment.Value);
                        break;

                    case SegmentStartLocation.Outside:
                        // Segment starts outside the previous one.

                        // There's no need to add end cap in case when initial start location was at the end of the previous segment
                        // since created overlap will make this cap invisible anyway.
                        // Example: imagine letter "T" where vertical line is prev segment and horizontal is a segment started at the end
                        // of it, went to the right and then to the left (expanded backwards). In this case start location will be "End" and
                        // modified location will be "Outside". With that in mind we do not need to add the end cap at the top of the vertical
                        // line since horizontal one will pass through it. However, that wouldn't be the case if horizontal line was located at
                        // the middle and so end cap would be required.
                        if (segment.StartLocation != SegmentStartLocation.End)
                            addEndCap(prevSegment.Value);

                        // add start cap to the current one
                        addStartCap(segment);
                        break;
                }
            }

            private void addEndCap(DrawableSegment segment) =>
                addCap(new Line(segment.TopRight, segment.BottomRight));

            private void addStartCap(DrawableSegment segment) =>
                addCap(new Line(segment.BottomLeft, segment.TopLeft));

            private static float progressFor(Line line, float length, Vector2 point)
            {
                Vector2 a = (line.EndPoint - line.StartPoint) / length;
                return Vector2.Dot(a, point - line.StartPoint) / length;
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                triangleBatch?.Dispose();
            }

            private enum SegmentStartLocation
            {
                Start,
                Middle,
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

                /// <summary>
                /// Position of this <see cref="DrawableSegment"/> relative to the previous one.
                /// </summary>
                public SegmentStartLocation StartLocation { get; }

                /// <summary>
                /// Position of this modified <see cref="DrawableSegment"/> relative to the previous one.
                /// </summary>
                public SegmentStartLocation ModifiedStartLocation { get; }

                /// <param name="guide">The line defining this <see cref="DrawableSegment"/>.</param>
                /// <param name="radius">The path radius.</param>
                /// <param name="startLocation">Position of this <see cref="DrawableSegment"/> relative to the previous one.</param>
                /// <param name="modifiedStartLocation">Position of this modified <see cref="DrawableSegment"/> relative to the previous one.</param>
                public DrawableSegment(Line guide, float radius, SegmentStartLocation startLocation, SegmentStartLocation modifiedStartLocation)
                {
                    Guide = guide;
                    StartLocation = startLocation;
                    ModifiedStartLocation = modifiedStartLocation;

                    Vector2 ortho = Guide.OrthogonalDirection;
                    if (float.IsNaN(ortho.X) || float.IsNaN(ortho.Y))
                        ortho = Vector2.UnitY;

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
