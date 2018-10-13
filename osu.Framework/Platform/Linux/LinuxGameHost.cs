// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Platform.Linux.Native;
using osu.Framework.Platform.SDL;

namespace osu.Framework.Platform.Linux
{
    public class LinuxGameHost : DesktopGameHost
    {
        internal LinuxGameHost(string gameName, bool bindIPC = false)
            : base(gameName, bindIPC)
        {
            Window = new LinuxGameWindow();
            Window.WindowStateChanged += (sender, e) =>
            {
                if (Window.WindowState != OpenTK.WindowState.Minimized)
                    OnActivated();
                else
                    OnDeactivated();
            };

            // required for the time being to address libbass_fx.so load failures (see https://github.com/ppy/osu/issues/2852)
            Library.Load("libbass.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
        }

        protected override Storage GetStorage(string baseName) => new LinuxStorage(baseName, this);

        public override Clipboard GetClipboard()
        {
            if (((LinuxGameWindow)Window).IsSDL)
            {
                return new SDLClipboard();
            }
            else
            {
                return new LinuxClipboard();
            }
        }
    }
}
