// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Platform;
using osu.Framework.Platform.Linux;
using osu.Framework.Platform.Windows;

namespace osu.Framework
{
    public static class Host
    {
        public static DesktopGameHost GetSuitableHost(string gameName, bool bindIPC = false)
        {
            if (RuntimeInfo.IsUnix)
                return new LinuxGameHost(gameName, bindIPC);

            return new WindowsGameHost(gameName, bindIPC);
        }
    }
}
