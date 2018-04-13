// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.MathUtils
{
    /// <summary>
    /// Exposes functionality for formatting numbers.
    /// </summary>
    public static class NumberFormatter
    {
        /// <summary>
        /// Prints the number with at most two decimal digits, followed by a magnitude suffic (k, M, G, T, etc.) depending on the magnitude of the number. If the number is
        /// too large or small this will print the number using scientific notation instead.
        /// </summary>
        /// <param name="number">The number to print.</param>
        /// <returns>The number with at most two decimal digits, followed by a magnitude suffic (k, M, G, T, etc.) depending on the magnitude of the number. If the number is
        /// too large or small this will print the number using scientific notation instead.</returns>
        public static string PrintWithSiSuffix(double number)
        {
            // The logarithm is undefined for zero.
            if (number == 0)
                return "0";

            var isNeg = number < 0;
            number = Math.Abs(number);
            var strs = new[] { "y", "z", "a", "f", "p", "n", "µ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y" };
            int log1000 = (int)Math.Floor(Math.Log(number, 1000));
            int index = log1000 + 8;

            if (index < 0 || index >= strs.Length)
                return $"{number:E}";

            return $"{(isNeg ? "-" : "")}{number / Math.Pow(1000, log1000):G3}{strs[index]}";
        }
    }
}
