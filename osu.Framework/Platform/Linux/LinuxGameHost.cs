// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Platform.Linux
{
    using Native;
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
            Library.Load("libbass.so", Library.Flags.RTLD_LAZY | Library.Flags.RTLD_GLOBAL);
            Library.LogVersion("libbass.so", GetBassVersion);
            Library.LogVersion("libbass_fx.so", GetBassFxVersion);

        }
        public static System.Version GetBassVersion() => ManagedBass.Bass.Version;
        public static System.Version GetBassFxVersion() => ManagedBass.Fx.BassFx.Version;

        protected override Storage GetStorage(string baseName) => new LinuxStorage(baseName, this);

        public override Clipboard GetClipboard() => new LinuxClipboard();
    }
}
