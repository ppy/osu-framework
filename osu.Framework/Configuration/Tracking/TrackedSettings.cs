// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;

namespace osu.Framework.Configuration.Tracking
{
    public class TrackedSettings : List<ITrackedSetting>
    {
        public event Action<SettingDescription> SettingChanged;

        public void LoadFrom<TLookup>(ConfigManager<TLookup> configManager)
            where TLookup : struct, Enum
        {
            foreach (var value in this)
            {
                value.LoadFrom(configManager);
                value.SettingChanged += d => SettingChanged?.Invoke(d);
            }
        }

        public void Unload()
        {
            foreach (var value in this)
                value.Unload();
        }
    }
}
