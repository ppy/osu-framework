// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Utils
{
    /// <summary>
    /// Helper methods to approximate a path by interpolating a sequence of control points.
    /// </summary>
    public static class PathApproximator
    {
        internal const float BEZIER_TOLERANCE = 0.25f;

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
        public static List<Vector2> BezierToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints)
        {
            return BSplineToPiecewiseLinear(controlPoints, Math.Max(1, controlPoints.Length - 1));
        }

        /// <summary>
        /// Converts a B-spline with polynomial order <paramref name="degree"/> to a series of Bezier control points
        /// via Boehm's algorithm.
        /// </summary>
        /// <remarks>
        /// Does nothing if <paramref name="controlPoints"/> has zero points or one point.
        /// Algorithm unsuitable for large values of <paramref name="degree"/> with many knots.
        /// </remarks>
        /// <param name="controlPoints">The control points.</param>
        /// <param name="degree">The polynomial order.</param>
        /// <returns>An array of vectors containing control point positions for the resulting Bezier curve.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="degree"/> was less than 1.</exception>
        public static Vector2[] BSplineToBezier(ReadOnlySpan<Vector2> controlPoints, int degree)
        {
            // Zero-th degree splines would be piecewise-constant, which cannot be represented by the piecewise-
            // linear output of this function. Negative degrees would require rational splines which this code
            // does not support.
            ArgumentOutOfRangeException.ThrowIfLessThan(degree, 1);

            // Spline fitting does not make sense when the input contains no points or just one point. In this case
            // the user likely wants this function to behave like a no-op.
            if (controlPoints.Length < 2)
                return controlPoints.Length == 0 ? Array.Empty<Vector2>() : new[] { controlPoints[0] };

            return bSplineToBezierInternal(controlPoints, ref degree).SelectMany(segment => segment).ToArray();
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a clamped uniform B-spline with polynomial order <paramref name="degree"/>,
        /// by dividing it into a series of bezier control points at its knots, then adaptively repeatedly
        /// subdividing those until their approximation error vanishes below a given threshold.
        /// </summary>
        /// <remarks>
        /// Does nothing if <paramref name="controlPoints"/> has zero points or one point.
        /// Generalises to bezier approximation functionality when <paramref name="degree"/> is too large to create knots.
        /// Algorithm unsuitable for large values of <paramref name="degree"/> with many knots.
        /// </remarks>
        /// <param name="controlPoints">The control points.</param>
        /// <param name="degree">The polynomial order.</param>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="degree"/> was less than 1.</exception>
        public static List<Vector2> BSplineToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints, int degree)
        {
            // Zero-th degree splines would be piecewise-constant, which cannot be represented by the piecewise-
            // linear output of this function. Negative degrees would require rational splines which this code
            // does not support.
            ArgumentOutOfRangeException.ThrowIfLessThan(degree, 1);

            // Spline fitting does not make sense when the input contains no points or just one point. In this case
            // the user likely wants this function to behave like a no-op.
            if (controlPoints.Length < 2)
                return controlPoints.Length == 0 ? new List<Vector2>() : new List<Vector2> { controlPoints[0] };

            // With fewer control points than the degree, splines can not be unambiguously fitted. Rather than erroring
            // out, we set the degree to the minimal number that permits a unique fit to avoid special casing in
            // incremental spline building algorithms that call this function.
            degree = Math.Min(degree, controlPoints.Length - 1);

            List<Vector2> output = new List<Vector2>();
            int pointCount = controlPoints.Length - 1;

            Stack<Vector2[]> toFlatten = bSplineToBezierInternal(controlPoints, ref degree);
            Stack<Vector2[]> freeBuffers = new Stack<Vector2[]>();

            // "toFlatten" contains all the curves which are not yet approximated well enough.
            // We use a stack to emulate recursion without the risk of running into a stack overflow.
            // (More specifically, we iteratively and adaptively refine our curve with a
            // <a href="https://en.wikipedia.org/wiki/Depth-first_search">Depth-first search</a>
            // over the tree resulting from the subdivisions we make.)

            var subdivisionBuffer1 = new Vector2[degree + 1];
            var subdivisionBuffer2 = new Vector2[degree * 2 + 1];

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
                    bezierApproximate(parent, output, subdivisionBuffer1, subdivisionBuffer2, degree + 1);

                    freeBuffers.Push(parent);
                    continue;
                }

                // If we do not yet have a sufficiently "flat" (in other words, detailed) approximation we keep
                // subdividing the curve we are currently operating on.
                Vector2[] rightChild = freeBuffers.Count > 0 ? freeBuffers.Pop() : new Vector2[degree + 1];
                bezierSubdivide(parent, leftChild, rightChild, subdivisionBuffer1, degree + 1);

                // We re-use the buffer of the parent for one of the children, so that we save one allocation per iteration.
                for (int i = 0; i < degree + 1; ++i)
                    parent[i] = leftChild[i];

                toFlatten.Push(rightChild);
                toFlatten.Push(parent);
            }

            output.Add(controlPoints[pointCount]);
            return output;
        }

        /// <summary>
        /// Creates a piecewise-linear approximation of a Catmull-Rom spline.
        /// </summary>
        /// <returns>A list of vectors representing the piecewise-linear approximation.</returns>
        public static List<Vector2> CatmullToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints)
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
        public static List<Vector2> CircularArcToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints)
        {
            CircularArcProperties pr = new CircularArcProperties(controlPoints);
            if (!pr.IsValid)
                return BezierToPiecewiseLinear(controlPoints);

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
        public static List<Vector2> LinearToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints)
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
        public static List<Vector2> LagrangePolynomialToPiecewiseLinear(ReadOnlySpan<Vector2> controlPoints)
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
        /// Creates a bezier curve approximation from a piecewise-linear path.
        /// </summary>
        /// <param name="inputPath">The piecewise-linear path to approximate.</param>
        /// <param name="numControlPoints">The number of control points to use in the bezier approximation.</param>
        /// <param name="numTestPoints">The number of points to evaluate the bezier path at for optimization, basically a resolution.</param>
        /// <param name="maxIterations">The number of optimization steps.</param>
        /// <param name="learningRate">The rate of optimization. Larger values converge faster but can be unstable.</param>
        /// <param name="b1">The B1 parameter for the Adam optimizer. Between 0 and 1.</param>
        /// <param name="b2">The B2 parameter for the Adam optimizer. Between 0 and 1.</param>
        /// <param name="initialControlPoints">The initial bezier control points to use before optimization. The length of this list should be equal to <paramref name="numControlPoints"/>.</param>
        /// <param name="learnableMask">Mask determining which control point positions are fixed and cannot be changed by the optimiser.</param>
        /// <returns>A List of vectors representing the bezier control points.</returns>
        public static List<Vector2> PiecewiseLinearToBezier(ReadOnlySpan<Vector2> inputPath,
                                                            int numControlPoints,
                                                            int numTestPoints = 100,
                                                            int maxIterations = 100,
                                                            float learningRate = 8f,
                                                            float b1 = 0.8f,
                                                            float b2 = 0.99f,
                                                            List<Vector2>? initialControlPoints = null,
                                                            float[,]? learnableMask = null)
        {
            numTestPoints = Math.Max(numTestPoints, 3);
            return piecewiseLinearToSpline(inputPath, generateBezierWeights(numControlPoints, numTestPoints),
                maxIterations, learningRate, b1, b2, initialControlPoints, learnableMask);
        }

        /// <summary>
        /// Creates a B-spline approximation from a piecewise-linear path.
        /// </summary>
        /// <param name="inputPath">The piecewise-linear path to approximate.</param>
        /// <param name="numControlPoints">The number of control points to use in the B-spline approximation.</param>
        /// <param name="degree">The polynomial order.</param>
        /// <param name="numTestPoints">The number of points to evaluate the B-spline path at for optimization, basically a resolution.</param>
        /// <param name="maxIterations">The number of optimization steps.</param>
        /// <param name="learningRate">The rate of optimization. Larger values converge faster but can be unstable.</param>
        /// <param name="b1">The B1 parameter for the Adam optimizer. Between 0 and 1.</param>
        /// <param name="b2">The B2 parameter for the Adam optimizer. Between 0 and 1.</param>
        /// <param name="initialControlPoints">The initial B-spline control points to use before optimization. The length of this list should be equal to <paramref name="numControlPoints"/>.</param>
        /// <param name="learnableMask">Mask determining which control point positions are fixed and cannot be changed by the optimiser.</param>
        /// <returns>A List of vectors representing the B-spline control points.</returns>
        public static List<Vector2> PiecewiseLinearToBSpline(ReadOnlySpan<Vector2> inputPath,
                                                             int numControlPoints,
                                                             int degree,
                                                             int numTestPoints = 100,
                                                             int maxIterations = 100,
                                                             float learningRate = 8f,
                                                             float b1 = 0.8f,
                                                             float b2 = 0.99f,
                                                             List<Vector2>? initialControlPoints = null,
                                                             float[,]? learnableMask = null)
        {
            degree = Math.Min(degree, numControlPoints - 1);
            numTestPoints = Math.Max(numTestPoints, 3);
            return piecewiseLinearToSpline(inputPath, generateBSplineWeights(numControlPoints, numTestPoints, degree),
                maxIterations, learningRate, b1, b2, initialControlPoints, learnableMask);
        }

        /// <summary>
        /// Creates an arbitrary spline approximation from a piecewise-linear path.
        /// Works for any spline type where the interpolation is a linear combination of the control points.
        /// </summary>
        /// <param name="inputPath">The piecewise-linear path to approximate.</param>
        /// <param name="weights">A 2D matrix that contains the spline basis functions at multiple positions. The length of the first dimension is the number of test points, and the length of the second dimension is the number of control points.</param>
        /// <param name="maxIterations">The number of optimization steps.</param>
        /// <param name="learningRate">The rate of optimization. Larger values converge faster but can be unstable.</param>
        /// <param name="b1">The B1 parameter for the Adam optimizer. Between 0 and 1.</param>
        /// <param name="b2">The B2 parameter for the Adam optimizer. Between 0 and 1.</param>
        /// <param name="initialControlPoints">The initial control points to use before optimization. The length of this list should be equal to the number of test points.</param>
        /// <param name="learnableMask">Mask determining which control point positions are fixed and cannot be changed by the optimiser.</param>
        /// <returns>A List of vectors representing the spline control points.</returns>
        private static List<Vector2> piecewiseLinearToSpline(ReadOnlySpan<Vector2> inputPath,
                                                             float[,] weights,
                                                             int maxIterations = 100,
                                                             float learningRate = 8f,
                                                             float b1 = 0.8f,
                                                             float b2 = 0.99f,
                                                             List<Vector2>? initialControlPoints = null,
                                                             float[,]? learnableMask = null)
        {
            int numControlPoints = weights.GetLength(1);
            int numTestPoints = weights.GetLength(0);

            // Generate transpose weight matrix
            float[,] weightsTranspose = new float[numControlPoints, numTestPoints];

            for (int i = 0; i < numControlPoints; i++)
            {
                for (int j = 0; j < numTestPoints; j++)
                {
                    weightsTranspose[i, j] = weights[j, i];
                }
            }

            // Create efficient interpolation on the input path
            var interpolator = new Interpolator(inputPath, numTestPoints);

            // Initialize control points
            float[,] labels = new float[2, numTestPoints];
            float[,] controlPoints = new float[2, numControlPoints];

            if (initialControlPoints is not null)
            {
                for (int i = 0; i < numControlPoints; i++)
                {
                    controlPoints[0, i] = initialControlPoints[i].X;
                    controlPoints[1, i] = initialControlPoints[i].Y;
                }
            }
            else
                // Create initial control point positions equally spaced along the input path
                interpolator.Interpolate(linspace(0, 1, numControlPoints), controlPoints);

            // Initialize Adam optimizer variables
            float[,] m = new float[2, numControlPoints];
            float[,] v = new float[2, numControlPoints];

            if (learnableMask is null)
            {
                learnableMask = new float[2, numControlPoints];

                for (int i = 1; i < numControlPoints - 1; i++)
                {
                    learnableMask[0, i] = 1;
                    learnableMask[1, i] = 1;
                }
            }

            // Initialize intermediate variables
            float[,] points = new float[2, numTestPoints];
            float[,] grad = new float[2, numControlPoints];
            float[] distanceDistribution = new float[numTestPoints];

            for (int step = 0; step < maxIterations; step++)
            {
                matmul(controlPoints, weights, points);

                // Update labels to shift the distance distribution between points
                if (step % 11 == 0)
                {
                    getDistanceDistribution(points, distanceDistribution, 0.1f);
                    interpolator.Interpolate(distanceDistribution, labels);
                }

                // Calculate the gradient on the control points
                matDiff(labels, points, points);
                matmul(points, weightsTranspose, grad);
                matScale(grad, -1f / numControlPoints, grad);

                // Apply learnable mask to prevent moving the fixed points
                matProduct(grad, learnableMask, grad);

                // Update control points with Adam optimizer
                matLerp(grad, m, b1, m);
                matProduct(grad, grad, grad);
                matLerp(grad, v, b2, v);
                adamUpdate(controlPoints, m, v, step, learningRate, b1, b2);
            }

            // Convert the resulting control points array
            var result = new List<Vector2>(numControlPoints);

            for (int i = 0; i < numControlPoints; i++)
            {
                result.Add(new Vector2(controlPoints[0, i], controlPoints[1, i]));
            }

            return result;
        }

        private static void adamUpdate(float[,] parameters, float[,] m, float[,] v, int step, float learningRate, float b1, float b2)
        {
            const float epsilon = 1E-8f;
            float mMult = 1 / (1 - MathF.Pow(b1, step + 1));
            float vMult = 1 / (1 - MathF.Pow(b2, step + 1));
            int m0 = m.GetLength(0);
            int m1 = m.GetLength(1);

            for (int i = 0; i < m0; i++)
            {
                for (int j = 0; j < m1; j++)
                {
                    float mCorr = m[i, j] * mMult;
                    float vCorr = v[i, j] * vMult;
                    parameters[i, j] -= learningRate * mCorr / (MathF.Sqrt(vCorr) + epsilon);
                }
            }
        }

        private static unsafe void matLerp(float[,] mat1, float[,] mat2, float t, float[,] result)
        {
            // mat1 can not be the same array as result, or it will not work correctly
            if (ReferenceEquals(mat1, result))
                throw new ArgumentException($"{nameof(mat1)} can not be the same array as {nameof(result)}.");

            fixed (float* mat1P = mat1, mat2P = mat2, resultP = result)
            {
                var span1 = new Span<float>(mat1P, mat1.Length);
                var span2 = new Span<float>(mat2P, mat2.Length);
                var spanR = new Span<float>(resultP, result.Length);
                TensorPrimitives.Multiply(span2, t, spanR);
                TensorPrimitives.MultiplyAdd(span1, 1 - t, spanR, spanR);
            }
        }

        private static unsafe void matProduct(float[,] mat1, float[,] mat2, float[,] result)
        {
            fixed (float* mat1P = mat1, mat2P = mat2, resultP = result)
            {
                var span1 = new Span<float>(mat1P, mat1.Length);
                var span2 = new Span<float>(mat2P, mat2.Length);
                var spanR = new Span<float>(resultP, result.Length);
                TensorPrimitives.Multiply(span1, span2, spanR);
            }
        }

        private static unsafe void matScale(float[,] mat, float scalar, float[,] result)
        {
            fixed (float* matP = mat, resultP = result)
            {
                var span1 = new Span<float>(matP, mat.Length);
                var spanR = new Span<float>(resultP, result.Length);
                TensorPrimitives.Multiply(span1, scalar, spanR);
            }
        }

        private static unsafe void matDiff(float[,] mat1, float[,] mat2, float[,] result)
        {
            fixed (float* mat1P = mat1, mat2P = mat2, resultP = result)
            {
                var span1 = new Span<float>(mat1P, mat1.Length);
                var span2 = new Span<float>(mat2P, mat2.Length);
                var spanR = new Span<float>(resultP, result.Length);
                TensorPrimitives.Subtract(span1, span2, spanR);
            }
        }

        // This matmul operation is not standard because it computes (m, p) * (n, p) -> (m, n)
        // This is because the memory for the reduced dimension must be contiguous
        private static unsafe void matmul(float[,] mat1, float[,] mat2, float[,] result)
        {
            int m = mat1.GetLength(0);
            int n = mat2.GetLength(0);
            int p = mat1.GetLength(1);

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    fixed (float* mat1P = mat1, mat2P = mat2)
                    {
                        var span1 = new Span<float>(mat1P + i * p, p);
                        var span2 = new Span<float>(mat2P + j * p, p);
                        result[i, j] = TensorPrimitives.Dot(span1, span2);
                    }
                }
            }
        }

        private static float[] linspace(float start, float end, int count)
        {
            float[] result = new float[count];

            for (int i = 0; i < count; i++)
            {
                result[i] = start + (end - start) * i / (count - 1);
            }

            return result;
        }

        /// <summary>
        /// Calculates a normalized cumulative distribution for the Euclidean distance between points on a piecewise-linear path.
        /// </summary>
        /// <param name="points">(2, n) shape array which represents the points of the piecewise-linear path.</param>
        /// <param name="result">n-length array to write the result to.</param>
        /// <param name="regularizingFactor">Factor to be added to each computed distance between points.</param>
        private static void getDistanceDistribution(float[,] points, float[] result, float regularizingFactor = 0f)
        {
            int m = points.GetLength(1);
            float accumulator = 0;
            result[0] = 0;

            for (int i = 1; i < m; i++)
            {
                float dist = MathF.Sqrt(MathF.Pow(points[0, i] - points[0, i - 1], 2) + MathF.Pow(points[1, i] - points[1, i - 1], 2));
                accumulator += dist + regularizingFactor;
                result[i] = accumulator;
            }

            var spanR = result.AsSpan();
            TensorPrimitives.Divide(spanR, accumulator, spanR);
        }

        private class Interpolator
        {
            private readonly int ny;
            private readonly float[,] ys;

            public Interpolator(ReadOnlySpan<Vector2> inputPath, int resolution = 1000)
            {
                float[,] arr = new float[2, inputPath.Length];

                for (int i = 0; i < inputPath.Length; i++)
                {
                    arr[0, i] = inputPath[i].X;
                    arr[1, i] = inputPath[i].Y;
                }

                float[] dist = new float[inputPath.Length];
                getDistanceDistribution(arr, dist);
                ny = resolution;
                ys = new float[resolution, 2];
                int current = 0;

                for (int i = 0; i < resolution; i++)
                {
                    float target = (float)i / (resolution - 1);

                    while (dist[current] < target)
                        current++;

                    int prev = Math.Max(0, current - 1);
                    float currDist = dist[current];
                    float prevDist = dist[prev];
                    float t = (currDist - target) / (currDist - prevDist);

                    if (float.IsNaN(t))
                        t = 0;

                    ys[i, 0] = t * arr[0, prev] + (1 - t) * arr[0, current];
                    ys[i, 1] = t * arr[1, prev] + (1 - t) * arr[1, current];
                }
            }

            public void Interpolate(float[] x, float[,] result)
            {
                int nx = x.Length;

                for (int i = 0; i < nx; i++)
                {
                    float idx = x[i] * (ny - 1);
                    int idxBelow = (int)idx;
                    int idxAbove = Math.Min(idxBelow + 1, ny - 1);
                    idxBelow = Math.Max(idxAbove - 1, 0);

                    float t = idx - idxBelow;

                    result[0, i] = t * ys[idxAbove, 0] + (1 - t) * ys[idxBelow, 0];
                    result[1, i] = t * ys[idxAbove, 1] + (1 - t) * ys[idxBelow, 1];
                }
            }
        }

        /// <summary>
        /// Calculate a matrix of B-spline basis function values.
        /// </summary>
        /// <param name="numControlPoints">The number of control points.</param>
        /// <param name="numTestPoints">The number of points to evaluate the spline at.</param>
        /// <param name="degree">The order of the B-spline.</param>
        /// <returns>Matrix array of B-spline basis function values.</returns>
        private static float[,] generateBSplineWeights(int numControlPoints, int numTestPoints, int degree)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(numControlPoints, 2);

            ArgumentOutOfRangeException.ThrowIfLessThan(numTestPoints, 2);

            if (degree < 0 || degree >= numControlPoints)
                throw new ArgumentOutOfRangeException(nameof(degree), $"{nameof(degree)} must be >=0 and <{nameof(numControlPoints)} but was {degree}.");

            // Calculate the basis function values using the Cox-de Boor recursion formula
            // Generate an open uniform knot vector from 0 to 1
            float[] x = linspace(0, 1, numTestPoints);
            float[] knots = new float[numControlPoints + degree + 1];

            for (int i = 0; i < degree; i++)
            {
                knots[i] = 0;
                knots[numControlPoints + degree - i] = 1;
            }

            for (int i = degree; i < numControlPoints + 1; i++)
            {
                knots[i] = (float)(i - degree) / (numControlPoints - degree);
            }

            // Calculate the first order basis
            float[,] prevOrder = new float[numTestPoints, numControlPoints];

            for (int i = 0; i < numTestPoints; i++)
            {
                prevOrder[i, (int)MathHelper.Clamp(x[i] * (numControlPoints - degree), 0, numControlPoints - degree - 1)] = 1;
            }

            // Calculate the higher order basis
            for (int q = 1; q < degree + 1; q++)
            {
                for (int i = 0; i < numTestPoints; i++)
                {
                    // This code multiplies the previous order by equal length arrays of alphas and betas,
                    // then shifts the alpha array by one index, and adds the results, resulting in one extra length.
                    // nextOrder = (prevOrder * alphas).shiftRight() + (prevOrder * betas)
                    float prevAlpha = 0;

                    for (int j = 0; j < numControlPoints - degree + q - 1; j++)
                    {
                        float alpha = (x[i] - knots[degree - q + 1 + j]) / (knots[degree + 1 + j] - knots[degree - q + 1 + j]);
                        float alphaVal = alpha * prevOrder[i, j];
                        float betaVal = (1 - alpha) * prevOrder[i, j];
                        prevOrder[i, j] = prevAlpha + betaVal;
                        prevAlpha = alphaVal;
                    }

                    prevOrder[i, numControlPoints - degree + q - 1] = prevAlpha;
                }
            }

            return prevOrder;
        }

        private static float[,] generateBezierWeights(int numControlPoints, int numTestPoints)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(numControlPoints, 2);
            ArgumentOutOfRangeException.ThrowIfLessThan(numTestPoints, 2);

            long[] coefficients = binomialCoefficients(numControlPoints - 1);
            float[,] p = new float[numTestPoints, numControlPoints];

            for (int i = 0; i < numTestPoints; i++)
            {
                p[i, 0] = 1;
                float t = (float)i / (numTestPoints - 1);

                for (int j = 1; j < numControlPoints; j++)
                {
                    p[i, j] = p[i, j - 1] * t;
                }
            }

            float[,] result = new float[numTestPoints, numControlPoints];

            for (int i = 0; i < numTestPoints; i++)
            {
                for (int j = 0; j < numControlPoints; j++)
                {
                    result[i, j] = coefficients[j] * p[i, j] * p[numTestPoints - i - 1, numControlPoints - j - 1];
                }
            }

            return result;
        }

        /// <summary>
        /// Computes an array with all binomial coefficients from 0 to n inclusive.
        /// </summary>
        /// <returns>n+1 length array with the binomial coefficients.</returns>
        private static long[] binomialCoefficients(int n)
        {
            long[] coefficients = new long[n + 1];
            coefficients[0] = 1;

            for (int i = 1; i < (n + 2) / 2; i++)
            {
                coefficients[i] = coefficients[i - 1] * (n + 1 - i) / i;
            }

            for (int i = n; i > n / 2; i--)
            {
                coefficients[i] = coefficients[n - i];
            }

            return coefficients;
        }

        private static Stack<Vector2[]> bSplineToBezierInternal(ReadOnlySpan<Vector2> controlPoints, ref int degree)
        {
            Stack<Vector2[]> result = new Stack<Vector2[]>();

            // With fewer control points than the degree, splines can not be unambiguously fitted. Rather than erroring
            // out, we set the degree to the minimal number that permits a unique fit to avoid special casing in
            // incremental spline building algorithms that call this function.
            degree = Math.Min(degree, controlPoints.Length - 1);

            int pointCount = controlPoints.Length - 1;
            var points = controlPoints.ToArray();

            if (degree == pointCount)
            {
                // B-spline subdivision unnecessary, degenerate to single bezier.
                result.Push(points);
            }
            else
            {
                // Subdivide B-spline into bezier control points at knots.
                for (int i = 0; i < pointCount - degree; i++)
                {
                    var subBezier = new Vector2[degree + 1];
                    subBezier[0] = points[i];

                    // Destructively insert the knot degree-1 times via Boehm's algorithm.
                    for (int j = 0; j < degree - 1; j++)
                    {
                        subBezier[j + 1] = points[i + 1];

                        for (int k = 1; k < degree - j; k++)
                        {
                            int l = Math.Min(k, pointCount - degree - i);
                            points[i + k] = (l * points[i + k] + points[i + k + 1]) / (l + 1);
                        }
                    }

                    subBezier[degree] = points[i + 1];
                    result.Push(subBezier);
                }

                result.Push(points[(pointCount - degree)..]);
                // Reverse the stack so elements can be accessed in order.
                result = new Stack<Vector2[]>(result);
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
                if ((controlPoints[i - 1] - 2 * controlPoints[i] + controlPoints[i + 1]).LengthSquared > BEZIER_TOLERANCE * BEZIER_TOLERANCE * 4)
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
