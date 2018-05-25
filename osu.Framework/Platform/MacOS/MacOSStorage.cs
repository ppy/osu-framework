// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration.LocalSettings;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSStorage : UserStorage
    {
        public MacOSStorage(string baseName)
            : base(baseName, new MacOSLocalSettingsManager(new LocalStorage()))
        {
        }
    }
}
