// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Platform;

namespace osu.Framework.Configuration.LocalSettings
{
    public abstract class LocalSettingsManager : IniConfigManager<LocalSetting>
    {
        protected override string Filename => @"settings.ini";

        protected abstract string GetDefaultPath();

        protected LocalSettingsManager(Storage storage)
            : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            Set(LocalSetting.Path, GetDefaultPath());
        }
    }

    public enum LocalSetting
    {
        Path
    }
}
