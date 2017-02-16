﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
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
        private readonly OpenTKKeyboardHandler keyboardHandler = new OpenTKKeyboardHandler();
        internal LinuxGameHost(GraphicsContextFlags flags, string gameName, bool bindIPC = false) : base(gameName, bindIPC)
        {
            Window = new DesktopGameWindow();
            Window.WindowStateChanged += (sender, e) =>
            {
                if (Window.WindowState != OpenTK.WindowState.Minimized)
                    OnActivated();
                else
                    OnDeactivated();
            };
            Dependencies.Cache(Storage = new LinuxStorage(gameName));
        }

        public override IEnumerable<InputHandler> GetInputHandlers()
        {
            return new InputHandler[] { new OpenTKMouseHandler(), keyboardHandler };
        }
    }
}
