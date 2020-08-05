// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Platform.Linux.Sdl;
using osuTK;

namespace osu.Framework.Platform.Linux
{
    public class LinuxGameHost : DesktopGameHost
    {
        internal LinuxGameHost(string gameName, bool bindIPC = false, ToolkitOptions toolkitOptions = default, bool portableInstallation = false, bool useSdl = false)
            : base(gameName, bindIPC, toolkitOptions, portableInstallation, useSdl)
        {
        }

        protected override IWindow CreateWindow() =>
            !UseSdl ? (IWindow)new LinuxGameWindow() : new DesktopWindow();

        public override string UserStoragePath
        {
            get
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                string xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                string[] paths =
                {
                    xdg ?? Path.Combine(home, ".local", "share"),
                    Path.Combine(home)
                };

                foreach (string path in paths)
                {
                    if (Directory.Exists(path))
                        return path;
                }

                return paths[0];
            }
        }

        public override Clipboard GetClipboard() =>
            Window is DesktopWindow || (Window as LinuxGameWindow)?.IsSdl == true ? (Clipboard)new SdlClipboard() : new LinuxClipboard();
    }
}
