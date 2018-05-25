// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Platform;
using System;
using System.IO;

namespace osu.Framework.Configuration.LocalSettings
{
    public class LinuxLocalSettingsManager : LocalSettingsManager
    {
        protected override string GetDefaultPath()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            string xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            string[] paths =
            {
                xdg ?? Path.Combine(home, ".local", "share"),
                Path.Combine(home)
            };

            foreach (string path in paths)
            {
                if (Directory.Exists(path))
                    return path;
            }

            return paths[0];
        }

        public LinuxLocalSettingsManager(Storage storage)
            : base(storage)
        {
        }
    }
}
