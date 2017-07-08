// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
            var isNeg = number < 0;
            number = Math.Abs(number);
            var strs = new[] { "", "k", "M", "G", "T", "P", "E", "Z", "Y" };
            foreach (var str in strs)
            {
                if (number < 1000)
                    return $"{(isNeg ? "-" : "")}{Math.Round(number, 2):G}{str}";

                number = number / 1000;
            }
            return $"{number:E}";
        }
    }
}
