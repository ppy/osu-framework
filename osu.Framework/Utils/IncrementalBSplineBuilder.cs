// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Caching;
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

        private Vector2 getPathAt(List<Vector2> path, List<float> cumulativeDistances, float t)
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

        public const float FdEpsilon = PathApproximator.BEZIER_TOLERANCE * 8f;

        /// <summary>
        /// Get the tangent at a given point on the path.
        /// </summary>
        /// <param name="path">The path to get the tangent from.</param>
        /// <param name="cumulativeDistances">The cumulative distances of the path.</param>
        /// <param name="t">The point on the path to get the tangent from.</param>
        /// <returns>The tangent at the given point on the path.</returns>
        private Vector2 getTangentAt(List<Vector2> path, List<float> cumulativeDistances, float t)
        {
            Vector2 xminus = getPathAt(path, cumulativeDistances, t - FdEpsilon);
            Vector2 xplus = getPathAt(path, cumulativeDistances, t + FdEpsilon);

            return xplus == xminus ? Vector2.Zero : (xplus - xminus).Normalized();
        }

        /// <summary>
        /// Get the amount of rotation (in radians) at a given point on the path.
        /// </summary>
        /// <param name="path">The path to get the rotation from.</param>
        /// <param name="cumulativeDistances">The cumulative distances of the path.</param>
        /// <param name="t">The point on the path to get the rotation from.</param>
        /// <returns>The amount of rotation (in radians) at the given point on the path.</returns>
        private float getWindingAt(List<Vector2> path, List<float> cumulativeDistances, float t)
        {
            Vector2 xminus = getPathAt(path, cumulativeDistances, t - FdEpsilon);
            Vector2 x = getPathAt(path, cumulativeDistances, t);
            Vector2 xplus = getPathAt(path, cumulativeDistances, t + FdEpsilon);
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
        /// Gets or sets the corner threshold for determining when to add a new control point. Must not be negative. Default is 0.1.
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
        ///
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
            var controlPoints = new Vector2[(int)(inputPathLength / FdEpsilon)];
            for (int i = 0; i < controlPoints.Length; ++i)
                controlPoints[i] = getPathAt(inputPath, cumulativeInputPathLength, i * FdEpsilon);

            // Empirically, degree 7 works really well as a good tradeoff for smoothing vs sharpness here.
            int smoothedInputPathDegree = 7;
            var vertices = PathApproximator.BSplineToPiecewiseLinear(controlPoints, smoothedInputPathDegree);
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
            var corners = new List<Vector2>();
            var cornerT = new List<float> { 0f };

            float threshold = cornerThreshold / FdEpsilon;

            float stepSize = FdEpsilon;
            int nSteps = (int)(distances[^1] / stepSize);

            // Empirically, averaging the winding rate over a neighborgood of 32 samples seems to be
            // a good representation of the neighborhood of the curve.
            const int nAvgSamples = 32;
            float avgCurvature = 0.0f;

            for (int i = 0; i < nSteps; ++i)
            {
                // Update average curvature by adding the new winding rate and subtracting the old one from
                // nAvgSamples steps ago.
                float newt = i * stepSize;
                float newWinding = MathF.Abs(getWindingAt(vertices, distances, newt));

                float oldt = (i - nAvgSamples) * stepSize;
                float oldWinding = oldt < 0 ? 0 : MathF.Abs(getWindingAt(vertices, distances, oldt));

                avgCurvature = avgCurvature + (newWinding - oldWinding) / nAvgSamples;

                // Check whether the current winding rate is a local maximum and whether it exceeds the
                // threshold as well as the surrounding average curvature. If so, we have found a corner.
                // Also prohibit marking new corners that are too close to the previous one.
                float midt = (i - nAvgSamples / 2f) * stepSize;
                float midWinding = midt < 0 ? 0 : MathF.Abs(getWindingAt(vertices, distances, midt));

                float distToPrevCorner = cornerT.Count == 0 ? float.MaxValue : newt - cornerT[^1];
                if (midWinding > threshold && midWinding > avgCurvature * 4 && distToPrevCorner > nAvgSamples * stepSize)
                    cornerT.Add(midt);
            }

            // The end of the path is by definition a corner
            cornerT.Add(distances[^1]);
            return cornerT;
        }

        private void regenerateApproximatedPathControlPoints() {
            // Approximating a given input path with a BSpline has three stages:
            //  1. Fit a dense-ish BSpline (with one control point in FdEpsilon-sized intervals) to the input path.
            //     The purpose of this dense BSpline is an initial smoothening that permits reliable curvature
            //     analysis in the next steps.
            //  2. Detect corners by thresholding local curvature maxima and place sharp control points at these corners.
            //  3. Place additional control points inbetween the sharp ones with density proportional to the product
            //     of Tolerance and curvature.
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
            float stepSize = FdEpsilon;
            for (int i = 1; i < cornerTs.Count; ++i)
            {
                float totalAngle = 0;

                float t0 = cornerTs[i - 1] + stepSize * 2;
                float t1 = cornerTs[i] - stepSize * 2;

                if (t1 > t0)
                {
                    int nSteps = (int)((t1 - t0) / stepSize);
                    for (int j = 0; j < nSteps; ++j)
                    {
                        float t = t0 + j * stepSize;
                        totalAngle += getWindingAt(vertices, distances, t);
                    }

                    int nControlPoints = (int)(totalAngle / Tolerance);
                    float controlPointSpacing = totalAngle / nControlPoints;
                    float currentAngle = 0;
                    for (int j = 0; j < nSteps; ++j)
                    {
                        float t = t0 + j * stepSize;
                        if (currentAngle > controlPointSpacing)
                        {
                            cps.Add(getPathAt(vertices, distances, t));
                            currentAngle -= controlPointSpacing;
                        }

                        currentAngle += getWindingAt(vertices, distances, t);
                    }
                }

                // Insert the corner at the end of the segment as a sharp control point consisting of
                // degree many regular control points, meaning that the BSpline will have a kink here.
                // Special case the last corner which will be the end of the path and thus automatically
                // duplicated degree times by BSplineToPiecewiseLinear down the line.
                Vector2 corner = getPathAt(vertices, distances, cornerTs[i]);
                if (i == cornerTs.Count - 1) {
                    cps.Add(corner);
                } else {
                    for (int j = 0; j < degree; ++j)
                        cps.Add(corner);
                }
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
            if (inputDistance < FdEpsilon)
                return;

            inputPath.Add(v);
            cumulativeInputPathLength.Add((cumulativeInputPathLength.Count == 0 ? 0 : cumulativeInputPathLength[^1]) + inputDistance);
        }
    }
}
