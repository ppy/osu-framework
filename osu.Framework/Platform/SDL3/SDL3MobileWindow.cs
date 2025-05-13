// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Input.StateChanges;
using SDL;
using static SDL.SDL3;

namespace osu.Framework.Platform.SDL3
{
    internal class SDL3MobileWindow : SDL3Window
    {
        public SDL3MobileWindow(GraphicsSurfaceType surfaceType, string appName)
            : base(surfaceType, appName)
        {
        }

        // Pen input is not necessarily direct on mobile platforms (specifically Android, where external tablets are supported),
        // but until users experience issues with this, consider it "direct" for now.
        protected override TabletPenDeviceType GetPenDeviceType(SDL_PenID id) => TabletPenDeviceType.Direct;

        protected override unsafe void UpdateWindowStateAndSize(WindowState state, Display display, DisplayMode displayMode)
        {
            // This sets the status bar to hidden.
            SDL_SetWindowFullscreen(SDLWindowHandle, true);

            // Don't run base logic at all. Let's keep things simple.
        }
    }
}
