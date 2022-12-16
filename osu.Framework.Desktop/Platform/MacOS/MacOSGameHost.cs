// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Handlers;
using osu.Framework.Input.Handlers.Mouse;
using osu.Framework.Platform;

namespace osu.Framework.Desktop.Platform.MacOS
{
    public class MacOSGameHost : DesktopGameHost
    {
        internal MacOSGameHost(string gameName, HostOptions options)
            : base(gameName, options)
        {
        }

        protected override IWindow CreateWindow(GraphicsSurfaceType preferredSurface) => new MacOSWindow(preferredSurface);

        public override IEnumerable<string> UserStoragePaths
        {
            get
            {
                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Application Support");

                string xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

                if (!string.IsNullOrEmpty(xdg))
                    yield return xdg;

                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local", "share");

                foreach (string path in base.UserStoragePaths)
                    yield return path;
            }
        }

        public override Clipboard GetClipboard() => new MacOSClipboard();

        protected override ReadableKeyCombinationProvider CreateReadableKeyCombinationProvider() => new MacOSReadableKeyCombinationProvider();

        protected override void Swap()
        {
            base.Swap();

            // It has been reported that this helps performance on macOS (https://github.com/ppy/osu/issues/7447)
            if (Window.GraphicsSurface.Type == GraphicsSurfaceType.OpenGL && !Renderer.VerticalSync)
                Renderer.WaitUntilIdle();
        }

        protected override IEnumerable<InputHandler> CreateAvailableInputHandlers()
        {
            var handlers = base.CreateAvailableInputHandlers();

            foreach (var h in handlers.OfType<MouseHandler>())
            {
                // There are several bugs we need to fix with macOS / SDL2 cursor handling before switching this on.
                h.UseRelativeMode.Value = false;
            }

            return handlers;
        }

        public override IEnumerable<KeyBinding> PlatformKeyBindings => AppleConstants.KeyBindings;
    }
}
