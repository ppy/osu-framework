// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using TKVector2 = osuTK.Vector2;
using SNVector2 = System.Numerics.Vector2;

namespace osu.Framework.Extensions
{
    /// <summary>
    /// Temporary extension functions for bridging between osuTK, Veldrid, and System.Numerics
    /// </summary>
    public static class BridgingExtensions
    {
        public static TKVector2 ToOsuTK(this SNVector2 vec) =>
            new TKVector2(vec.X, vec.Y);

        public static SNVector2 ToSystemNumerics(this TKVector2 vec) =>
            new SNVector2(vec.X, vec.Y);
    }
}
