// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace osu.Framework.Utils
{
    /// <summary>
    /// A class for incrementally building B-Spline paths from a series of linear points.
    /// This can be used to obtain a B-Spline with a minimal number of control points for any
    /// given set of linear input points, for example, a hand-drawn path.
    /// </summary>
    public class IncrementalBSplineBuilder
    {
        private readonly List<Vector2> inputPath = new List<Vector2>();
        private readonly List<float> cumulativeInputPathLength = new List<float>();

        private static Vector2 getPathAt(List<Vector2> path, List<float> cumulativeDistances, float t)
        {
            if (path.Count == 0)
                throw new InvalidOperationException("Input path is empty.");
            else if (path.Count == 1)
                return path[0];

            if (t <= 0)
                return path[0];

            if (t >= cumulativeDistances[^1])
                return path[^1];

            int index = cumulativeDistances.BinarySearch(t);
            if (index < 0)
                index = ~index;

            float lengthBefore = index == 0 ? 0 : cumulativeDistances[index - 1];
            float lengthAfter = cumulativeDistances[index];
            float segmentLength = lengthAfter - lengthBefore;
            float segmentT = (t - lengthBefore) / segmentLength;

            return Vector2.Lerp(path[index], path[index + 1], segmentT);
        }

        private float inputPathLength => cumulativeInputPathLength.Count == 0 ? 0 : cumulativeInputPathLength[^1];

        /// <summary>
        /// Spacing to use in spline-related finite difference (FD) calculations.
        /// </summary>
        internal const float FD_EPSILON = PathApproximator.BEZIER_TOLERANCE * 8f;

        /// <summary>
        /// Get the absolute amount of rotation (in radians) at a given point on the path.
        /// </summary>
        /// <param name="path">The path to get the rotation from.</param>
        /// <param name="cumulativeDistances">The cumulative distances of the path.</param>
        /// <param name="t">The point on the path to get the rotation from.</param>
        /// <returns>The absolute amount of rotation (in radians) at the given point on the path.</returns>
        private static float getAbsWindingAt(List<Vector2> path, List<float> cumulativeDistances, float t)
        {
            Vector2 xminus = getPathAt(path, cumulativeDistances, t - FD_EPSILON);
            Vector2 x = getPathAt(path, cumulativeDistances, t);
            Vector2 xplus = getPathAt(path, cumulativeDistances, t + FD_EPSILON);
            Vector2 tminus = x == xminus ? Vector2.Zero : (x - xminus).Normalized();
            Vector2 tplus = xplus == x ? Vector2.Zero : (xplus - x).Normalized();
            return MathF.Abs(MathF.Acos(Math.Clamp(Vector2.Dot(tminus, tplus), -1f, 1f)));
        }

        private readonly Cached<List<Vector2>> outputCache = new Cached<List<Vector2>>
        {
            Value = new List<Vector2>()
        };

        private readonly Cached<List<Vector2>> controlPoints = new Cached<List<Vector2>>
        {
            Value = new List<Vector2>()
        };

        private int degree;

        /// <summary>
        /// Gets or sets the degree of the B-Spline. Must not be negative. Default is 3.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
        public int Degree
        {
            get => degree;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Degree must not be negative.");

                degree = value;
                outputCache.Invalidate();
                controlPoints.Invalidate();
            }
        }

        private float tolerance;

        /// <summary>
        /// Gets or sets the tolerance for determining when to add a new control point. Must not be negative. Default is 1.5.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
        public float Tolerance
        {
            get => tolerance;
            set
            {
                if (tolerance < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Tolerance must not be negative.");

                tolerance = value;
                outputCache.Invalidate();
                controlPoints.Invalidate();
            }
        }

        private float cornerThreshold;

        /// <summary>
        /// Gets or sets the corner threshold for determining when to add a new control point. Must not be negative. Default is 0.4.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the value is negative.</exception>
        public float CornerThreshold
        {
            get => cornerThreshold;
            set
            {
                if (cornerThreshold < 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "CornerThreshold must not be negative.");

                cornerThreshold = value;
                outputCache.Invalidate();
                controlPoints.Invalidate();
            }
        }

        /// <summary>
        /// The piecewise linear approximation of the B-spline created from the input path.
        /// </summary>
        public IReadOnlyList<Vector2> OutputPath
        {
            get
            {
                if (!outputCache.IsValid)
                    redrawApproximatedPath();

                return outputCache.Value;
            }
        }

        /// <summary>
        /// The list of control points of the B-Spline. This is inferred from the input path.
        /// </summary>
        public IReadOnlyList<Vector2> ControlPoints
        {
            get
            {
                if (!controlPoints.IsValid)
                    regenerateApproximatedPathControlPoints();

                return controlPoints.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IncrementalBSplineBuilder"/> class with specified degree and tolerance.
        /// </summary>
        /// <param name="degree">The degree of the B-Spline.</param>
        /// <param name="tolerance">The tolerance for control point addition.</param>
        /// <param name="cornerThreshold">The threshold to use for inserting sharp control points at corners.</param>
        public IncrementalBSplineBuilder(int degree = 3, float tolerance = 1.5f, float cornerThreshold = 0.4f)
        {
            Degree = degree;
            Tolerance = tolerance;
            CornerThreshold = cornerThreshold;
        }

        /// <summary>
        /// The list of input points.
        /// </summary>
        public IReadOnlyList<Vector2> GetInputPath()
            => inputPath.ToArray();

        /// <summary>
        /// Computes a smoothed version of the input path by generating a high-degree BSpline from densely
        /// spaces samples of the input path.
        /// </summary>
        /// <returns>A tuple containing the smoothed vertices and the cumulative distances of the smoothed path.</returns>
        private (List<Vector2> vertices, List<float> distances) computeSmoothedInputPath()
        {
            var cps = new Vector2[(int)(inputPathLength / FD_EPSILON)];
            for (int i = 0; i < cps.Length; ++i)
                cps[i] = getPathAt(inputPath, cumulativeInputPathLength, i * FD_EPSILON);

            // Empirically, degree 7 works really well as a good tradeoff for smoothing vs sharpness here.
            const int smoothed_input_path_degree = 7;
            var vertices = PathApproximator.BSplineToPiecewiseLinear(cps, smoothed_input_path_degree);
            var distances = new List<float>();
            float cumulativeLength = 0;

            for (int i = 1; i < vertices.Count; ++i)
            {
                cumulativeLength += Vector2.Distance(vertices[i], vertices[i - 1]);
                distances.Add(cumulativeLength);
            }

            return (vertices, distances);
        }

        /// <summary>
        /// Detects corners in the input path by thresholding how much the path curves and checking
        /// whether this curvature is local (i.e. a corner rather than a smooth, yet tight turn).
        /// </summary>
        /// <param name="vertices">The vertices of the input path.</param>
        /// <param name="distances">The cumulative distances of the input path.</param>
        /// <returns>A list of t values at which corners occur.</returns>
        private List<float> detectCorners(List<Vector2> vertices, List<float> distances)
        {
            var cornerT = new List<float> { 0f };

            float threshold = cornerThreshold / FD_EPSILON;

            const float step_size = FD_EPSILON;
            int nSteps = (int)(distances[^1] / step_size);

            // Empirically, averaging the winding rate over a neighborhood of 32 samples seems to be
            // a good representation of the neighborhood of the curve.
            const int n_avg_samples = 32;
            float avgCurvature = 0.0f;

            for (int i = 0; i < nSteps; ++i)
            {
                // Update average curvature by adding the new winding rate and subtracting the old one from
                // nAvgSamples steps ago.
                float newt = i * step_size;
                float newWinding = getAbsWindingAt(vertices, distances, newt);

                float oldt = (i - n_avg_samples) * step_size;
                float oldWinding = oldt < 0 ? 0 : getAbsWindingAt(vertices, distances, oldt);

                avgCurvature += (newWinding - oldWinding) / n_avg_samples;

                // Check whether the current winding rate is a local maximum and whether it exceeds the
                // threshold as well as the surrounding average curvature. If so, we have found a corner.
                // Also prohibit marking new corners that are too close to the previous one, where "too close"
                // is defined as the averaging windows overlapping. This ensures the same corner can not
                // be detected twice.
                float midt = (i - n_avg_samples / 2f) * step_size;
                float midWinding = midt < 0 ? 0 : getAbsWindingAt(vertices, distances, midt);

                float distToPrevCorner = cornerT.Count == 0 ? float.MaxValue : newt - cornerT[^1];
                if (midWinding > threshold && midWinding > avgCurvature * 4 && distToPrevCorner > n_avg_samples * step_size)
                    cornerT.Add(midt);
            }

            // The end of the path is by definition a corner
            cornerT.Add(distances[^1]);
            return cornerT;
        }

        private void regenerateApproximatedPathControlPoints()
        {
            // Approximating a given input path with a BSpline has three stages:
            //  1. Fit a dense-ish BSpline (with one control point in FdEpsilon-sized intervals) to the input path.
            //     The purpose of this dense BSpline is an initial smoothening that permits reliable curvature
            //     analysis in the next steps.
            //  2. Detect corners by thresholding local curvature maxima and place sharp control points at these corners.
            //  3. Place additional control points inbetween the sharp ones with density proportional to the product
            //     of Tolerance and curvature.
            //  4. Additionally, we special case linear segments: if the path does not deviate more
            //     than some threshold from a straight line, we do not add additional control points.
            var (vertices, distances) = computeSmoothedInputPath();

            if (vertices.Count < 2)
            {
                controlPoints.Value = vertices;
                return;
            }

            controlPoints.Value = new List<Vector2>();

            Debug.Assert(vertices.Count == distances.Count + 1);
            var cornerTs = detectCorners(vertices, distances);

            var cps = controlPoints.Value;
            cps.Add(vertices[0]);

            // Populate each segment between corners with control points that have density proportional to the
            // product of Tolerance and curvature.
            const float step_size = FD_EPSILON;

            for (int i = 1; i < cornerTs.Count; ++i)
            {
                float totalWinding = 0;

                float t0 = cornerTs[i - 1] + step_size * 2;
                float t1 = cornerTs[i] - step_size * 2;

                Vector2 c0 = getPathAt(vertices, distances, cornerTs[i - 1]);
                Vector2 c1 = getPathAt(vertices, distances, cornerTs[i]);
                Line linearConnection = new Line(c0, c1);

                var tmp = new List<Vector2>();
                bool allOnLine = true;
                float on_line_threshold = 5 * Tolerance * step_size;

                if (t1 > t0)
                {
                    int nSteps = (int)((t1 - t0) / step_size);

                    for (int j = 0; j < nSteps; ++j)
                    {
                        float t = t0 + j * step_size;
                        totalWinding += getAbsWindingAt(vertices, distances, t);
                    }

                    int nControlPoints = (int)(totalWinding / Tolerance);
                    float controlPointSpacing = totalWinding / nControlPoints;
                    float currentWinding = 0;

                    for (int j = 0; j < nSteps; ++j)
                    {
                        float t = t0 + j * step_size;

                        if (currentWinding > controlPointSpacing)
                        {
                            Vector2 p = getPathAt(vertices, distances, t);
                            if (linearConnection.DistanceSquaredToPoint(p) > on_line_threshold * on_line_threshold)
                                allOnLine = false;

                            tmp.Add(p);
                            currentWinding -= controlPointSpacing;
                        }

                        currentWinding += getAbsWindingAt(vertices, distances, t);
                    }
                }

                if (!allOnLine)
                    cps.AddRange(tmp);

                // Insert the corner at the end of the segment as a sharp control point consisting of
                // degree many regular control points, meaning that the BSpline will have a kink here.
                // Special case the last corner which will be the end of the path and thus automatically
                // duplicated degree times by BSplineToPiecewiseLinear down the line.
                if (i == cornerTs.Count - 1)
                    cps.Add(c1);
                else
                    cps.AddRange(Enumerable.Repeat(c1, degree));
            }
        }

        private void redrawApproximatedPath()
        {
            outputCache.Value = PathApproximator.BSplineToPiecewiseLinear(ControlPoints.ToArray(), degree);
        }

        /// <summary>
        /// Clears the input path and the B-Spline.
        /// </summary>
        public void Clear()
        {
            inputPath.Clear();
            cumulativeInputPathLength.Clear();

            controlPoints.Value = new List<Vector2>();
            outputCache.Value = new List<Vector2>();
        }

        /// <summary>
        /// Adds a linear point to the path and updates the B-Spline accordingly.
        /// </summary>
        /// <param name="v">The vector representing the point to add.</param>
        public void AddLinearPoint(Vector2 v)
        {
            outputCache.Invalidate();
            controlPoints.Invalidate();

            if (inputPath.Count == 0)
            {
                inputPath.Add(v);
                return;
            }

            float inputDistance = Vector2.Distance(v, inputPath[^1]);
            if (inputDistance < FD_EPSILON * 2)
                return;

            inputPath.Add(v);
            cumulativeInputPathLength.Add((cumulativeInputPathLength.Count == 0 ? 0 : cumulativeInputPathLength[^1]) + inputDistance);
        }
    }
}
