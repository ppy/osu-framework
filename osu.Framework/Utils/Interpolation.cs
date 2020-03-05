// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Transforms;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Utils
{
    public static class Interpolation
    {
        public static double Lerp(double start, double final, double amount) => start + (final - start) * amount;

        /// <summary>
        /// Interpolates between 2 values (start and final) using a given base and exponent.
        /// </summary>
        /// <param name="start">The start value.</param>
        /// <param name="final">The end value.</param>
        /// <param name="base">The base of the exponential. The valid range is [0, 1], where smaller values mean that the final value is achieved more quickly, and values closer to 1 results in slow convergence to the final value.</param>
        /// <param name="exponent">The exponent of the exponential. An exponent of 0 results in the start values, whereas larger exponents make the result converge to the final value.</param>
        /// <returns></returns>
        public static double Damp(double start, double final, double @base, double exponent)
        {
            if (@base < 0 || @base > 1)
                throw new ArgumentOutOfRangeException(nameof(@base), $"{nameof(@base)} has to lie in [0,1], but is {@base}.");
            if (exponent < 0)
                throw new ArgumentOutOfRangeException(nameof(exponent), $"{nameof(exponent)} has to be bigger than 0, but is {exponent}.");

            return Lerp(start, final, 1 - Math.Pow(@base, exponent));
        }

        /// <summary>
        /// Interpolates between a set of points using a lagrange polynomial.
        /// </summary>
        /// <param name="points">An array of coordinates. No two x should be the same.</param>
        /// <param name="time">The x coordinate to calculate the y coordinate for.</param>
        public static double Lagrange(ReadOnlySpan<Vector2> points, double time)
        {
            if (points == null || points.Length == 0)
                throw new ArgumentException($"{nameof(points)} must contain at least one point");

            double sum = 0;
            for (int i = 0; i < points.Length; i++)
                sum += points[i].Y * LagrangeBasis(points, i, time);
            return sum;
        }

        /// <summary>
        /// Calculates the Lagrange basis polynomial for a given set of x coordinates. Used as a helper function to compute Lagrange polynomials.
        /// </summary>
        /// <param name="points">An array of coordinates. No two x should be the same.</param>
        /// <param name="base">The index inside the coordinate array which polynomial to compute.</param>
        /// <param name="time">The x coordinate to calculate the basis polynomial for.</param>
        public static double LagrangeBasis(ReadOnlySpan<Vector2> points, int @base, double time)
        {
            double product = 1;

            for (int i = 0; i < points.Length; i++)
            {
                if (i != @base)
                    product *= (time - points[i].X) / (points[@base].X - points[i].X);
            }

            return product;
        }

        /// <summary>
        /// Calculates the Barycentric weights for a Lagrange polynomial for a given set of coordinates. Can be used as a helper function to compute a Lagrange polynomial repeatedly.
        /// </summary>
        /// <param name="points">An array of coordinates. No two x should be the same.</param>
        public static double[] BarycentricWeights(ReadOnlySpan<Vector2> points)
        {
            int n = points.Length;
            double[] w = new double[n];

            for (int i = 0; i < n; i++)
            {
                w[i] = 1;

                for (int j = 0; j < n; j++)
                {
                    if (i != j)
                        w[i] *= points[i].X - points[j].X;
                }

                w[i] = 1.0 / w[i];
            }

            return w;
        }

        /// <summary>
        /// Calculates the Lagrange basis polynomial for a given set of x coordinates based on previously computed barycentric weights.
        /// </summary>
        /// <param name="points">An array of coordinates. No two x should be the same.</param>
        /// <param name="weights">An array of precomputed barycentric weights.</param>
        /// <param name="time">The x coordinate to calculate the basis polynomial for.</param>
        public static double BarycentricLagrange(ReadOnlySpan<Vector2> points, double[] weights, double time)
        {
            if (points == null || points.Length == 0)
                throw new ArgumentException($"{nameof(points)} must contain at least one point");
            if (points.Length != weights.Length)
                throw new ArgumentException($"{nameof(points)} must contain exactly as many items as {nameof(weights)}");

            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < points.Length; i++)
            {
                // while this is not great with branch prediction, it prevents NaN at control point X coordinates
                if (time == points[i].X)
                    return points[i].Y;

                double li = weights[i] / (time - points[i].X);
                numerator += li * points[i].Y;
                denominator += li;
            }

            return numerator / denominator;
        }

        #region ValueAt

        public static ColourInfo ValueAt(double time, ColourInfo startColour, ColourInfo endColour, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, startColour, endColour, startTime, endTime, new DefaultEasingFunction(easing));

        public static EdgeEffectParameters ValueAt(double time, EdgeEffectParameters startParams, EdgeEffectParameters endParams, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, startParams, endParams, startTime, endTime, new DefaultEasingFunction(easing));

        public static SRGBColour ValueAt(double time, SRGBColour startColour, SRGBColour endColour, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, startColour, endColour, startTime, endTime, new DefaultEasingFunction(easing));

        public static Color4 ValueAt(double time, Color4 startColour, Color4 endColour, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, startColour, endColour, startTime, endTime, new DefaultEasingFunction(easing));

        public static byte ValueAt(double time, byte val1, byte val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static sbyte ValueAt(double time, sbyte val1, sbyte val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static short ValueAt(double time, short val1, short val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static ushort ValueAt(double time, ushort val1, ushort val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static int ValueAt(double time, int val1, int val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static uint ValueAt(double time, uint val1, uint val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static long ValueAt(double time, long val1, long val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static ulong ValueAt(double time, ulong val1, ulong val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static float ValueAt(double time, float val1, float val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static decimal ValueAt(double time, decimal val1, decimal val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static double ValueAt(double time, double val1, double val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static Vector2 ValueAt(double time, Vector2 val1, Vector2 val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static RectangleF ValueAt(double time, RectangleF val1, RectangleF val2, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, val1, val2, startTime, endTime, new DefaultEasingFunction(easing));

        public static TValue ValueAt<TValue>(double time, TValue startValue, TValue endValue, double startTime, double endTime, Easing easing = Easing.None)
            => ValueAt(time, startValue, endValue, startTime, endTime, new DefaultEasingFunction(easing));

        #endregion

        #region ValueAt<TEasing>

        public static ColourInfo ValueAt<TEasing>(double time, ColourInfo startColour, ColourInfo endColour, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
        {
            if (startColour.HasSingleColour && endColour.HasSingleColour)
                return ValueAt(time, (Color4)startColour, (Color4)endColour, startTime, endTime, easing);

            return new ColourInfo
            {
                TopLeft = ValueAt(time, (Color4)startColour.TopLeft, (Color4)endColour.TopLeft, startTime, endTime, easing),
                BottomLeft = ValueAt(time, (Color4)startColour.BottomLeft, (Color4)endColour.BottomLeft, startTime, endTime, easing),
                TopRight = ValueAt(time, (Color4)startColour.TopRight, (Color4)endColour.TopRight, startTime, endTime, easing),
                BottomRight = ValueAt(time, (Color4)startColour.BottomRight, (Color4)endColour.BottomRight, startTime, endTime, easing),
            };
        }

        public static EdgeEffectParameters ValueAt<TEasing>(double time, EdgeEffectParameters startParams, EdgeEffectParameters endParams, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => new EdgeEffectParameters
            {
                Type = startParams.Type,
                Hollow = startParams.Hollow,
                Colour = ValueAt(time, startParams.Colour, endParams.Colour, startTime, endTime, easing),
                Offset = ValueAt(time, startParams.Offset, endParams.Offset, startTime, endTime, easing),
                Radius = ValueAt(time, startParams.Radius, endParams.Radius, startTime, endTime, easing),
                Roundness = ValueAt(time, startParams.Roundness, endParams.Roundness, startTime, endTime, easing),
            };

        public static SRGBColour ValueAt<TEasing>(double time, SRGBColour startColour, SRGBColour endColour, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => ValueAt(time, (Color4)startColour, (Color4)endColour, startTime, endTime, easing);

        public static Color4 ValueAt<TEasing>(double time, Color4 startColour, Color4 endColour, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
        {
            if (startColour == endColour)
                return startColour;

            double current = time - startTime;
            double duration = endTime - startTime;

            if (duration == 0 || current == 0)
                return startColour;

            float t = Math.Max(0, Math.Min(1, (float)ApplyEasing(easing, current / duration)));

            return new Color4(
                startColour.R + t * (endColour.R - startColour.R),
                startColour.G + t * (endColour.G - startColour.G),
                startColour.B + t * (endColour.B - startColour.B),
                startColour.A + t * (endColour.A - startColour.A));
        }

        public static byte ValueAt<TEasing>(double time, byte val1, byte val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (byte)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static sbyte ValueAt<TEasing>(double time, sbyte val1, sbyte val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (sbyte)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static short ValueAt<TEasing>(double time, short val1, short val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (short)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static ushort ValueAt<TEasing>(double time, ushort val1, ushort val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (ushort)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static int ValueAt<TEasing>(double time, int val1, int val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (int)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static uint ValueAt<TEasing>(double time, uint val1, uint val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (uint)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static long ValueAt<TEasing>(double time, long val1, long val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (long)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static ulong ValueAt<TEasing>(double time, ulong val1, ulong val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (ulong)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static float ValueAt<TEasing>(double time, float val1, float val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (float)ValueAt(time, (double)val1, val2, startTime, endTime, easing);

        public static decimal ValueAt<TEasing>(double time, decimal val1, decimal val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => (decimal)ValueAt(time, (double)val1, (double)val2, startTime, endTime, easing);

        public static double ValueAt<TEasing>(double time, double val1, double val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
        {
            if (val1 == val2)
                return val1;

            double current = time - startTime;
            double duration = endTime - startTime;

            if (current == 0)
                return val1;
            if (duration == 0)
                return val2;

            double t = ApplyEasing(easing, current / duration);
            return val1 + t * (val2 - val1);
        }

        public static Vector2 ValueAt<TEasing>(double time, Vector2 val1, Vector2 val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
        {
            float current = (float)(time - startTime);
            float duration = (float)(endTime - startTime);

            if (duration == 0 || current == 0)
                return val1;

            float t = (float)ApplyEasing(easing, current / duration);
            return val1 + t * (val2 - val1);
        }

        public static RectangleF ValueAt<TEasing>(double time, RectangleF val1, RectangleF val2, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
        {
            float current = (float)(time - startTime);
            float duration = (float)(endTime - startTime);

            if (duration == 0 || current == 0)
                return val1;

            float t = (float)ApplyEasing(easing, current / duration);

            return new RectangleF(
                val1.X + t * (val2.X - val1.X),
                val1.Y + t * (val2.Y - val1.Y),
                val1.Width + t * (val2.Width - val1.Width),
                val1.Height + t * (val2.X - val1.Height));
        }

        public static TValue ValueAt<TValue, TEasing>(double time, TValue startValue, TValue endValue, double startTime, double endTime, in TEasing easing)
            where TEasing : IEasingFunction
            => GenericInterpolation<TValue>.FUNCTION(time, startValue, endValue, startTime, endTime, easing);

        #endregion

        public static double ApplyEasing(Easing easing, double time)
            => ApplyEasing(new DefaultEasingFunction(easing), time);

        public static double ApplyEasing<TEasing>(TEasing easing, double time)
            where TEasing : IEasingFunction
            => easing.ApplyEasing(time);

        private static class GenericInterpolation<TValue>
        {
            public static readonly InterpolationFunc<TValue> FUNCTION;

            static GenericInterpolation()
            {
                const string interpolation_method = nameof(Interpolation.ValueAt);

                var parameters = typeof(InterpolationFunc<TValue>)
                                 .GetMethod(nameof(InterpolationFunc<TValue>.Invoke))
                                 ?.GetParameters().Select(p => p.ParameterType).ToArray();

                MethodInfo valueAtMethod = typeof(Interpolation).GetMethod(interpolation_method, parameters);

                if (valueAtMethod != null)
                    FUNCTION = (InterpolationFunc<TValue>)valueAtMethod.CreateDelegate(typeof(InterpolationFunc<TValue>));
                else
                {
                    var typeRef = FormatterServices.GetSafeUninitializedObject(typeof(TValue)) as IInterpolable<TValue>;

                    if (typeRef == null)
                        throw new NotSupportedException($"Type {typeof(TValue)} has no interpolation function. Implement the interface {typeof(IInterpolable<TValue>)} interface on the object.");

                    FUNCTION = typeRef.ValueAt;
                }
            }
        }
    }

    public delegate TValue InterpolationFunc<TValue>(double time, TValue startValue, TValue endValue, double startTime, double endTime, Easing easingType);
}
