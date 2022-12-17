// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using osu.Framework.Extensions;
using osu.Framework.Input;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Joystick;
using osu.Framework.Input.Handlers.Keyboard;
using osu.Framework.Input.Handlers.Midi;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Input.Handlers.Tablet;
using osu.Framework.Input.Handlers.Touch;
using osu.Framework.Platform;

namespace osu.Framework.Desktop.Platform
{
    public abstract class DesktopGameHost : GameHost
    {
        protected DesktopGameHost(string gameName, HostOptions options = null)
            : base(gameName, options)
        {
            IsPortableInstallation = Options.PortableInstallation;
        }

        public sealed override Storage GetStorage(string path) => new DesktopStorage(path, this);

        public bool IsPortableInstallation { get; }

        public override bool CapsLockEnabled => (Window as SDL2DesktopWindow)?.CapsLockPressed == true;

        public override bool OpenFileExternally(string filename)
        {
            openUsingShellExecute(filename);
            return true;
        }

        public override void OpenUrlExternally(string url)
        {
            if (!url.CheckIsValidUrl())
                throw new ArgumentException("The provided URL must be one of either http://, https:// or mailto: protocols.", nameof(url));

            openUsingShellExecute(url);
        }

        public override bool PresentFileExternally(string filename)
        {
            // should be overriden to highlight/select the file in the folder if such native API exists.
            OpenFileExternally(Path.GetDirectoryName(filename.TrimDirectorySeparator()));
            return true;
        }

        private void openUsingShellExecute(string path) => Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true //see https://github.com/dotnet/corefx/issues/10361
        });

        protected override TextInputSource CreateTextInput()
        {
            if (Window is SDL2DesktopWindow desktopWindow)
                return new SDL2DesktopWindowTextInput(desktopWindow);

            return base.CreateTextInput();
        }

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
