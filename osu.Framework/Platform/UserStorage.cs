// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
