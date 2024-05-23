// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform.SDL2;
using static SDL2.SDL;

namespace osu.Framework.Platform.Linux
{
    internal class SDL2LinuxWindow : SDL2DesktopWindow
    {
        public SDL2LinuxWindow(GraphicsSurfaceType surfaceType, string appName, bool bypassCompositor)
            : base(surfaceType, appName)
        {
            SDL_SetHint(SDL_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR, bypassCompositor ? "1" : "0");
        }
    }
}
