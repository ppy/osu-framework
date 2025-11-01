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

                if (segments.Count == 0 || pathShader == null)
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

            private void addCap(Line cap, Vector2 origin)
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
                    new Quad(cap.StartPoint, v2, origin, v3),
                    new Quad(new Vector2(0, -1), new Vector2(1, -1), Vector2.Zero, Vector2.One)
                );

                drawTriangle
                (
                    new Triangle(origin, v3, cap.EndPoint),
                    new Triangle(Vector2.Zero, Vector2.One, new Vector2(0, 1))
                );
            }

            private void addSegmentQuad(SegmentWithThickness segment)
            {
                drawQuad
                (
                    new Quad(segment.EdgeLeft.StartPoint, segment.EdgeLeft.EndPoint, segment.Guide.StartPoint, segment.Guide.EndPoint),
                    new Quad(new Vector2(0, -1), new Vector2(0, -1), Vector2.Zero, Vector2.Zero)
                );
                drawQuad
                (
                    new Quad(segment.EdgeRight.StartPoint, segment.EdgeRight.EndPoint, segment.Guide.StartPoint, segment.Guide.EndPoint),
                    new Quad(new Vector2(0, 1), new Vector2(0, 1), Vector2.Zero, Vector2.Zero)
                );
            }

            private void addConnectionBetween(SegmentWithThickness segment, SegmentWithThickness prevSegment)
            {
                float thetaDiff = segment.Guide.Theta - prevSegment.Guide.Theta;

                if (Math.Abs(thetaDiff) > MathF.PI)
                    thetaDiff = -Math.Sign(thetaDiff) * 2 * MathF.PI + thetaDiff;

                if (thetaDiff == 0f)
                    return;

                Vector2 origin = segment.Guide.StartPoint;
                Vector2 end = thetaDiff > 0f ? segment.EdgeRight.StartPoint : segment.EdgeLeft.StartPoint;
                Line start = thetaDiff > 0f ? new Line(prevSegment.EdgeLeft.EndPoint, prevSegment.EdgeRight.EndPoint) : new Line(prevSegment.EdgeRight.EndPoint, prevSegment.EdgeLeft.EndPoint);

                if (Math.Abs(thetaDiff) < Math.PI / max_res) // small angle, 1 triangle
                {
                    drawTriangle
                    (
                        new Triangle(start.EndPoint, origin, end),
                        new Triangle(new Vector2(1, 0), Vector2.Zero, new Vector2(1, 0))
                    );
                }
                else if (Math.Abs(thetaDiff) < Math.PI * 0.5) // less than 90 degrees, 2 triangles
                {
                    Vector2 middle = Vector2.Lerp(start.EndPoint, end, 0.5f);
                    Vector2 v3 = Vector2.Lerp(origin, middle, radius / (float)Math.Cos(Math.Abs(thetaDiff) * 0.5) / Vector2.Distance(origin, middle));

                    drawQuad(new Quad(start.EndPoint, v3, origin, end), origin);
                }
                else // more than 90 degrees - 3 triangles
                {
                    Vector2 ortho = start.OrthogonalDirection;
                    if (float.IsNaN(ortho.X) || float.IsNaN(ortho.Y))
                        ortho = Vector2.UnitY;

                    Vector2 v1 = start.StartPoint + Math.Sign(thetaDiff) * ortho * radius;
                    Vector2 v2 = start.EndPoint + Math.Sign(thetaDiff) * ortho * radius;
                    Vector2 middle = Vector2.Lerp(v1, v2, 0.5f);

                    Vector2 middle2 = Vector2.Lerp(middle, end, 0.5f);
                    Vector2 v3 = Vector2.Lerp(origin, middle2, radius / (float)Math.Cos((Math.Abs(thetaDiff) - Math.PI * 0.5) * 0.5) / Vector2.Distance(origin, middle2));

                    drawQuad(new Quad(start.EndPoint, v2, origin, v3), origin);
                    drawTriangle(new Triangle(origin, v3, end), origin);
                }
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

                Debug.Assert(segments.Count > 0);

                Line? segmentToDraw = null;
                SegmentStartLocation location = SegmentStartLocation.Outside;
                SegmentStartLocation modifiedLocation = SegmentStartLocation.Outside;
                SegmentWithThickness? lastDrawnSegment = null;

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
                        if (Precision.AlmostEquals(closest, segments[i].EndPoint, 0.1f))
                        {
                            if (progress < 0)
                            {
                                // expand segment backwards
                                segmentToDraw = new Line(segments[i].EndPoint, segmentToDraw.Value.EndPoint);
                                modifiedLocation = SegmentStartLocation.Outside;
                            }
                            else if (progress > 1)
                            {
                                // or forward
                                segmentToDraw = new Line(segmentToDraw.Value.StartPoint, segments[i].EndPoint);
                            }
                        }
                        else // Otherwise draw the expanded segment
                        {
                            SegmentWithThickness s = new SegmentWithThickness(segmentToDraw.Value, radius, location, modifiedLocation);
                            addSegmentQuad(s);
                            connect(s, lastDrawnSegment);

                            lastDrawnSegment = s;

                            // Figure out at which point within currently drawn segment the new one starts
                            float p = progressFor(segmentToDraw.Value, segmentToDrawLength, segments[i].StartPoint);
                            segmentToDraw = segments[i];
                            location = modifiedLocation = Precision.AlmostEquals(p, 1f) ? SegmentStartLocation.End : Precision.AlmostEquals(p, 0f) ? SegmentStartLocation.Start : SegmentStartLocation.Middle;
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
                    SegmentWithThickness s = new SegmentWithThickness(segmentToDraw.Value, radius, location, modifiedLocation);
                    addSegmentQuad(s);
                    connect(s, lastDrawnSegment);
                    addEndCap(s);
                }
            }

            /// <summary>
            /// Connects the start of the segment to the end of a previous one.
            /// </summary>
            private void connect(SegmentWithThickness segment, SegmentWithThickness? prevSegment)
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

            private void addEndCap(SegmentWithThickness segment) =>
                addCap(new Line(segment.EdgeLeft.EndPoint, segment.EdgeRight.EndPoint), segment.Guide.EndPoint);

            private void addStartCap(SegmentWithThickness segment) =>
                addCap(new Line(segment.EdgeRight.StartPoint, segment.EdgeLeft.StartPoint), segment.Guide.StartPoint);

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

            private readonly struct SegmentWithThickness
            {
                /// <summary>
                /// The line defining this <see cref="SegmentWithThickness"/>.
                /// </summary>
                public Line Guide { get; }

                /// <summary>
                /// The line parallel to <see cref="Guide"/> and located on the left side of it.
                /// </summary>
                public Line EdgeLeft { get; }

                /// <summary>
                /// The line parallel to <see cref="Guide"/> and located on the right side of it.
                /// </summary>
                public Line EdgeRight { get; }

                /// <summary>
                /// Position of this <see cref="SegmentWithThickness"/> relative to the previous one.
                /// </summary>
                public SegmentStartLocation StartLocation { get; }

                /// <summary>
                /// Position of this modified <see cref="SegmentWithThickness"/> relative to the previous one.
                /// </summary>
                public SegmentStartLocation ModifiedStartLocation { get; }

                /// <param name="guide">The line defining this <see cref="SegmentWithThickness"/>.</param>
                /// <param name="distance">The distance at which <see cref="EdgeLeft"/> and <see cref="EdgeRight"/> will be located from the <see cref="Guide"/>.</param>
                /// <param name="startLocation">Position of this <see cref="SegmentWithThickness"/> relative to the previous one.</param>
                /// <param name="modifiedStartLocation">Position of this modified <see cref="SegmentWithThickness"/> relative to the previous one.</param>
                public SegmentWithThickness(Line guide, float distance, SegmentStartLocation startLocation, SegmentStartLocation modifiedStartLocation)
                {
                    Guide = guide;
                    StartLocation = startLocation;
                    ModifiedStartLocation = modifiedStartLocation;

                    Vector2 ortho = Guide.OrthogonalDirection;
                    if (float.IsNaN(ortho.X) || float.IsNaN(ortho.Y))
                        ortho = Vector2.UnitY;

                    EdgeLeft = new Line(Guide.StartPoint + ortho * distance, Guide.EndPoint + ortho * distance);
                    EdgeRight = new Line(Guide.StartPoint - ortho * distance, Guide.EndPoint - ortho * distance);
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
