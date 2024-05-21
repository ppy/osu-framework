// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using static SDL2.SDL;

namespace osu.Framework.Platform.Windows
{
    internal partial class WindowsMouseHandler
    {
        private SDL_WindowsMessageHook sdl2Callback = null!;

        private bool bindHandlerSDL2(GameHost host)
        {
            // ReSharper disable once ConvertClosureToMethodGroup
            sdl2Callback = (ptr, wnd, u, param, l) => onWndProcSDL2(ptr, wnd, u, param, l);

            Enabled.BindValueChanged(enabled =>
            {
                host.InputThread.Scheduler.Add(() => SDL_SetWindowsMessageHook(enabled.NewValue ? sdl2Callback : null, IntPtr.Zero));
            }, true);

            return true;
        }
    }
}
