// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Configuration;

namespace osu.Framework.Platform
{
    public abstract class ScreenOrientationManager
    {
        public Bindable<ScreenOrientation> OrientationBindable { get; protected set; }

        public ScreenOrientationManager(FrameworkConfigManager config)
        {
            OrientationBindable = config.GetBindable<ScreenOrientation>(FrameworkSetting.ScreenOrientation);
            OrientationBindable.BindValueChanged(OnScreenOrientationSettingChanged);
        }

        /// <summary>
        /// This event will be invoked when ScreenOrientation setting is changed
        /// </summary>
        /// <remarks>
        /// Override this event to customize behavior when orientation setting is changed
        /// </remarks>
        protected abstract void OnScreenOrientationSettingChanged(ValueChangedEvent<ScreenOrientation> value);
    }
}
