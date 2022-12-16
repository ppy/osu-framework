// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Desktop.Platform;
using osu.Framework.Desktop.Platform.Linux;
using osu.Framework.Desktop.Platform.MacOS;
using osu.Framework.Desktop.Platform.Window;

namespace osu.Framework.Desktop
{
    public static class DesktopHost
    {
        public static DesktopGameHost GetSuitableHost(string gameName, HostOptions hostOptions = null)
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    return new WindowsGameHost(gameName, hostOptions);

                case RuntimeInfo.Platform.Linux:
                    return new LinuxGameHost(gameName, hostOptions);

                case RuntimeInfo.Platform.macOS:
                    return new MacOSGameHost(gameName, hostOptions);

                default:
                    throw new InvalidOperationException($"Could not find a suitable host for the selected operating system ({RuntimeInfo.OS}).");
            }
        }
    }
}
