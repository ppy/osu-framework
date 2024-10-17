// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    internal class SDL3MobileWindow : SDL3Window
    {
        public SDL3MobileWindow(GraphicsSurfaceType surfaceType, string appName)
            : base(surfaceType, appName)
        {
        }

        protected override unsafe void UpdateWindowStateAndSize(WindowState state, Display display, DisplayMode displayMode)
        {
            // This sets the status bar to hidden.
            SDL_SetWindowFullscreen(SDLWindowHandle, true);

            // Don't run base logic at all. Let's keep things simple.
        }
    }
}
