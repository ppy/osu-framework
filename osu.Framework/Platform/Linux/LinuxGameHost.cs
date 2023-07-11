// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SDL2;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Mouse;

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
            SDL.SDL_SetHint(SDL.SDL_VIDEO_X11_NET_WM_BYPASS_COMPOSITOR, BypassCompositor ? "1" : "0");
            base.SetupForRun();
        }

        public override IEnumerable<string> UserStoragePaths
        {
            get
            {
                string? xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

                if (!string.IsNullOrEmpty(xdg))
                    yield return xdg;

                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

                foreach (string path in base.UserStoragePaths)
                    yield return path;
            }
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
