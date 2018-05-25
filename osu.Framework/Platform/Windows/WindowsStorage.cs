﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration.LocalSettings;

namespace osu.Framework.Platform.Windows
{
    public class WindowsStorage : UserStorage
    {
        public WindowsStorage(string baseName)
            : base(baseName, new WindowsLocalSettingsManager(new LocalStorage()))
        {
        }
    }
}
