// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using TKVector2 = osuTK.Vector2;
using SNVector2 = System.Numerics.Vector2;
using SDPoint = System.Drawing.Point;
using SDSize = System.Drawing.Size;

namespace osu.Framework.Extensions
{
    /// <summary>
    /// Temporary extension functions for bridging between osuTK, System.Drawing, and System.Numerics
    /// Can be removed when the SDL3 migration is complete.
    /// </summary>
    public static class BridgingExtensions
    {
        public static TKVector2 ToOsuTK(this SNVector2 vec) =>
            new TKVector2(vec.X, vec.Y);

        public static SNVector2 ToSystemNumerics(this TKVector2 vec) =>
            new SNVector2(vec.X, vec.Y);

        public static SNVector2 ToSystemNumerics(this SDSize size) =>
            new SNVector2(size.Width, size.Height);

        public static SNVector2 ToSystemNumerics(this SDPoint point) =>
            new SNVector2(point.X, point.Y);

        public static SDSize ToSystemDrawingSize(this SNVector2 vec) =>
            new SDSize((int)vec.X, (int)vec.Y);

        public static SDPoint ToSystemDrawingPoint(this SNVector2 vec) =>
            new SDPoint((int)vec.X, (int)vec.Y);
    }
}
