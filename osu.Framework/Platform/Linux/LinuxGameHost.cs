// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
            !UseSdl ? (IWindow)new LinuxGameWindow() : new Window();

        protected override Storage GetStorage(string baseName) => new LinuxStorage(baseName, this);

        public override Clipboard GetClipboard() =>
            Window is Window || (Window as LinuxGameWindow)?.IsSdl == true ? (Clipboard)new SdlClipboard() : new LinuxClipboard();
    }
}
