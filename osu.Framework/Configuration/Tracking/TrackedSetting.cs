// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System;

namespace osu.Framework.Configuration.Tracking
{
    public abstract class TrackedSetting<U> : ITrackedSetting
    {
        public event Action<SettingDescription> SettingChanged;

        private readonly object setting;
        private readonly Func<U, SettingDescription> generateDescription;

        private Bindable<U> bindable;

        protected TrackedSetting(object setting, Func<U, SettingDescription> generateDescription)
        {
            this.setting = setting;
            this.generateDescription = generateDescription;
        }

        public void LoadFrom<T>(ConfigManager<T> configManager)
            where T : struct
        {
            bindable = configManager.GetBindable<U>((T)setting);
            bindable.ValueChanged += displaySetting;
        }

        public void Unload()
        {
            bindable.ValueChanged -= displaySetting;
        }

        private void displaySetting(U value) => SettingChanged?.Invoke(generateDescription(value));
    }
}
