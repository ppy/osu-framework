// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Utils
{
    /// <summary>
    /// Helper methods to approximate a path by interpolating a sequence of control points.
    /// </summary>
    public static class PathApproximator
    {
        private const float bezier_tolerance = 0.25f;

        /// <summary>
        /// The amount of pieces to calculate for each control point quadruplet.
        /// </summary>
        private const int catmull_detail = 50;

        private const float circular_arc_tolerance = 0.1f;

        /// <summary>
        /// Creates a piecewise-linear approximation of a bezier curve, by adaptively repeatedly subdividing
        /// the control points until their approximation error vanishes below a given threshold.
        /// </summary>
        /// <param name="controlPoints">The control points.</param>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> ApproximateBezier(ReadOnlySpan<Vector2> controlPoints)
        {
            return ApproximateBSpline(controlPoints);
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a clamped uniform B-spline with polynomial order p,
        /// by dividing it into a series of bezier control points at its knots, then adaptively repeatedly
        /// subdividing those until their approximation error vanishes below a given threshold.
        /// Retains previous bezier approximation functionality when p is 0 or too large to create knots.
        /// Algorithm unsuitable for large values of p with many knots.
        /// </summary>
        /// <param name="controlPoints">The control points.</param>
        /// <param name="p">The polynomial order.</param>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> ApproximateBSpline(ReadOnlySpan<Vector2> controlPoints, int p = 0)
        {
            List<Vector2> output = new List<Vector2>();
            int n = controlPoints.Length - 1;

            if (n < 0)
                return output;

            Stack<Vector2[]> toFlatten = new Stack<Vector2[]>();
            Stack<Vector2[]> freeBuffers = new Stack<Vector2[]>();

            var points = controlPoints.ToArray();

            if (p > 0 && p < n)
            {
                // Subdivide B-spline into bezier control points at knots.
                for (int i = 0; i < n - p; i++)
                {
                    var subBezier = new Vector2[p + 1];
                    subBezier[0] = points[i];

                    // Destructively insert the knot p-1 times via Boehm's algorithm.
                    for (int j = 0; j < p - 1; j++)
                    {
                        subBezier[j + 1] = points[i + 1];

                        for (int k = 1; k < p - j; k++)
                        {
                            int l = Math.Min(k, n - p - i);
                            points[i + k] = (l * points[i + k] + points[i + k + 1]) / (l + 1);
                        }
                    }

                    subBezier[p] = points[i + 1];
                    toFlatten.Push(subBezier);
                }

                toFlatten.Push(points[(n - p)..]);
                // Reverse the stack so elements can be accessed in order.
                toFlatten = new Stack<Vector2[]>(toFlatten);
            }
            else
            {
                // B-spline subdivision unnecessary, degenerate to single bezier.
                p = n;
                toFlatten.Push(points);
            }
            // "toFlatten" contains all the curves which are not yet approximated well enough.
            // We use a stack to emulate recursion without the risk of running into a stack overflow.
            // (More specifically, we iteratively and adaptively refine our curve with a
            // <a href="https://en.wikipedia.org/wiki/Depth-first_search">Depth-first search</a>
            // over the tree resulting from the subdivisions we make.)

            var subdivisionBuffer1 = new Vector2[p + 1];
            var subdivisionBuffer2 = new Vector2[p * 2 + 1];

            Vector2[] leftChild = subdivisionBuffer2;

            while (toFlatten.Count > 0)
            {
                Vector2[] parent = toFlatten.Pop();

                if (bezierIsFlatEnough(parent))
                {
                    // If the control points we currently operate on are sufficiently "flat", we use
                    // an extension to De Casteljau's algorithm to obtain a piecewise-linear approximation
                    // of the bezier curve represented by our control points, consisting of the same amount
                    // of points as there are control points.
                    bezierApproximate(parent, output, subdivisionBuffer1, subdivisionBuffer2, p + 1);

                    freeBuffers.Push(parent);
                    continue;
                }

                // If we do not yet have a sufficiently "flat" (in other words, detailed) approximation we keep
                // subdividing the curve we are currently operating on.
                Vector2[] rightChild = freeBuffers.Count > 0 ? freeBuffers.Pop() : new Vector2[p + 1];
                bezierSubdivide(parent, leftChild, rightChild, subdivisionBuffer1, p + 1);

                // We re-use the buffer of the parent for one of the children, so that we save one allocation per iteration.
                for (int i = 0; i < p + 1; ++i)
                    parent[i] = leftChild[i];

                toFlatten.Push(rightChild);
                toFlatten.Push(parent);
            }

            output.Add(controlPoints[n]);
            return output;
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a Catmull-Rom spline.
        /// </summary>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> ApproximateCatmull(ReadOnlySpan<Vector2> controlPoints)
        {
            var result = new List<Vector2>((controlPoints.Length - 1) * catmull_detail * 2);

            for (int i = 0; i < controlPoints.Length - 1; i++)
            {
                var v1 = i > 0 ? controlPoints[i - 1] : controlPoints[i];
                var v2 = controlPoints[i];
                var v3 = i < controlPoints.Length - 1 ? controlPoints[i + 1] : v2 + v2 - v1;
                var v4 = i < controlPoints.Length - 2 ? controlPoints[i + 2] : v3 + v3 - v2;

                for (int c = 0; c < catmull_detail; c++)
                {
                    result.Add(catmullFindPoint(ref v1, ref v2, ref v3, ref v4, (float)c / catmull_detail));
                    result.Add(catmullFindPoint(ref v1, ref v2, ref v3, ref v4, (float)(c + 1) / catmull_detail));
                }
            }

            return result;
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a circular arc curve.
        /// </summary>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> ApproximateCircularArc(ReadOnlySpan<Vector2> controlPoints)
        {
            CircularArcProperties pr = new CircularArcProperties(controlPoints);
            if (!pr.IsValid)
                return ApproximateBezier(controlPoints);

            // We select the amount of points for the approximation by requiring the discrete curvature
            // to be smaller than the provided tolerance. The exact angle required to meet the tolerance
            // is: 2 * Math.Acos(1 - TOLERANCE / r)
            // The special case is required for extremely short sliders where the radius is smaller than
            // the tolerance. This is a pathological rather than a realistic case.
            int amountPoints = 2 * pr.Radius <= circular_arc_tolerance ? 2 : Math.Max(2, (int)Math.Ceiling(pr.ThetaRange / (2 * Math.Acos(1 - circular_arc_tolerance / pr.Radius))));

            List<Vector2> output = new List<Vector2>(amountPoints);

            for (int i = 0; i < amountPoints; ++i)
            {
                double fract = (double)i / (amountPoints - 1);
                double theta = pr.ThetaStart + pr.Direction * fract * pr.ThetaRange;
                Vector2 o = new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)) * pr.Radius;
                output.Add(pr.Centre + o);
            }

            return output;
        }

        /// <summary>
        /// Computes the bounding box of a circular arc.
        /// </summary>
        /// <param name="controlPoints">Three distinct points on the arc.</param>
        /// <returns>The rectangle inscribing the circular arc.</returns>
        public static RectangleF CircularArcBoundingBox(ReadOnlySpan<Vector2> controlPoints)
        {
            CircularArcProperties pr = new CircularArcProperties(controlPoints);
            if (!pr.IsValid)
                return RectangleF.Empty;

            // We find the bounding box using the end-points, as well as
            // each 90 degree angle inside the range of the arc
            List<Vector2> points = new List<Vector2>
            {
                controlPoints[0],
                controlPoints[2]
            };

            const double right_angle = Math.PI / 2;
            double step = right_angle * pr.Direction;

            double quotient = pr.ThetaStart / right_angle;
            // choose an initial right angle, closest to ThetaStart, going in the direction of the arc.
            // thanks to this, when looping over quadrant points to check if they lie on the arc, we only need to check against ThetaEnd.
            double closestRightAngle = right_angle * (pr.Direction > 0 ? Math.Ceiling(quotient) : Math.Floor(quotient));

            // at most, four quadrant points must be considered.
            for (int i = 0; i < 4; ++i)
            {
                double angle = closestRightAngle + step * i;

                // check whether angle has exceeded ThetaEnd.
                // multiplying by Direction eliminates branching caused by the fact that step can be either positive or negative.
                if (Precision.DefinitelyBigger((angle - pr.ThetaEnd) * pr.Direction, 0))
                    break;

                Vector2 o = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * pr.Radius;
                points.Add(pr.Centre + o);
            }

            float minX = points.Min(p => p.X);
            float minY = points.Min(p => p.Y);
            float maxX = points.Max(p => p.X);
            float maxY = points.Max(p => p.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a linear curve.
        /// Basically, returns the input.
        /// </summary>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> ApproximateLinear(ReadOnlySpan<Vector2> controlPoints)
        {
            var result = new List<Vector2>(controlPoints.Length);

            foreach (var c in controlPoints)
                result.Add(c);

            return result;
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a lagrange polynomial.
        /// </summary>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> ApproximateLagrangePolynomial(ReadOnlySpan<Vector2> controlPoints)
        {
            // TODO: add some smarter logic here, chebyshev nodes?
            const int num_steps = 51;

            var result = new List<Vector2>(num_steps);

            double[] weights = Interpolation.BarycentricWeights(controlPoints);

            float minX = controlPoints[0].X;
            float maxX = controlPoints[0].X;

            for (int i = 1; i < controlPoints.Length; i++)
            {
                minX = Math.Min(minX, controlPoints[i].X);
                maxX = Math.Max(maxX, controlPoints[i].X);
            }

            float dx = maxX - minX;

            for (int i = 0; i < num_steps; i++)
            {
                float x = minX + dx / (num_steps - 1) * i;
                float y = (float)Interpolation.BarycentricLagrange(controlPoints, weights, x);
                result.Add(new Vector2(x, y));
            }

            return result;
        }

        /// <summary>
        /// Make sure the 2nd order derivative (approximated using finite elements) is within tolerable bounds.
        /// NOTE: The 2nd order derivative of a 2d curve represents its curvature, so intuitively this function
        ///       checks (as the name suggests) whether our approximation is _locally_ "flat". More curvy parts
        ///       need to have a denser approximation to be more "flat".
        /// </summary>
        /// <param name="controlPoints">The control points to check for flatness.</param>
        /// <returns>Whether the control points are flat enough.</returns>
        private static bool bezierIsFlatEnough(Vector2[] controlPoints)
        {
            for (int i = 1; i < controlPoints.Length - 1; i++)
            {
                if ((controlPoints[i - 1] - 2 * controlPoints[i] + controlPoints[i + 1]).LengthSquared > bezier_tolerance * bezier_tolerance * 4)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Subdivides n control points representing a bezier curve into 2 sets of n control points, each
        /// describing a bezier curve equivalent to a half of the original curve. Effectively this splits
        /// the original curve into 2 curves which result in the original curve when pieced back together.
        /// </summary>
        /// <param name="controlPoints">The control points to split.</param>
        /// <param name="l">Output: The control points corresponding to the left half of the curve.</param>
        /// <param name="r">Output: The control points corresponding to the right half of the curve.</param>
        /// <param name="subdivisionBuffer">The first buffer containing the current subdivision state.</param>
        /// <param name="count">The number of control points in the original list.</param>
        private static void bezierSubdivide(Vector2[] controlPoints, Vector2[] l, Vector2[] r, Vector2[] subdivisionBuffer, int count)
        {
            Vector2[] midpoints = subdivisionBuffer;

            for (int i = 0; i < count; ++i)
                midpoints[i] = controlPoints[i];

            for (int i = 0; i < count; i++)
            {
                l[i] = midpoints[0];
                r[count - i - 1] = midpoints[count - i - 1];

                for (int j = 0; j < count - i - 1; j++)
                    midpoints[j] = (midpoints[j] + midpoints[j + 1]) / 2;
            }
        }

        /// <summary>
        /// This uses <a href="https://en.wikipedia.org/wiki/De_Casteljau%27s_algorithm">De Casteljau's algorithm</a> to obtain an optimal
        /// piecewise-linear approximation of the bezier curve with the same amount of points as there are control points.
        /// </summary>
        /// <param name="controlPoints">The control points describing the bezier curve to be approximated.</param>
        /// <param name="output">The points representing the resulting piecewise-linear approximation.</param>
        /// <param name="count">The number of control points in the original list.</param>
        /// <param name="subdivisionBuffer1">The first buffer containing the current subdivision state.</param>
        /// <param name="subdivisionBuffer2">The second buffer containing the current subdivision state.</param>
        private static void bezierApproximate(Vector2[] controlPoints, List<Vector2> output, Vector2[] subdivisionBuffer1, Vector2[] subdivisionBuffer2, int count)
        {
            Vector2[] l = subdivisionBuffer2;
            Vector2[] r = subdivisionBuffer1;

            bezierSubdivide(controlPoints, l, r, subdivisionBuffer1, count);

            for (int i = 0; i < count - 1; ++i)
                l[count + i] = r[i + 1];

            output.Add(controlPoints[0]);

            for (int i = 1; i < count - 1; ++i)
            {
                int index = 2 * i;
                Vector2 p = 0.25f * (l[index - 1] + 2 * l[index] + l[index + 1]);
                output.Add(p);
            }
        }

        /// <summary>
        /// Finds a point on the spline at the position of a parameter.
        /// </summary>
        /// <param name="vec1">The first vector.</param>
        /// <param name="vec2">The second vector.</param>
        /// <param name="vec3">The third vector.</param>
        /// <param name="vec4">The fourth vector.</param>
        /// <param name="t">The parameter at which to find the point on the spline, in the range [0, 1].</param>
        /// <returns>The point on the spline at <paramref name="t"/>.</returns>
        private static Vector2 catmullFindPoint(ref Vector2 vec1, ref Vector2 vec2, ref Vector2 vec3, ref Vector2 vec4, float t)
        {
            float t2 = t * t;
            float t3 = t * t2;

            Vector2 result;
            result.X = 0.5f * (2f * vec2.X + (-vec1.X + vec3.X) * t + (2f * vec1.X - 5f * vec2.X + 4f * vec3.X - vec4.X) * t2 + (-vec1.X + 3f * vec2.X - 3f * vec3.X + vec4.X) * t3);
            result.Y = 0.5f * (2f * vec2.Y + (-vec1.Y + vec3.Y) * t + (2f * vec1.Y - 5f * vec2.Y + 4f * vec3.Y - vec4.Y) * t2 + (-vec1.Y + 3f * vec2.Y - 3f * vec3.Y + vec4.Y) * t3);

            return result;
        }
    }
}
