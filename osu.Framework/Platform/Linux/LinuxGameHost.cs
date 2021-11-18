// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using osu.Framework.Input;
using osu.Framework.Platform.Linux.SDL2;

namespace osu.Framework.Platform.Linux
{
    public class LinuxGameHost : DesktopGameHost
    {
        internal LinuxGameHost(string gameName, bool bindIPC = false, bool portableInstallation = false)
            : base(gameName, bindIPC, portableInstallation)
        {
        }

        protected override IWindow CreateWindow() => new SDL2DesktopWindow();

        public override IEnumerable<string> UserStoragePaths
        {
            get
            {
                string xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");

                if (!string.IsNullOrEmpty(xdg))
                    yield return xdg;

                yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".local", "share");

                foreach (string path in base.UserStoragePaths)
                    yield return path;
            }
        }

        public override Clipboard GetClipboard() => new SDL2Clipboard();

        protected override ReadableKeyCombinationProvider CreateReadableKeyCombinationProvider() => new LinuxReadableKeyCombinationProvider();
    }
}
