// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;

namespace osu.Framework.Configuration.Tracking
{
    public interface ITrackedSetting
    {
        /// <summary>
        /// Invoked when this setting has changed.
        /// </summary>
        event Action<SettingDescription> SettingChanged;

        /// <summary>
        /// Begins tracking this setting.
        /// </summary>
        /// <param name="configManager">The <see cref="ConfigManager{T}"/> to track from.</param>
        void LoadFrom<T>(ConfigManager<T> configManager)
            where T : struct;

        /// <summary>
        /// Stops tracking this setting.
        /// </summary>
        void Unload();
    }
}
