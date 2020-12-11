// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Primitives
{
    [StructLayout(LayoutKind.Sequential)]
    public class Hexagon : IConvexPolygon, IEquatable<Hexagon>
    {
        // Note: Do not change the order of vertices. They are ordered in screen-space counter-clockwise fashion.
        // See: IPolygon.GetVertices()
        private static readonly float SQRT_3 = (float)Math.Sqrt(3);
        public readonly Vector2 Center;
        public readonly Vector2 P0;
        public readonly Vector2 P1;
        public readonly Vector2 P2;
        public readonly Vector2 P3;
        public readonly Vector2 P4;
        public readonly Vector2 P5;

        public readonly Triangle FarUpTriangle;
        public readonly Triangle NearUpTriangle;
        public readonly Triangle NearDownTriangle;
        public readonly Triangle FarDownTriangle;


        /// <summary>
        /// Creates a regular Hexagon from two opposing points; the center will be in between these two points.
        /// </summary>
        /// <param name="p0">The first point.</param>
        /// <param name="p3">The point directly across the hexagon.</param>
        public Hexagon(Vector2 p0, Vector2 p3)
        {
            P0 = p0;
            P3 = p3;

            Center = (p0 + p3) / 2;

            Vector2 p3ToCenter = p3 - Center;
            Vector2 betweenCenterAndP3 = (p3 + Center) / 2;
            Vector2 inverseP3CenterVector = p3ToCenter.PerpendicularLeft.Normalized();
            P4 = betweenCenterAndP3 - 0.5f * p3ToCenter.Length * SQRT_3 * inverseP3CenterVector;
            P2 = betweenCenterAndP3 + 0.5f * p3ToCenter.Length * SQRT_3 * inverseP3CenterVector;

            Vector2 p0ToCenter = Center - p0;
            Vector2 betweenCenterAndP0 = (Center + p0) / 2;
            Vector2 inverseP0CenterVector = p0ToCenter.PerpendicularLeft.Normalized();
            P1 = betweenCenterAndP0 + 0.5f * p0ToCenter.Length * SQRT_3 * inverseP0CenterVector;
            P5 = betweenCenterAndP0 - 0.5f * p0ToCenter.Length * SQRT_3 * inverseP0CenterVector;

            FarDownTriangle = new Triangle(P0, P1, P2);
            NearDownTriangle = new Triangle(P0, P2, P3);
            NearUpTriangle = new Triangle(P0, P3, P4);
            FarUpTriangle = new Triangle(P0, P4, P5);

        }

        public ReadOnlySpan<Vector2> GetAxisVertices() => GetVertices();

        public ReadOnlySpan<Vector2> GetVertices() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in P0), 6);

        public bool Equals(Hexagon other) =>
            P0 == other.P0 &&
            P3 == other.P3;

        /// <summary>
        /// Checks whether a point lies within the Hexagon.
        /// </summary>
        /// <param name="pos">The point to check.</param>
        /// <returns>Outcome of the check.</returns>
        public bool Contains(Vector2 pos) => FarUpTriangle.Contains(pos) || NearUpTriangle.Contains(pos) || NearDownTriangle.Contains(pos) || FarDownTriangle.Contains(pos);

        public RectangleF AABBFloat
        {
            get
            {
                float xMin = Math.Min(P0.X, Math.Min(P1.X, Math.Min(P2.X, Math.Min(P3.X, Math.Min(P4.X, P5.X)))));
                float yMin = Math.Min(P0.Y, Math.Min(P1.Y, Math.Min(P2.Y, Math.Min(P3.Y, Math.Min(P4.Y, P5.Y)))));
                float xMax = Math.Max(P0.X, Math.Max(P1.X, Math.Max(P2.X, Math.Max(P3.X, Math.Max(P4.X, P5.X)))));
                float yMax = Math.Max(P0.Y, Math.Max(P1.Y, Math.Max(P2.Y, Math.Max(P3.Y, Math.Max(P4.Y, P5.Y)))));

                return new RectangleF(xMin, yMin, xMax - xMin, yMax - yMin);
            }
        }

        public float Area => (FarUpTriangle.Area + NearUpTriangle.Area) * 2;
    }
}
