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

            /// <summary>
            /// Draws the provided segment to the screen.
            /// </summary>
            /// <param name="segment">The segment to be drawn.</param>
            /// <param name="prevSegment">Previous segment.</param>
            /// <param name="location">Position of the segment relative to the previous one.</param>
            /// <param name="endCap">Whether end cap of this segment must be drawn.</param>
            private void drawSegment(ref DrawableSegment segment, ref DrawableSegment prevSegment, SegmentStartLocation location, bool endCap)
            {
                // When segment starts outside the previous one, nothing is being connected to the start of the segment and start cap is required.
                bool startCap = location == SegmentStartLocation.Outside;

                // Segment starts at the end of the previous one
                if (location == SegmentStartLocation.End)
                {
                    // When drawConnectionBetween returns false - that means angle between segments is less than 90 degrees
                    // and we can just draw connection as a start cap of a current segment.
                    startCap |= !drawConnectionBetween(ref segment, ref prevSegment);
                }

                Vector2 topLeft = segment.TopLeft;
                Vector2 topRight = segment.TopRight;
                Vector2 bottomLeft = segment.BottomLeft;
                Vector2 bottomRight = segment.BottomRight;
                Vector2 offset = segment.DirectionNormalized * radius;

                if (startCap)
                {
                    topLeft -= offset;
                    bottomLeft -= offset;
                }

                if (endCap)
                {
                    topRight += offset;
                    bottomRight += offset;
                }

                drawQuad(topLeft, topRight, bottomLeft, bottomRight, segment.StartPoint, segment.EndPoint);
            }

            /// <summary>
            /// Draws connection between provided segments.
            /// </summary>
            /// <param name="segment">The current segment.</param>
            /// <param name="prevSegment">The segment which ends at the start of the current one.</param>
            /// <returns>Whether connection has been drawn.</returns>
            private bool drawConnectionBetween(ref DrawableSegment segment, ref DrawableSegment prevSegment)
            {
                Debug.Assert(prevSegment.EndPoint == segment.StartPoint);

                Vector2 dir = segment.DirectionNormalized;
                Vector2 dir2 = -prevSegment.DirectionNormalized;

                Vector2.Dot(ref dir, ref dir2, out float dot);

                // Angle between segments is less than 90 degrees - don't draw anything and use segment start cap instead.
                // Overdraw is inevitable anyway and this seems like a cheaper option than computing exact shape.
                // Also by doing this we can further reduce vertex count.
                if (dot >= 0)
                    return false;

                Vector2.PerpDot(ref dir, ref dir2, out float pDot);

                Line toConnect = pDot < 0f ? new Line(prevSegment.TopRight, segment.TopLeft) : new Line(prevSegment.BottomRight, segment.BottomLeft);

                float thetaDiff = Math.Abs(MathF.Atan(pDot / dot));
                Vector2 outerVertex = toConnect.StartPoint - dir2 * radius * (float)Math.Tan(thetaDiff * 0.5);
                Vector2 origin = segment.StartPoint;

                // position of a vertex which is located slightly below segments intersection to cover potentially missing pixels due to segments not having shared vertices
                // Vector2.Lerp(outerVertex, origin, 1.1f)
                Vector2 innerVertex = origin * 1.1f - outerVertex * 0.1f;

                // at this small angle curvature isn't noticeable, we can get away with a single triangle
                if (thetaDiff < Math.PI / max_res)
                    drawTriangle(toConnect.StartPoint, innerVertex, toConnect.EndPoint, origin, origin);
                else // 2 triangles for the remaining cases
                    drawQuad(toConnect.StartPoint, outerVertex, innerVertex, toConnect.EndPoint, origin, origin);

                return true;
            }

            private void drawTriangle(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 start, Vector2 end)
            {
                Debug.Assert(triangleBatch != null);

                triangleBatch.Add(new PathVertex
                {
                    Position = p0,
                    StartPos = start,
                    EndPos = end,
                    Radius = radius
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = p1,
                    StartPos = start,
                    EndPos = end,
                    Radius = radius
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = p2,
                    StartPos = start,
                    EndPos = end,
                    Radius = radius
                });
            }

            private void drawQuad(Vector2 topLeft, Vector2 topRight, Vector2 bottomLeft, Vector2 bottomRight, Vector2 start, Vector2 end)
            {
                Debug.Assert(triangleBatch != null);

                triangleBatch.Add(new PathVertex
                {
                    Position = topLeft,
                    StartPos = start,
                    EndPos = end,
                    Radius = radius
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = topRight,
                    StartPos = start,
                    EndPos = end,
                    Radius = radius
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = bottomLeft,
                    StartPos = start,
                    EndPos = end,
                    Radius = radius
                });

                triangleBatch.Add(new PathVertex
                {
                    Position = bottomLeft,
                    StartPos = start,
                    EndPos = end,
                    Radius = radius
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = topRight,
                    StartPos = start,
                    EndPos = end,
                    Radius = radius
                });
                triangleBatch.Add(new PathVertex
                {
                    Position = bottomRight,
                    StartPos = start,
                    EndPos = end,
                    Radius = radius
                });
            }

            private void updateVertexBuffer()
            {
                Debug.Assert(segments.Count > 0);

                Line segmentToDraw = segments[0];

                SegmentStartLocation location = SegmentStartLocation.Outside;
                SegmentStartLocation nextLocation = SegmentStartLocation.End;

                // We initialize "fake" initial segment before the 0'th one
                // so that on first drawSegment() call with current SegmentStartLocation parameters path start cap will be drawn.
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
                    Vector2.PerpDot(ref dir, ref dir2, out float pDot);

                    // Expand segment if next end point is located within a line passing through it (distance from the next vertex to the segment is small)
                    if (pDot * pDot / lengthSquared < 0.0001f)
                    {
                        nextLocation = SegmentStartLocation.StartOrMiddle;

                        Vector2.Dot(ref dir, ref dir2, out float dot);

                        // new vertex is located behind the segment start point, expand segment backwards
                        if (dot < 0)
                        {
                            segmentToDraw = new Line(nextVertex, segmentToDraw.EndPoint);
                            location = SegmentStartLocation.Outside;
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
                        // if next segment starts at the start or the middle of the current one, nothing will be connected to the end of the current segment - end cap is required.
                        drawSegment(ref s, ref lastDrawnSegment, location, nextLocation == SegmentStartLocation.StartOrMiddle);

                        lastDrawnSegment = s;
                        segmentToDraw = segments[i];
                        location = nextLocation;
                        nextLocation = SegmentStartLocation.End;
                    }
                }

                // Finish drawing last segment
                var ds = new DrawableSegment(segmentToDraw, radius);
                drawSegment(ref ds, ref lastDrawnSegment, location, true);
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
                /// End point of this <see cref="DrawableSegment"/>.
                /// </summary>
                public readonly Vector2 EndPoint;

                /// <summary>
                /// Start point of this <see cref="DrawableSegment"/>.
                /// </summary>
                public readonly Vector2 StartPoint;

                /// <summary>
                /// The normalized direction of this <see cref="DrawableSegment"/>.
                /// </summary>
                public readonly Vector2 DirectionNormalized;

                /// <summary>
                /// The top-left position of the draw quad of this <see cref="DrawableSegment"/>.
                /// </summary>
                public readonly Vector2 TopLeft;

                /// <summary>
                /// The top-right position of the draw quad of this <see cref="DrawableSegment"/>.
                /// </summary>
                public readonly Vector2 TopRight;

                /// <summary>
                /// The bottom-left position of the draw quad of this <see cref="DrawableSegment"/>.
                /// </summary>
                public readonly Vector2 BottomLeft;

                /// <summary>
                /// The bottom-right position of the draw quad of this <see cref="DrawableSegment"/>.
                /// </summary>
                public readonly Vector2 BottomRight;

                /// <param name="guide">The line defining this <see cref="DrawableSegment"/>.</param>
                /// <param name="radius">The path radius.</param>
                public DrawableSegment(Line guide, float radius)
                {
                    StartPoint = guide.StartPoint;
                    EndPoint = guide.EndPoint;

                    Vector2 dir = guide.DirectionNormalized;

                    if (float.IsNaN(dir.X) || float.IsNaN(dir.Y))
                        dir = Vector2.UnitX;

                    DirectionNormalized = dir;

                    Vector2 ortho = new Vector2(-dir.Y, dir.X);

                    TopLeft = StartPoint + ortho * radius;
                    TopRight = EndPoint + ortho * radius;
                    BottomLeft = StartPoint - ortho * radius;
                    BottomRight = EndPoint - ortho * radius;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct PathVertex : IEquatable<PathVertex>, IVertex
            {
                [VertexMember(2, VertexAttribPointerType.Float)]
                public Vector2 Position;

                [VertexMember(2, VertexAttribPointerType.Float)]
                public Vector2 StartPos;

                [VertexMember(2, VertexAttribPointerType.Float)]
                public Vector2 EndPos;

                [VertexMember(1, VertexAttribPointerType.Float)]
                public float Radius;

                public readonly bool Equals(PathVertex other) =>
                    Position.Equals(other.Position)
                    && StartPos.Equals(other.StartPos)
                    && EndPos.Equals(other.EndPos)
                    && Radius.Equals(other.Radius);
            }
        }
    }
}
