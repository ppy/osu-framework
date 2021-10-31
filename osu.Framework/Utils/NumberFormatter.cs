// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Utils
{
    /// <summary>
    /// Exposes functionality for formatting numbers.
    /// </summary>
    public static class NumberFormatter
    {
        private static readonly string[] suffixes = { "y", "z", "a", "f", "p", "n", "µ", "m", "", "k", "M", "G", "T", "P", "E", "Z", "Y" };

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

            bool isNeg = number < 0;
            number = Math.Abs(number);

            int log1000 = (int)Math.Floor(Math.Log(number, 1000));
            int index = log1000 + 8;

            if (index >= suffixes.Length)
                return $"{(isNeg ? "-" : "")}{number:E}";

            return $"{(isNeg ? "-" : "")}{number / Math.Pow(1000, log1000):G3}{suffixes[index]}";
        }
    }
}
