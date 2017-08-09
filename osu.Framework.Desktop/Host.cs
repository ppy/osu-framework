// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Desktop.Platform;
using osu.Framework.Desktop.Platform.Linux;
using osu.Framework.Desktop.Platform.MacOS;
using osu.Framework.Desktop.Platform.Windows;

namespace osu.Framework.Desktop
{
    public static class Host
    {
        public static DesktopGameHost GetSuitableHost(string gameName, bool bindIPC = false)
        {
            if (RuntimeInfo.IsMacOsx)
                return new MacOSGameHost(gameName, bindIPC);
            
            if (RuntimeInfo.IsUnix)
                return new LinuxGameHost(gameName, bindIPC);

            return new WindowsGameHost(gameName, bindIPC);
        }
    }
}
