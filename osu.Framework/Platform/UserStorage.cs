using osu.Framework.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace osu.Framework.Platform
{
    public class UserStorage : DesktopStorage
    {
        private readonly LocalSettingsManager configManager = new LocalSettingsManager();

        protected override string LocateBasePath()
        {
            return configManager.Get<string>(LocalSetting.Path);
        }

        public UserStorage()
            : base(string.Empty)
        {
            configManager.Load();
        }
    }
}
