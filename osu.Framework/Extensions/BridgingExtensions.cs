// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using TKVector2 = osuTK.Vector2;
using SNVector2 = System.Numerics.Vector2;
using SDPoint = System.Drawing.Point;
using SDSize = System.Drawing.Size;
using TKVector3 = osuTK.Vector3;
using SNVector3 = System.Numerics.Vector3;
using TKVector4 = osuTK.Vector4;
using SNVector4 = System.Numerics.Vector4;

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

        public static TKVector3 ToOsuTK(this SNVector3 vec) =>
            new TKVector3(vec.X, vec.Y, vec.Z);

        public static TKVector4 ToOsuTK(this SNVector4 vec) =>
            new TKVector4(vec.X, vec.Y, vec.Z, vec.W);

        public static SNVector2 ToSystemNumerics(this TKVector2 vec) =>
            new SNVector2(vec.X, vec.Y);

        public static SNVector3 ToSystemNumerics(this TKVector3 vec) =>
            new SNVector3(vec.X, vec.Y, vec.Z);

        public static SNVector4 ToSystemNumerics(this TKVector4 vec) =>
            new SNVector4(vec.X, vec.Y, vec.Z, vec.W);

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
