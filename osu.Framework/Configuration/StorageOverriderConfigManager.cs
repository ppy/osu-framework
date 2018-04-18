using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace osu.Framework.Configuration
{
    class StorageOverriderConfigManager : LocalIniConfigManager<StorageConfig>
    {
        
        protected override void InitialiseDefaults()
        {

            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.MacOsx:
                    Set(StorageConfig.Path, Path.Combine(GetLinuxMacDefaultFolder(), "osu!lazer"));
                    break;
                case RuntimeInfo.Platform.Linux:
                    Set(StorageConfig.Path, Path.Combine(GetLinuxMacDefaultFolder(), "osu!lazer"));
                    break;
                case RuntimeInfo.Platform.Windows:
                    Set(StorageConfig.Path, Path.Combine(GetWindowsDefaultFolder(), "osu!lazer"));
                    break;
                default:
                    throw new InvalidOperationException($"Could not find a suitable default path for the selected operating system ({Enum.GetName(typeof(RuntimeInfo.Platform), RuntimeInfo.OS)}).");
            }

            
        }

        private String GetWindowsDefaultFolder()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        private String GetLinuxMacDefaultFolder()
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

    public enum StorageConfig
    {
        Path
    }

}
