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
using osu.Framework.Utils;

namespace osu.Framework.Graphics.Lines
{
    public partial class Path
    {
        protected class PathDrawNode : DrawNode
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

            protected override void Draw(IRenderer renderer)
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
                ? colour.SRGB
                : DrawColourInfo.Colour.Interpolate(relativePosition(localPos)).SRGB;

            private void addSegmentQuads(SegmentWithThickness segment, RectangleF texRect)
            {
                Debug.Assert(triangleBatch != null);

                // Each segment of the path is actually rendered as 2 quads, being split in half along the approximating line.
                // On this line the depth is 1 instead of 0, which is done in order to properly handle self-overlap using the depth buffer.
                Vector3 firstMiddlePoint = new Vector3(segment.Guide.StartPoint.X, segment.Guide.StartPoint.Y, 1);
                Vector3 secondMiddlePoint = new Vector3(segment.Guide.EndPoint.X, segment.Guide.EndPoint.Y, 1);
                Color4 firstMiddleColour = colourAt(segment.Guide.StartPoint);
                Color4 secondMiddleColour = colourAt(segment.Guide.EndPoint);

                // Each of the quads (mentioned above) is rendered as 2 triangles:
                // Outer quad, triangle 1
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segment.EdgeRight.EndPoint.X, segment.EdgeRight.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segment.EdgeRight.EndPoint)
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segment.EdgeRight.StartPoint.X, segment.EdgeRight.StartPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segment.EdgeRight.StartPoint)
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
                    Position = new Vector3(segment.EdgeRight.EndPoint.X, segment.EdgeRight.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segment.EdgeRight.EndPoint)
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
                    Position = new Vector3(segment.EdgeLeft.EndPoint.X, segment.EdgeLeft.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segment.EdgeLeft.EndPoint)
                });

                // Inner quad, triangle 2
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segment.EdgeLeft.EndPoint.X, segment.EdgeLeft.EndPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segment.EdgeLeft.EndPoint)
                });
                triangleBatch.Add(new TexturedVertex3D
                {
                    Position = new Vector3(segment.EdgeLeft.StartPoint.X, segment.EdgeLeft.StartPoint.Y, 0),
                    TexturePosition = new Vector2(texRect.Left, texRect.Centre.Y),
                    Colour = colourAt(segment.EdgeLeft.StartPoint)
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
                            addSegmentQuads(s, texRect);
                            connect(s, lastDrawnSegment, texRect);

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
                    addSegmentQuads(s, texRect);
                    connect(s, lastDrawnSegment, texRect);
                    addEndCap(s, texRect);
                }
            }

            /// <summary>
            /// Connects the start of the segment to the end of a previous one.
            /// </summary>
            private void connect(SegmentWithThickness segment, SegmentWithThickness? prevSegment, RectangleF texRect)
            {
                if (!prevSegment.HasValue)
                {
                    // Nothing to connect to - add start cap
                    addStartCap(segment, texRect);
                    return;
                }

                switch (segment.ModifiedStartLocation)
                {
                    default:
                    case SegmentStartLocation.End:
                        // Segment starts at the end of the previous one
                        addConnectionBetween(segment, prevSegment.Value, texRect);
                        break;

                    case SegmentStartLocation.Start:
                    case SegmentStartLocation.Middle:
                        // Segment starts at the start or the middle of the previous one - add end cap to the previous segment
                        addEndCap(prevSegment.Value, texRect);
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
                            addEndCap(prevSegment.Value, texRect);

                        // add start cap to the current one
                        addStartCap(segment, texRect);
                        break;
                }
            }

            private void addConnectionBetween(SegmentWithThickness segment, SegmentWithThickness prevSegment, RectangleF texRect)
            {
                float thetaDiff = segment.Guide.Theta - prevSegment.Guide.Theta;
                addSegmentCaps(thetaDiff, segment.EdgeLeft, segment.EdgeRight, prevSegment.EdgeLeft, prevSegment.EdgeRight, texRect);
            }

            private void addEndCap(SegmentWithThickness segment, RectangleF texRect)
            {
                // Explanation of semi-circle caps:
                // Semi-circles are essentially 180 degree caps. So to create these caps, we
                // can simply "fake" a segment that's 180 degrees flipped. This works because
                // we are taking advantage of the fact that a path which makes a 180 degree
                // bend would have a semi-circle cap.

                Line flippedLeft = new Line(segment.EdgeRight.EndPoint, segment.EdgeRight.StartPoint);
                Line flippedRight = new Line(segment.EdgeLeft.EndPoint, segment.EdgeLeft.StartPoint);
                addSegmentCaps(MathF.PI, flippedLeft, flippedRight, segment.EdgeLeft, segment.EdgeRight, texRect);
            }

            private void addStartCap(SegmentWithThickness segment, RectangleF texRect)
            {
                Line flippedLeft = new Line(segment.EdgeRight.EndPoint, segment.EdgeRight.StartPoint);
                Line flippedRight = new Line(segment.EdgeLeft.EndPoint, segment.EdgeLeft.StartPoint);
                addSegmentCaps(MathF.PI, segment.EdgeLeft, segment.EdgeRight, flippedLeft, flippedRight, texRect);
            }

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
        }
    }
}
