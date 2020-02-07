// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform.Linux.Native;
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

        protected override void SetupForRun()
        {
            base.SetupForRun();

            // required for the time being to address libbass_fx.so load failures (see https://github.com/ppy/osu/issues/2852)
            Library.Load("libbass.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
        }

        protected override IWindow CreateWindow() =>
            !UseSdl ? (IWindow)new LinuxGameWindow() : new SDLWindow();

        protected override Storage GetStorage(string baseName) => new LinuxStorage(baseName, this);

        public override Clipboard GetClipboard() =>
            Window is SDLWindow || (Window as LinuxGameWindow)?.IsSdl == true ? (Clipboard)new SdlClipboard() : new LinuxClipboard();
    }
}
