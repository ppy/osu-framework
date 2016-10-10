// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Desktop.Input.Handlers.Keyboard;
using osu.Framework.Desktop.Input.Handlers.Mouse;
using osu.Framework.Input.Handlers;
using OpenTK.Graphics;

namespace osu.Framework.Desktop.Platform.Linux
{
    public class LinuxGameHost : DesktopGameHost
    {
        internal LinuxGameHost(GraphicsContextFlags flags, string gameName)
        {
            Window = new LinuxGameWindow(flags);
            Window.Activated += OnActivated;
            Window.Deactivated += OnDeactivated;
            Storage = new LinuxStorage(gameName);
        }

        public override IEnumerable<InputHandler> GetInputHandlers()
        {
            return new InputHandler[] {
                new OpenTKMouseHandler(), //handles cursor position
                new FormMouseHandler(),   //handles button states
                new OpenTKKeyboardHandler(),
            };
        }
    }
}
