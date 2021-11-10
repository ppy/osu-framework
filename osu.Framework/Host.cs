// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Platform;
using osu.Framework.Platform.Linux;
using osu.Framework.Platform.MacOS;
using osu.Framework.Platform.Windows;
using System;

namespace osu.Framework
{
    public static class Host
    {
        public static DesktopGameHost GetSuitableHost(string gameName, bool bindIPC = false, bool portableInstallation = false)
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.macOS:
                    return new MacOSGameHost(gameName, bindIPC, portableInstallation);

                case RuntimeInfo.Platform.Linux:
                    return new LinuxGameHost(gameName, bindIPC, portableInstallation);

                case RuntimeInfo.Platform.Windows:
                    return new WindowsGameHost(gameName, bindIPC, portableInstallation);

                default:
                    throw new InvalidOperationException($"Could not find a suitable host for the selected operating system ({Enum.GetName(typeof(RuntimeInfo.Platform), RuntimeInfo.OS)}).");
            }
        }
    }
}
