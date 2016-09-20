// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Desktop.OS.Linux;
using osu.Framework.Desktop.OS.Windows;
using osu.Framework.OS;
using OpenTK.Graphics;

namespace osu.Framework.Desktop
{
    public static class Host
    {
        public static BasicGameHost GetSuitableHost()
        {
            GraphicsContextFlags flags = GraphicsContextFlags.Default;
            if (RuntimeInfo.IsUnix)
                return new LinuxGameHost(flags);
            else
                return new WindowsGameHost(flags);
        }
    }
}
