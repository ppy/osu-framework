// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using SDL2;

namespace osu.Framework.iOS
{
    public class IOSWindow : SDL2Window
    {
        private readonly IOSGameHost host;

        protected override IEnumerable<WindowMode> DefaultSupportedWindowModes => new[]
        {
            Configuration.WindowMode.Fullscreen,
        };

        public IOSWindow(GraphicsSurfaceType surfaceType, IOSGameHost host)
            : base(surfaceType)
        {
            this.host = host;
        }

        protected override void HandleEvent(SDL.SDL_Event e)
        {
            // todo: this should be in SDL2Window to cover Android as well once it uses SDL.
            switch (e.type)
            {
                case SDL.SDL_EventType.SDL_APP_DIDENTERBACKGROUND:
                    host.Suspend();
                    break;

                case SDL.SDL_EventType.SDL_APP_WILLENTERFOREGROUND:
                    host.Resume();
                    break;

                case SDL.SDL_EventType.SDL_APP_LOWMEMORY:
                    host.Collect();
                    break;

                default:
                    base.HandleEvent(e);
                    break;
            }
        }

        // todo: add support for safe area.
    }
}
