// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.MathUtils
{
    /// <summary>
    /// Helper function for Unit Conversation.
    /// </summary>
    public static class UnitConverter
    {
        // https://www.w3schools.com/cssref/css_units.asp
        /// <summary>
        /// Computes Centimetre to Pixels
        /// </summary>
        /// <returns>Pixels</returns>
        public static double Cm2Pixel(double cm) => cm * 37.795275591d; // this could be actually wrong. as i calculated it in my head.

        /// <summary>
        /// Computes Inches to Pixels
        /// </summary>
        /// <returns>Pixels</returns>
        public static double Inch2Pixel(double inch) => inch * 96d;

        /// <summary>
        /// Computes Points to Pixels
        /// </summary>
        /// <returns>Pixels</returns>
        public static double Point2Pixel(double pt) => pt * (96d / 72d);

        /// <summary>
        /// Computes Pica to Pixels
        /// </summary>
        /// <returns>Pixels</returns>
        public static double Pica2Pixel(double pc) => pc * Point2Pixel(12d);
    }
}
