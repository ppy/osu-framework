// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;

namespace osu.Framework.Graphics.Primitives
{
    public struct Triangle : IConvexPolygon
    {
        public Vector2 P0;
        public Vector2 P1;
        public Vector2 P2;

        /// <summary>
        /// Axis formed between <see cref="P0"/> and <see cref="P1"/>.
        /// </summary>
        public Axis Axis01;

        /// <summary>
        /// Axis formed between <see cref="P1"/> and <see cref="P2"/>.
        /// </summary>
        public Axis Axis12;

        /// <summary>
        /// Axis formed between <see cref="P2"/> and <see cref="P0"/>.
        /// </summary>
        public Axis Axis20;

        public int VertexCount => 3;
        public int AxisCount => 3;

        public Triangle(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            P0 = p0;
            P1 = p1;
            P2 = p2;

            Axis01 = new Axis(P0, P1);
            Axis12 = new Axis(P1, P2);
            Axis20 = new Axis(P2, P0);
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

        public Vector2 GetVertex(int index)
        {
            switch (index)
            {
                case 0:
                    return P0;
                case 1:
                    return P1;
                case 2:
                    return P2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public Axis GetAxis(int index)
        {
            switch (index)
            {
                case 0:
                    return Axis01;
                case 1:
                    return Axis12;
                case 2:
                    return Axis20;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
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

        public double ConservativeArea => Math.Abs((P0.Y - P1.Y) * (P1.X - P2.X)) / 2;

        public double Area
        {
            get
            {
                float a = (P0 - P1).Length;
                float b = (P0 - P2).Length;
                float c = (P1 - P2).Length;
                float s = (a + b + c) / 2.0f;
                return Math.Sqrt(s * (s - a) * (s - b) * (s - c));
            }
        }
    }
}
