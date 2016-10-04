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
        public override bool IsActive => true; // TODO LINUX

        internal LinuxGameHost(GraphicsContextFlags flags, string game)
        {
            Window = new LinuxGameWindow(flags);
            Window.Activated += OnActivated;
            Window.Deactivated += OnDeactivated;
            Storage = new LinuxStorage(game);
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
