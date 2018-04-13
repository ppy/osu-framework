// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;

namespace osu.Framework.Graphics.Primitives
{
    public struct Triangle
    {
        public Vector2 P0;
        public Vector2 P1;
        public Vector2 P2;

        public Triangle(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;
        }

        /// <summary>
        /// Checks whether a point lies within the triangle.
        /// </summary>
        /// <param name="pos">The point to check.</param>
        /// <returns>Outcome of the check.</returns>
        public bool Contains(Vector2 pos)
        {
            // This code parametrizes pos as a linear combination of 2 edges s*(p1-p0) + t*(p2->p0).
            // pos is contained if s>0, t>0, s+t<1
            float area2 = P0.Y * (P2.X - P1.X) + P0.X * (P1.Y - P2.Y) + P1.X * P2.Y - P1.Y * P2.X;
            if (area2 == 0)
                return false;

            float s = (P0.Y * P2.X - P0.X * P2.Y + (P2.Y - P0.Y) * pos.X + (P0.X - P2.X) * pos.Y) / area2;
            if (s < 0)
                return false;

            float t = (P0.X * P1.Y - P0.Y * P1.X + (P0.Y - P1.Y) * pos.X + (P1.X - P0.X) * pos.Y) / area2;
            if (t < 0 || s + t > 1)
                return false;

            return true;
        }

        public RectangleF AABBFloat
        {
            get
            {
                float xMin = Math.Min(P0.X, Math.Min(P1.X, P2.X));
                float yMin = Math.Min(P0.Y, Math.Min(P1.Y, P2.Y));
                float xMax = Math.Max(P0.X, Math.Max(P1.X, P2.X));
                float yMax = Math.Max(P0.Y, Math.Max(P1.Y, P2.Y));

                return new RectangleF(xMin, yMin, xMax - xMin, yMax - yMin);
            }
        }

        public float ConservativeArea => Math.Abs((P0.Y - P1.Y) * (P1.X - P2.X)) / 2;

        public float Area
        {
            get
            {
                float a = (P0 - P1).Length;
                float b = (P0 - P2).Length;
                float c = (P1 - P2).Length;
                float s = (a + b + c) / 2.0f;
                return (float)Math.Sqrt(s * (s - a) * (s - b) * (s - c));
            }
        }
    }
}
