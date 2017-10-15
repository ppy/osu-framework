// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Platform.MacOS
{
    public class MacOSGameHost : DesktopGameHost
    {
        internal MacOSGameHost(string gameName, bool bindIPC = false)
            : base(gameName, bindIPC)
        {
            Window = new DesktopGameWindow();
            Window.WindowStateChanged += (sender, e) =>
            {
                if (Window.WindowState != OpenTK.WindowState.Minimized)
                    OnActivated();
                else
                    OnDeactivated();
            };
            Dependencies.Cache(Storage = new MacOSStorage(gameName));
        }

        public override Clipboard GetClipboard()
        {
            return new MacOSClipboard();
        }
    }
}
