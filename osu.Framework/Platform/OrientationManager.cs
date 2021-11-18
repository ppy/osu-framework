// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;

namespace osu.Framework.Platform
{
    public abstract class OrientationManager : Component
    {
        protected Bindable<Orientation> OrientationBindable;

        protected OrientationManager() { }
        public OrientationManager(FrameworkConfigManager config)
        {
            OrientationBindable = config.GetBindable<Orientation>(FrameworkSetting.Orientation);
            OrientationBindable.BindValueChanged(OrientationSettingChangedHandler);
        }

        public abstract void OrientationSettingChangedHandler(ValueChangedEvent<Orientation> value);
    }
}
