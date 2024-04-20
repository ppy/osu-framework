// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using SDL;

namespace osu.Framework.Platform
{
    internal class SDL3MobileWindow : SDL3Window
    {
        public SDL3MobileWindow(GraphicsSurfaceType surfaceType)
            : base(surfaceType)
        {
        }

        protected override unsafe void UpdateWindowStateAndSize(WindowState state, Display display, DisplayMode displayMode)
        {
            // This sets the status bar to hidden.
            SDL3.SDL_SetWindowFullscreen(SDLWindowHandle, SDL_bool.SDL_TRUE);

            // Don't run base logic at all. Let's keep things simple.
        }
    }
}
