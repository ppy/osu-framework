// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using System;

namespace osu.Framework.MathUtils
{
    public static class Precision
    {
        public const float FLOAT_EPSILON = 1e-3f;
        public const double DOUBLE_EPSILON = 1e-7;

        public static bool DefinitelyBigger(float value1, float value2, float acceptableDifference = FLOAT_EPSILON)
        {
            return value1 - acceptableDifference > value2;
        }

        public static bool DefinitelyBigger(double value1, double value2, double acceptableDifference = DOUBLE_EPSILON)
        {
            return value1 - acceptableDifference > value2;
        }

        public static bool AlmostBigger(float value1, float value2, float acceptableDifference = FLOAT_EPSILON)
        {
            return value1 > value2 - acceptableDifference;
        }

        public static bool AlmostBigger(double value1, double value2, double acceptableDifference = DOUBLE_EPSILON)
        {
            return value1 > value2 - acceptableDifference;
        }

        public static bool AlmostEquals(float value1, float value2, float acceptableDifference = FLOAT_EPSILON)
        {
            return Math.Abs(value1 - value2) <= acceptableDifference;
        }

        public static bool AlmostEquals(Vector2 value1, Vector2 value2, float acceptableDifference = FLOAT_EPSILON)
        {
            return AlmostEquals(value1.X, value2.X, acceptableDifference) && AlmostEquals(value1.Y, value2.Y, acceptableDifference);
        }

        public static bool AlmostEquals(double value1, double value2, double acceptableDifference = DOUBLE_EPSILON)
        {
            return Math.Abs(value1 - value2) <= acceptableDifference;
        }
    }
}
