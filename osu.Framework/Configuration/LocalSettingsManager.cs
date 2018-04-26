// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Platform;
using System;
using System.IO;

namespace osu.Framework.Configuration
{
    public class LocalSettingsManager : IniConfigManager<LocalSetting>
    {
        protected override string Filename => @"settings.ini";

        public LocalSettingsManager(Storage storage)
            : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.MacOsx:
                    Set(LocalSetting.Path, Path.Combine(getLinuxMacDefaultFolder(), "osu!lazer"));
                    break;
                case RuntimeInfo.Platform.Linux:
                    Set(LocalSetting.Path, Path.Combine(getLinuxMacDefaultFolder(), "osu!lazer"));
                    break;
                case RuntimeInfo.Platform.Windows:
                    Set(LocalSetting.Path, Path.Combine(getWindowsDefaultFolder(), "osu!lazer"));
                    break;
                default:
                    throw new InvalidOperationException($"Could not find a suitable default path for the selected operating system ({Enum.GetName(typeof(RuntimeInfo.Platform), RuntimeInfo.OS)}).");
            }
        }

        private String getWindowsDefaultFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        private String getLinuxMacDefaultFolder()
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
    }

    public enum LocalSetting
    {
        Path
    }
}
