// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.MathUtils
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
                throw new ArgumentOutOfRangeException($"{nameof(@base)} has to lie in [0,1], but is {@base}.", nameof(@base));
            if (exponent < 0)
                throw new ArgumentOutOfRangeException($"{nameof(exponent)} has to be bigger than 0, but is {exponent}.", nameof(exponent));

            return Lerp(start, final, 1 - Math.Pow(@base, exponent));
        }

        public static ColourInfo ValueAt(double time, ColourInfo startColour, ColourInfo endColour, double startTime, double endTime, Easing easing = Easing.None)
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

        public static EdgeEffectParameters ValueAt(double time, EdgeEffectParameters startParams, EdgeEffectParameters endParams, double startTime, double endTime, Easing easing = Easing.None)
        {
            return new EdgeEffectParameters
            {
                Type = startParams.Type,
                Hollow = startParams.Hollow,
                Colour = ValueAt(time, startParams.Colour, endParams.Colour, startTime, endTime, easing),
                Offset = ValueAt(time, startParams.Offset, endParams.Offset, startTime, endTime, easing),
                Radius = ValueAt(time, startParams.Radius, endParams.Radius, startTime, endTime, easing),
                Roundness = ValueAt(time, startParams.Roundness, endParams.Roundness, startTime, endTime, easing),
            };
        }

        public static SRGBColour ValueAt(double time, SRGBColour startColour, SRGBColour endColour, double startTime, double endTime, Easing easing = Easing.None) =>
            ValueAt(time, (Color4)startColour, (Color4)endColour, startTime, endTime, easing);

        public static Color4 ValueAt(double time, Color4 startColour, Color4 endColour, double startTime, double endTime, Easing easing = Easing.None)
        {
            if (startColour == endColour)
                return startColour;

            double current = time - startTime;
            double duration = endTime - startTime;

            if (duration == 0 || current == 0)
                return startColour;

            return new Color4(
                (float)Math.Max(0, Math.Min(1, ApplyEasing(easing, current, startColour.R, endColour.R - startColour.R, duration))),
                (float)Math.Max(0, Math.Min(1, ApplyEasing(easing, current, startColour.G, endColour.G - startColour.G, duration))),
                (float)Math.Max(0, Math.Min(1, ApplyEasing(easing, current, startColour.B, endColour.B - startColour.B, duration))),
                (float)Math.Max(0, Math.Min(1, ApplyEasing(easing, current, startColour.A, endColour.A - startColour.A, duration))));
        }

        public static string ValueAt(double time, string startText, string endText, double startTime, double endTime, Easing easing = Easing.None)
            => time < endTime ? startText : endText;

        public static byte ValueAt(double time, byte val1, byte val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (byte)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static sbyte ValueAt(double time, sbyte val1, sbyte val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (sbyte)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static short ValueAt(double time, short val1, short val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (short)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static ushort ValueAt(double time, ushort val1, ushort val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (ushort)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static int ValueAt(double time, int val1, int val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (int)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static uint ValueAt(double time, uint val1, uint val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (uint)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static long ValueAt(double time, long val1, long val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (long)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static ulong ValueAt(double time, ulong val1, ulong val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (ulong)Math.Round(ValueAt(time, (double)val1, val2, startTime, endTime, easing));

        public static float ValueAt(double time, float val1, float val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (float)ValueAt(time, (double)val1, val2, startTime, endTime, easing);

        public static decimal ValueAt(double time, decimal val1, decimal val2, double startTime, double endTime, Easing easing = Easing.None) =>
            (decimal)ValueAt(time, (double)val1, (double)val2, startTime, endTime, easing);

        public static double ValueAt(double time, double val1, double val2, double startTime, double endTime, Easing easing = Easing.None)
        {
            if (val1 == val2)
                return val1;

            double current = time - startTime;
            double duration = endTime - startTime;

            if (current == 0)
                return val1;
            if (duration == 0)
                return val2;

            return ApplyEasing(easing, current, val1, val2 - val1, duration);
        }

        public static Vector2 ValueAt(double time, Vector2 val1, Vector2 val2, double startTime, double endTime, Easing easing = Easing.None)
        {
            float current = (float)(time - startTime);
            float duration = (float)(endTime - startTime);

            if (duration == 0 || current == 0)
                return val1;

            return new Vector2(
                (float)ApplyEasing(easing, current, val1.X, val2.X - val1.X, duration),
                (float)ApplyEasing(easing, current, val1.Y, val2.Y - val1.Y, duration));
        }

        public static RectangleF ValueAt(double time, RectangleF val1, RectangleF val2, double startTime, double endTime, Easing easing = Easing.None)
        {
            float current = (float)(time - startTime);
            float duration = (float)(endTime - startTime);

            if (duration == 0 || current == 0)
                return val1;

            return new RectangleF(
                (float)ApplyEasing(easing, current, val1.X, val2.X - val1.X, duration),
                (float)ApplyEasing(easing, current, val1.Y, val2.Y - val1.Y, duration),
                (float)ApplyEasing(easing, current, val1.Width, val2.Width - val1.Width, duration),
                (float)ApplyEasing(easing, current, val1.Height, val2.Height - val1.Height, duration));
        }

        public static double ApplyEasing(Easing easing, double time, double initial, double change, double duration)
        {
            if (change == 0 || time == 0 || duration == 0) return initial;
            if (time == duration) return initial + change;

            switch (easing)
            {
                default:
                    return change * (time / duration) + initial;
                case Easing.In:
                case Easing.InQuad:
                    return change * (time /= duration) * time + initial;
                case Easing.Out:
                case Easing.OutQuad:
                    return -change * (time /= duration) * (time - 2) + initial;
                case Easing.InOutQuad:
                    if ((time /= duration / 2) < 1) return change / 2 * time * time + initial;
                    return -change / 2 * (--time * (time - 2) - 1) + initial;
                case Easing.InCubic:
                    return change * (time /= duration) * time * time + initial;
                case Easing.OutCubic:
                    return change * ((time = time / duration - 1) * time * time + 1) + initial;
                case Easing.InOutCubic:
                    if ((time /= duration / 2) < 1) return change / 2 * time * time * time + initial;
                    return change / 2 * ((time -= 2) * time * time + 2) + initial;
                case Easing.InQuart:
                    return change * (time /= duration) * time * time * time + initial;
                case Easing.OutQuart:
                    return -change * ((time = time / duration - 1) * time * time * time - 1) + initial;
                case Easing.InOutQuart:
                    if ((time /= duration / 2) < 1) return change / 2 * time * time * time * time + initial;
                    return -change / 2 * ((time -= 2) * time * time * time - 2) + initial;
                case Easing.InQuint:
                    return change * (time /= duration) * time * time * time * time + initial;
                case Easing.OutQuint:
                    return change * ((time = time / duration - 1) * time * time * time * time + 1) + initial;
                case Easing.OutPow10:
                    return change * ((time = time / duration - 1) * Math.Pow(time, 10) + 1) + initial;
                case Easing.InOutQuint:
                    if ((time /= duration / 2) < 1) return change / 2 * time * time * time * time * time + initial;
                    return change / 2 * ((time -= 2) * time * time * time * time + 2) + initial;
                case Easing.InSine:
                    return -change * Math.Cos(time / duration * (MathHelper.Pi / 2)) + change + initial;
                case Easing.OutSine:
                    return change * Math.Sin(time / duration * (MathHelper.Pi / 2)) + initial;
                case Easing.InOutSine:
                    return -change / 2 * (Math.Cos(MathHelper.Pi * time / duration) - 1) + initial;
                case Easing.InExpo:
                    return change * Math.Pow(2, 10 * (time / duration - 1)) + initial;
                case Easing.OutExpo:
                    return time == duration ? initial + change : change * (-Math.Pow(2, -10 * time / duration) + 1) + initial;
                case Easing.InOutExpo:
                    if ((time /= duration / 2) < 1) return change / 2 * Math.Pow(2, 10 * (time - 1)) + initial;
                    return change / 2 * (-Math.Pow(2, -10 * --time) + 2) + initial;
                case Easing.InCirc:
                    return -change * (Math.Sqrt(1 - (time /= duration) * time) - 1) + initial;
                case Easing.OutCirc:
                    return change * Math.Sqrt(1 - (time = time / duration - 1) * time) + initial;
                case Easing.InOutCirc:
                    if ((time /= duration / 2) < 1) return -change / 2 * (Math.Sqrt(1 - time * time) - 1) + initial;
                    return change / 2 * (Math.Sqrt(1 - (time -= 2) * time) + 1) + initial;
                case Easing.InElastic:
                    {
                        if ((time /= duration) == 1) return initial + change;

                        var p = duration * .3;
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else
                            s = p / (2 * MathHelper.Pi) * Math.Asin(change / a);
                        return -(a * Math.Pow(2, 10 * (time -= 1)) * Math.Sin((time * duration - s) * (2 * MathHelper.Pi) / p)) + initial;
                    }
                case Easing.OutElastic:
                    {
                        if ((time /= duration) == 1) return initial + change;

                        var p = duration * .3;
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else s = p / (2 * MathHelper.Pi) * Math.Asin(change / a);
                        return a * Math.Pow(2, -10 * time) * Math.Sin((time * duration - s) * (2 * MathHelper.Pi) / p) + change + initial;
                    }
                case Easing.OutElasticHalf:
                    {
                        if ((time /= duration) == 1) return initial + change;

                        var p = duration * .3;
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else s = p / (2 * MathHelper.Pi) * Math.Asin(change / a);
                        return a * Math.Pow(2, -10 * time) * Math.Sin((0.5f * time * duration - s) * (2 * MathHelper.Pi) / p) + change + initial;
                    }
                case Easing.OutElasticQuarter:
                    {
                        if ((time /= duration) == 1) return initial + change;

                        var p = duration * .3;
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else s = p / (2 * MathHelper.Pi) * Math.Asin(change / a);
                        return a * Math.Pow(2, -10 * time) * Math.Sin((0.25f * time * duration - s) * (2 * MathHelper.Pi) / p) + change + initial;
                    }
                case Easing.InOutElastic:
                    {
                        if ((time /= duration / 2) == 2) return initial + change;

                        var p = duration * (.3 * 1.5);
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else s = p / (2 * MathHelper.Pi) * Math.Asin(change / a);
                        if (time < 1) return -.5 * (a * Math.Pow(2, 10 * (time -= 1)) * Math.Sin((time * duration - s) * (2 * MathHelper.Pi) / p)) + initial;
                        return a * Math.Pow(2, -10 * (time -= 1)) * Math.Sin((time * duration - s) * (2 * MathHelper.Pi) / p) * .5 + change + initial;
                    }
                case Easing.InBack:
                    {
                        const double s = 1.70158;
                        return change * (time /= duration) * time * ((s + 1) * time - s) + initial;
                    }
                case Easing.OutBack:
                    {
                        const double s = 1.70158;
                        return change * ((time = time / duration - 1) * time * ((s + 1) * time + s) + 1) + initial;
                    }
                case Easing.InOutBack:
                    {
                        double s = 1.70158;
                        if ((time /= duration / 2) < 1) return change / 2 * (time * time * (((s *= 1.525) + 1) * time - s)) + initial;
                        return change / 2 * ((time -= 2) * time * (((s *= 1.525) + 1) * time + s) + 2) + initial;
                    }
                case Easing.InBounce:
                    return change - ApplyEasing(Easing.OutBounce, duration - time, 0, change, duration) + initial;
                case Easing.OutBounce:
                    if ((time /= duration) < 1 / 2.75)
                    {
                        return change * (7.5625 * time * time) + initial;
                    }
                    if (time < 2 / 2.75)
                    {
                        return change * (7.5625 * (time -= 1.5 / 2.75) * time + .75) + initial;
                    }
                    if (time < 2.5 / 2.75)
                    {
                        return change * (7.5625 * (time -= 2.25 / 2.75) * time + .9375) + initial;
                    }
                    return change * (7.5625 * (time -= 2.625 / 2.75) * time + .984375) + initial;
                case Easing.InOutBounce:
                    if (time < duration / 2) return ApplyEasing(Easing.InBounce, time * 2, 0, change, duration) * .5 + initial;
                    return ApplyEasing(Easing.OutBounce, time * 2 - duration, 0, change, duration) * .5 + change * .5 + initial;
            }
        }
    }
}
