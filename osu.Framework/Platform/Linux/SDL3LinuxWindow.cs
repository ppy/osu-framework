// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform.SDL3;
using static SDL.SDL3;

namespace osu.Framework.Platform.Linux
{
    internal class SDL3LinuxWindow : SDL3DesktopWindow
    {
        public SDL3LinuxWindow(GraphicsSurfaceType surfaceType, string appName, bool bypassCompositor)
            : base(surfaceType, appName)
        {
            SDL_SetHint(SDL_HINT_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR, bypassCompositor ? "1"u8 : "0"u8);
        }
    }
}
