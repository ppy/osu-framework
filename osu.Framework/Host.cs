// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Diagnostics;
using osu.Framework.Platform;
using osu.Framework.Platform.Linux;
using osu.Framework.Platform.MacOS;
using osu.Framework.Platform.Windows;

namespace osu.Framework
{
    public static class Host
    {
        public static DesktopGameHost GetSuitableDesktopHost(string gameName, HostOptions hostOptions = null)
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    Debug.Assert(OperatingSystem.IsWindows());
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
