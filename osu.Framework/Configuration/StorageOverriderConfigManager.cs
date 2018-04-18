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
            Set(StorageConfig.Path, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "osu!lazer"));
        }

    }

    public enum StorageConfig
    {
        Path
    }

}
