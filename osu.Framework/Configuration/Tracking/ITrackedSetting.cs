// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration.Tracking
{
    /// <summary>
    /// A singular tracked setting.
    /// </summary>
    public interface ITrackedSetting
    {
        /// <summary>
        /// Invoked when this setting has changed.
        /// </summary>
        event Action<SettingDescription> SettingChanged;

        /// <summary>
        /// Loads a <see cref="Bindable{T}"/> into this tracked setting, binding to <see cref="SettingChanged"/>.
        /// </summary>
        /// <param name="configManager">The <see cref="ConfigManager{T}"/> to load from.</param>
        void LoadFrom<T>(ConfigManager<T> configManager)
            where T : struct;

        /// <summary>
        /// Unloads the <see cref="Bindable{T}"/> from this tracked setting, unbinding from <see cref="SettingChanged"/>.
        /// </summary>
        void Unload();
    }
}
