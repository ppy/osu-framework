// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Configuration;

namespace osu.Framework.Platform
{
    internal abstract class ScreenOrientationManager
    {
        private bool isLocked;

        private Bindable<ScreenOrientation> orientationBindable;

        protected ScreenOrientationManager(Bindable<ScreenOrientation> orientationSettingBindable)
        {
            orientationBindable = orientationSettingBindable;
            orientationBindable.BindValueChanged(value =>
            {
                if (isLocked) return;

                OnScreenOrientationSettingChanged(value);
            });
        }

        /// <summary>
        /// This event will be invoked when ScreenOrientation setting is changed
        /// </summary>
        /// <remarks>
        /// Override this event to set behavior when orientation setting is changed
        /// </remarks>
        protected abstract void OnScreenOrientationSettingChanged(ValueChangedEvent<ScreenOrientation> value);

        /// <summary>
        /// This event will be invoked when ScreenOrientation is locked
        /// </summary>
        /// <remarks>
        /// Override this event to set native behavior
        /// </remarks>
        protected abstract void OnScreenOrientationLocked();

        /// <summary>
        /// Lock/unlock the orientation to current orientation
        /// </summary>
        /// <param name="isLocked"><see langword="true"/> to lock the orientation and <see langword="false"/> to unlock</param>
        /// <exception cref="InvalidOperationException">Thrown if setting is disabled</exception>
        public void SetOrientationLock(bool isLocked)
        {
            if (orientationBindable.Disabled)
                throw new InvalidOperationException("Can't change screen orientation lock when setting is disabled");

            this.isLocked = isLocked;

            if (isLocked)
                OnScreenOrientationLocked();
            else
                orientationBindable.TriggerChange();
        }
    }
}
