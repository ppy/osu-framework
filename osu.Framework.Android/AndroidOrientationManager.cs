// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using AndroidPM = Android.Content.PM;
using osu.Framework.Platform;
using osu.Framework.Configuration;
using osu.Framework.Bindables;
using Android.App;

namespace osu.Framework.Android
{
    public class AndroidOrientationManager : ScreenOrientationManager
    {
        private bool isLocked = false;
        /// <summary>
        /// Initialize a manager class to control Orientation on Android
        /// </summary>
        /// <param name="config">Framework config manager used to get the orientation setting</param>
        /// <param name="gameActivity">Game activity to initialize orientation control on</param>
        public AndroidOrientationManager(FrameworkConfigManager config, Activity gameActivity)
            : base(config)
        {
            this.gameActivity = gameActivity;
        }

        private Activity gameActivity { get; set; }

        /// <summary>
        /// Convert <see cref="ScreenOrientation"/> framework setting enum to Android's <see cref="AndroidPM.ScreenOrientation"/> enum
        /// </summary>
        /// <remarks>
        /// Override this to customize orientation flags
        /// </remarks>
        /// <param name="orientation">Orientation</param>
        /// <returns><see cref="AndroidPM.ScreenOrientation"/> enum to use with Android SDK</returns>
        public virtual AndroidPM.ScreenOrientation SettingToNativeOrientation(ScreenOrientation orientation)
        {
            switch (orientation)
            {
                case ScreenOrientation.AnyLandscape:
                    return AndroidPM.ScreenOrientation.UserLandscape;
                case ScreenOrientation.AnyPortrait:
                    return AndroidPM.ScreenOrientation.UserPortrait;
                case ScreenOrientation.LandscapeLeft:
                    return AndroidPM.ScreenOrientation.ReverseLandscape;
                case ScreenOrientation.LandscapeRight:
                    return AndroidPM.ScreenOrientation.Landscape;
                case ScreenOrientation.Portrait:
                    return AndroidPM.ScreenOrientation.Portrait;
                case ScreenOrientation.ReversePortrait:
                    return AndroidPM.ScreenOrientation.ReversePortrait;
                case ScreenOrientation.Any:
                    return AndroidPM.ScreenOrientation.Sensor;
                case ScreenOrientation.Auto:
                    return AndroidPM.ScreenOrientation.FullUser;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation), "Unknown ScreenOrientation enum member");
            }
        }

        protected override void OnScreenOrientationSettingChanged(ValueChangedEvent<ScreenOrientation> value)
        {
            if (isLocked) return;
            gameActivity.RunOnUiThread(() =>
            {
                gameActivity.RequestedOrientation = SettingToNativeOrientation(value.NewValue);
            });
        }

        /// <summary>
        /// Lock/unlock the orientation to current orientation
        /// </summary>
        /// <param name="isLocked"><see langword="true"/> to lock the orientation and <see langword="false"/> to unlock</param>
        public void SetOrientationLock(bool isLocked)
        {
            this.isLocked = isLocked;
            gameActivity.RunOnUiThread(() =>
            {
                if (isLocked)
                    gameActivity.RequestedOrientation = AndroidPM.ScreenOrientation.Locked;
                else
                    OrientationBindable.TriggerChange();
            });
        }
    }
}
