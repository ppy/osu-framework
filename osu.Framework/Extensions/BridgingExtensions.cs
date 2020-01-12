// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using TKVector2 = osuTK.Vector2;
using SNVector2 = System.Numerics.Vector2;
using SDPoint = System.Drawing.Point;
using VPoint = Veldrid.Point;
using SDSize = System.Drawing.Size;
using VWindowState = Veldrid.WindowState;
using TKWindowState = osuTK.WindowState;

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

        public static SNVector2 ToSystemNumerics(this SDSize size) =>
            new SNVector2(size.Width, size.Height);

        public static SNVector2 ToSystemNumerics(this SDPoint point) =>
            new SNVector2(point.X, point.Y);

        public static SDSize ToSystemDrawingSize(this SNVector2 vec) =>
            new SDSize((int)vec.X, (int)vec.Y);

        public static SDPoint ToSystemDrawingPoint(this SNVector2 vec) =>
            new SDPoint((int)vec.X, (int)vec.Y);

        public static VPoint ToVeldridPoint(this SNVector2 vec) =>
            new VPoint((int)vec.X, (int)vec.Y);

        public static SNVector2 ToSystemNumerics(this VPoint point) =>
            new SNVector2(point.X, point.Y);

        public static TKWindowState ToOsuTK(this VWindowState state)
        {
            switch (state)
            {
                case VWindowState.Normal:
                    return TKWindowState.Normal;

                case VWindowState.FullScreen:
                    return TKWindowState.Fullscreen;

                case VWindowState.Maximized:
                    return TKWindowState.Maximized;

                case VWindowState.Minimized:
                    return TKWindowState.Minimized;

                case VWindowState.BorderlessFullScreen:
                    // WARNING: not supported by osuTK.WindowState
                    return TKWindowState.Fullscreen;

                case VWindowState.Hidden:
                    // WARNING: not supported by osuTK.WindowState
                    return TKWindowState.Normal;
            }

            return TKWindowState.Normal;
        }

        public static VWindowState ToVeldrid(this TKWindowState state)
        {
            switch (state)
            {
                case TKWindowState.Normal:
                    return VWindowState.Normal;

                case TKWindowState.Minimized:
                    return VWindowState.Minimized;

                case TKWindowState.Maximized:
                    return VWindowState.Maximized;

                case TKWindowState.Fullscreen:
                    return VWindowState.FullScreen;
            }

            // WARNING: some cases not supported by osuTK.WindowState
            return VWindowState.Normal;
        }
    }
}
