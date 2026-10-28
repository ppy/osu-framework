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
using osu.Framework.Graphics.Shaders.Types;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Lines
{
    public partial class Path
    {
        private class PathDrawNode : DrawNode
        {
            private const float precision = 0.01f; // Smallest allowed segment length. Used for segment reduction algorithm.

            protected new Path Source => (Path)base.Source;

            private readonly List<Line> segments = new List<Line>();

            private float radius;
            private IShader? pathShader;
            private IUniformBuffer<PathParameters>? parametersBuffer;

            private IVertexBatch<PathVertex>? quadBatch;

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

                // Size must be divisible by 4 such that the amount of vertices is a multiple of the amount of vertices
                // per primitive (quads in this case). Otherwise overflowing the batch will result in wrong
                // grouping of vertices into primitives.
                quadBatch ??= renderer.CreateQuadBatch<PathVertex>(9000, 10);

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

                parametersBuffer ??= renderer.CreateUniformBuffer<PathParameters>();
                parametersBuffer.Data = new PathParameters
                {
                    Radius = radius,
                };
                pathShader.BindUniformBlock("m_PathParameters", parametersBuffer);

                updateVertexBuffer();

                pathShader.Unbind();

                renderer.PopLocalMatrix();
            }

            /// <summary>
            /// Modifies provided segment to create visually continuous path and draws it to the screen.
            /// </summary>
            /// <param name="segmentToDraw">The segment to be drawn.</param>
            /// <param name="prevSegmentDirection">Direction of a previous segment. Used to compute angle between segments.</param>
            /// <param name="location">Position of the segment's start point relative to the previous segment.</param>
            /// <param name="endCap">Whether end cap of this segment must be drawn.</param>
            private void drawSegment(Line segmentToDraw, Vector2 prevSegmentDirection, SegmentStartLocation location, bool endCap)
            {
                Vector2 dir = segmentToDraw.Direction;
                float lengthSquared = dir.X * dir.X + dir.Y * dir.Y;
                Vector2 dirNorm = lengthSquared < precision * precision ? Vector2.UnitX : (dir / MathF.Sqrt(lengthSquared));
                Vector2 thicknessOffset = new Vector2(-dirNorm.Y, dirNorm.X) * radius;

                Vector2 capOffset = dirNorm * radius;
                Vector2 endOffset = endCap ? capOffset : Vector2.Zero;

                // Always assume start cap to act as a form of connection between segments.
                // The code below will manipulate its length to be as low as possible for each case to reduce overdraw.
                Vector2 startOffset = capOffset;

                // Segment starts from "inside" the previous one, the cap will be covered by the previous segment anyway.
                if (location == SegmentStartLocation.Inside)
                {
                    startOffset = Vector2.Zero;
                }
                else if (location == SegmentStartLocation.End) // Segment starts at the end of the previous one
                {
                    Vector2 dir2 = -prevSegmentDirection;
                    Vector2.Dot(ref dir, ref dir2, out float dot);

                    // if angle is more than 90 degrees we can reduce start cap length just enough for hole to be covered
                    if (dot < 0)
                    {
                        Vector2.PerpDot(ref dir, ref dir2, out float pDot);
                        float thetaDiff = Math.Abs(MathF.Atan(pDot / dot));
                        startOffset *= (float)Math.Sin(thetaDiff);
                    }
                }

                Debug.Assert(quadBatch != null);
                // Clockwise order: topLeft, topRight, bottomRight, bottomLeft.
                quadBatch.Add(new PathVertex(segmentToDraw.StartPoint + thicknessOffset - startOffset, segmentToDraw.StartPoint, segmentToDraw.EndPoint));
                quadBatch.Add(new PathVertex(segmentToDraw.EndPoint + thicknessOffset + endOffset, segmentToDraw.StartPoint, segmentToDraw.EndPoint));
                quadBatch.Add(new PathVertex(segmentToDraw.EndPoint - thicknessOffset + endOffset, segmentToDraw.StartPoint, segmentToDraw.EndPoint));
                quadBatch.Add(new PathVertex(segmentToDraw.StartPoint - thicknessOffset - startOffset, segmentToDraw.StartPoint, segmentToDraw.EndPoint));
            }

            private void updateVertexBuffer()
            {
                Debug.Assert(segments.Count > 0);

                Line segmentToDraw = segments[0];

                // location of the current segment relative to the previous one.
                SegmentStartLocation location = SegmentStartLocation.Outside;
                SegmentStartLocation nextLocation = SegmentStartLocation.End;

                // We initialize "fake" initial segment before the 0'th one
                // so that on first drawSegment() call with current SegmentStartLocation parameters path start cap will be added.
                Vector2 lastSegmentDirection = segmentToDraw.Direction;

                // This loop tries to merge consecutive path segments which are located within the same line.
                for (int i = 1; i < segments.Count; i++)
                {
                    Vector2 dir = segmentToDraw.Direction;
                    float lengthSquared = dir.X * dir.X + dir.Y * dir.Y;
                    Vector2 nextVertex = segments[i].EndPoint;

                    // If segment is too short, connect it to the next vertex directly.
                    if (lengthSquared < precision)
                    {
                        segmentToDraw = new Line(segmentToDraw.StartPoint, nextVertex);
                        continue;
                    }

                    Vector2 dir2 = nextVertex - segmentToDraw.StartPoint;
                    Vector2.PerpDot(ref dir, ref dir2, out float pDot);

                    // Expand segment if next vertex is located within a line passing through it (distance from the next vertex to the segment is less than precision)
                    if (pDot * pDot / lengthSquared < precision * precision)
                    {
                        nextLocation = SegmentStartLocation.Inside;

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
                        // if next segment starts inside the current one, nothing will be connected to the end of the current segment - end cap is required.
                        drawSegment(segmentToDraw, lastSegmentDirection, location, nextLocation == SegmentStartLocation.Inside);

                        lastSegmentDirection = dir;
                        segmentToDraw = segments[i];
                        location = nextLocation;
                        nextLocation = SegmentStartLocation.End;
                    }
                }

                // Finish drawing last segment
                drawSegment(segmentToDraw, lastSegmentDirection, location, true);
            }

            protected override void Dispose(bool isDisposing)
            {
                base.Dispose(isDisposing);

                quadBatch?.Dispose();
            }

            private enum SegmentStartLocation
            {
                Inside,
                End,
                Outside
            }

            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            private record struct PathParameters
            {
                public UniformFloat Radius;
                private UniformPadding12 pad;
            }

            [StructLayout(LayoutKind.Sequential)]
            public readonly struct PathVertex : IEquatable<PathVertex>, IVertex
            {
                [VertexMember(2, VertexAttribPointerType.Float)]
                public readonly Vector2 Position;

                [VertexMember(2, VertexAttribPointerType.Float)]
                public readonly Vector2 StartPos;

                [VertexMember(2, VertexAttribPointerType.Float)]
                public readonly Vector2 EndPos;

                public PathVertex(Vector2 position, Vector2 startPos, Vector2 endPos)
                {
                    Position = position;
                    StartPos = startPos;
                    EndPos = endPos;
                }

                public bool Equals(PathVertex other) =>
                    Position.Equals(other.Position)
                    && StartPos.Equals(other.StartPos)
                    && EndPos.Equals(other.EndPos);
            }
        }
    }
}
