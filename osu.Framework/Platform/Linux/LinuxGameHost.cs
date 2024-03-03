// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Mouse;
using static SDL2.SDL;

namespace osu.Framework.Platform.Linux
{
    public class LinuxGameHost : DesktopGameHost
    {
        /// <summary>
        /// If SDL disables the compositor.
        /// </summary>
        /// <remarks>
        /// On Linux, SDL will disable the compositor by default.
        /// Since not all applications want to do that, we can specify it manually.
        /// </remarks>
        public readonly bool BypassCompositor;

        internal LinuxGameHost(string gameName, HostOptions? options)
            : base(gameName, options)
        {
            BypassCompositor = Options.BypassCompositor;
        }

        protected override void SetupForRun()
        {
            SDL_SetHint(SDL_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR, BypassCompositor ? "1" : "0");
            base.SetupForRun();
        }

        protected override IWindow CreateWindow(GraphicsSurfaceType preferredSurface) => new SDL2DesktopWindow(preferredSurface);

        protected override ReadableKeyCombinationProvider CreateReadableKeyCombinationProvider() => new LinuxReadableKeyCombinationProvider();

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
        {
            var handlers = base.CreateAvailableInputHandlers();

            foreach (var h in handlers.OfType<MouseHandler>())
            {
                // There are several bugs we need to fix with Linux / SDL2 cursor handling before switching this on.
                h.UseRelativeMode.Value = false;
                h.UseRelativeMode.Default = false;
            }

            return handlers;
        }
    }
}
