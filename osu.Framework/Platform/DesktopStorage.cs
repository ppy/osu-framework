// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Configuration;

namespace osu.Framework.Platform
{
    public class DesktopStorage : NativeStorage
    {
        public DesktopStorage(string baseName, DesktopGameHost host)
            : base(baseName, host)
        {
            if (host.IsPortableInstallation || File.Exists(FrameworkConfigManager.FILENAME))
            {
                BasePath = "./";
                BaseName = string.Empty;
            }
        }
    }
}
