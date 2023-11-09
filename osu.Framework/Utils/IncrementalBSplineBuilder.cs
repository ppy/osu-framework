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

        private readonly Cached<List<Vector2>> outputCache = new Cached<List<Vector2>>
        {
            Value = new List<Vector2>()
        };

        private readonly List<Vector2> controlPoints = new List<Vector2>();

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
            }
        }

        private float tolerance;

        /// <summary>
        /// Gets or sets the tolerance for determining when to add a new control point. Must not be negative. Default is 0.1.
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
        /// Initializes a new instance of the <see cref="IncrementalBSplineBuilder"/> class with specified degree and tolerance.
        /// </summary>
        /// <param name="degree">The degree of the B-Spline.</param>
        /// <param name="tolerance">The tolerance for control point addition.</param>
        public IncrementalBSplineBuilder(int degree = 3, float tolerance = 0.1f)
        {
            Degree = degree;
            Tolerance = tolerance;
        }

        /// <summary>
        /// The list of control points of the B-Spline. This is inferred from the input path.
        /// </summary>
        public IReadOnlyList<Vector2> GetControlPoints()
            => controlPoints.ToArray();

        /// <summary>
        /// The list of input points.
        /// </summary>
        public IReadOnlyList<Vector2> GetInputPath()
            => inputPath.ToArray();

        private void redrawApproximatedPath()
        {
            // Set value of output cache to update the cache to be valid.
            outputCache.Value = new List<Vector2>();
            if (inputPath.Count == 0)
                return;

            var oldInputs = inputPath.ToList();

            inputPath.Clear();
            controlPoints.Clear();

            foreach (var v in oldInputs)
                AddLinearPoint(v);
        }

        /// <summary>
        /// Clears the input path and the B-Spline.
        /// </summary>
        public void Clear()
        {
            inputPath.Clear();
            controlPoints.Clear();
            if (outputCache.IsValid)
                outputCache.Value.Clear();
        }

        /// <summary>
        /// Adds a linear point to the path and updates the B-Spline accordingly.
        /// </summary>
        /// <param name="v">The vector representing the point to add.</param>
        public void AddLinearPoint(Vector2 v)
        {
            if (!outputCache.IsValid)
                redrawApproximatedPath();

            if (inputPath.Count == 0)
            {
                inputPath.Add(v);
                controlPoints.Add(inputPath[0]);
                controlPoints.Add(inputPath[0]);
                return;
            }

            inputPath.Add(v);

            var cps = controlPoints;
            Debug.Assert(cps.Count >= 2);

            cps[^1] = inputPath[^1];

            // Calculate the normalized momentum vectors for both raw and approximated paths.
            // Momentum here refers to a direction vector representing the path's direction of movement.
            var mraw = momentumDirection(inputPath, 3);
            var mcp = cps.Count > 2 ? momentumDirection(outputCache.Value, 1) : Vector2.Zero;

            // Determine the alignment between the raw path and control path's momentums.
            // It uses Vector2.Dot which calculates the cosine of the angle between two vectors.
            // This alignment is used to adjust the control points based on the path's direction change.
            float alignment = MathF.Max(Vector2.Dot(mraw, mcp), 0.01f);

            // Calculate the distance between the last two control points.
            // This distance is then used, along with alignment, to decide if a new control point is needed.
            // The threshold for adding a new control point is based on the alignment and a predefined accuracy factor.
            float distance = Vector2.Distance(cps[^1], cps[^2]);
            if (distance / MathF.Pow(alignment, 4) > Tolerance * 1000)
                cps.Add(cps[^1]);

            outputCache.Value = PathApproximator.BSplineToPiecewiseLinear(cps.ToArray(), degree);
        }

        private Vector2 momentumDirection(IReadOnlyList<Vector2> vertices, int window)
        {
            if (vertices.Count < window + 1)
                return Vector2.Zero;

            var sum = Vector2.Zero;
            for (int i = 0; i < window; i++)
                sum += vertices[^(i + 1)] - vertices[^(i + 2)];

            if (Precision.AlmostEquals(sum.X, 0, 1e-7f) && Precision.AlmostEquals(sum.Y, 0, 1e-7f))
                return Vector2.Zero;

            return (sum / window).Normalized();
        }
    }
}
