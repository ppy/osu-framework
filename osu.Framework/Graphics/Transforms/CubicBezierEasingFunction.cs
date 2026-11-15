// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// An easing function that creates a smooth transition using a cubic <see href="https://developer.mozilla.org/en-US/docs/Glossary/Bezier_curve">bezier curve</see>.
    /// </summary>
    public readonly struct CubicBezierEasingFunction : IEasingFunction
    {
        public readonly double X1;
        public readonly double Y1;
        public readonly double X2;
        public readonly double Y2;

        /// <param name="x1">x position of the first control point. Must be in 0-1 range</param>
        /// <param name="y1">y position of the first control point</param>
        /// <param name="x2">x position of the second control point. Must be in 0-1 range</param>
        /// <param name="y2">y position of the second control point</param>
        public CubicBezierEasingFunction(double x1, double y1, double x2, double y2)
        {
            if (Precision.DefinitelyBigger(0, x1) || Precision.DefinitelyBigger(x1, 1))
                throw new ArgumentOutOfRangeException(nameof(x1), "Must be within [0, 1] range.");

            if (Precision.DefinitelyBigger(0, x2) || Precision.DefinitelyBigger(x2, 1))
                throw new ArgumentOutOfRangeException(nameof(x2), "Must be within [0, 1] range.");

            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;
        }

        /// <param name="p1">position of the first control point.</param>
        /// <param name="p2">position of the second control point.</param>
        /// <remarks>
        /// The x psoition of both control points must be in 0-1 range.
        /// </remarks>
        public CubicBezierEasingFunction(Vector2 p1, Vector2 p2)
            : this(p1.X, p1.Y, p2.X, p2.Y)
        {
        }

        /// <summary>
        /// Constructs an easing function with initializes <see cref="Y1"/> to 0 and <see cref="Y2"/> to 1, which will perfectly flatten out the curve on both ends.
        /// </summary>
        /// <param name="easeIn">ease-in strength, in 0-1 range</param>
        /// <param name="easeOut">ease-out strength, in 0-1 range</param>
        public CubicBezierEasingFunction(double easeIn, double easeOut)
            : this(easeIn, 0, 1 - easeOut, 1)
        {
        }

        private static double evaluateBezier(double t, double a1, double a2) => (((1 - 3 * a2 + 3 * a1) * t + (3 * a2 - 6 * a1)) * t + 3 * a1) * t;

        private static double findTForX(double time, double x1, double x2)
        {
            double left = 0.0, right = 1.0, currentT = 0;

            for (int i = 0; i < 100; i++)
            {
                currentT = left + (right - left) / 2;
                double currentX = evaluateBezier(currentT, x1, x2) - time;

                if (currentX > 0)
                    right = currentT;
                else
                    left = currentT;

                if (Math.Abs(currentX) <= 0.0000001)
                    break;
            }

            return currentT;
        }

        public double ApplyEasing(double time) => evaluateBezier(findTForX(time, X1, X2), Y1, Y2);
    }
}
