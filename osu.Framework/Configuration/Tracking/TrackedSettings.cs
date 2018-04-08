// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Configuration.Tracking
{
    public class TrackedSettings : List<ITrackedSetting>
    {
        public event Action<SettingDescription> SettingChanged;

        public void LoadFrom<T>(ConfigManager<T> configManager)
            where T : struct
        {
            ForEach(value =>
            {
                value.LoadFrom(configManager);
                value.SettingChanged += d => SettingChanged?.Invoke(d);
            });
        }

        public void Unload()
        {
            ForEach(value => value.Unload());
        }
    }
}
