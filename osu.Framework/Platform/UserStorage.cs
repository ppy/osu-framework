// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration.LocalSettings;
using System;

namespace osu.Framework.Platform
{
    public abstract class UserStorage : DesktopStorage
    {
        private readonly LocalSettingsManager configManager;

        protected override string LocateBasePath()
        {
            return configManager.Get<string>(LocalSetting.Path);
        }

        protected UserStorage(string baseName, LocalSettingsManager localSettings)
            : base(baseName)
        {
            configManager = localSettings;
            configManager.Load();
        }

        protected class LocalStorage : DesktopStorage
        {
            protected override string LocateBasePath()
            {
                return Environment.CurrentDirectory;
            }

            public LocalStorage()
                : base(string.Empty)
            {
            }
        }
    }
}
