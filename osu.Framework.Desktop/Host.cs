// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Desktop.Platform.Linux;
using osu.Framework.Desktop.Platform.Windows;
using osu.Framework.Platform;
using OpenTK.Graphics;

namespace osu.Framework.Desktop
{
    public static class Host
    {
        public static BasicGameHost GetSuitableHost(string game)
        {
            GraphicsContextFlags flags = GraphicsContextFlags.Default;
            if (RuntimeInfo.IsUnix)
                return new LinuxGameHost(flags, game);
            else
                return new WindowsGameHost(flags, game);
        }
    }
}
