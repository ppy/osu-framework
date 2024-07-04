// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.Handlers.Keyboard;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.Handlers.Tablet;
using osu.Framework.Input.Handlers.Touch;
using osu.Framework.Platform.SDL2;
using osu.Framework.Platform.SDL3;
using SixLabors.ImageSharp.Formats.Png;

namespace osu.Framework.Platform
{
    public abstract class SDLGameHost : GameHost
    {
        public override bool CapsLockEnabled => (Window as ISDLWindow)?.CapsLockPressed == true;

        protected SDLGameHost(string gameName, HostOptions? options = null)
            : base(gameName, options)
        {
        }

        protected override TextInputSource CreateTextInput()
        {
            if (Window is ISDLWindow window)
                return new SDLWindowTextInput(window);

            return base.CreateTextInput();
        }

        protected override Clipboard CreateClipboard()
            => FrameworkEnvironment.UseSDL3
                ? new SDL3Clipboard(PngFormat.Instance) // PNG works well on linux
                : new SDL2Clipboard();

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers() =>
            new InputHandler[]
            {
                new KeyboardHandler(),
                // tablet should get priority over mouse to correctly handle cases where tablet drivers report as mice as well.
                new OpenTabletDriverHandler(),
                new MouseHandler(),
                new TouchHandler(),
                new JoystickHandler(),
                new MidiHandler(),
            };
    }
}
