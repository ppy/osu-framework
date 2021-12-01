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
        /// <summary>
        /// Initialize a manager class to control Orientation on Android
        /// </summary>
        /// <param name="settingBindable">Screen orientation setting bindable</param>
        /// <param name="gameActivity">Game activity to initialize orientation control on</param>
        public AndroidOrientationManager(Activity gameActivity, Bindable<ScreenOrientation> settingBindable)
            : base(settingBindable)
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
        protected virtual AndroidPM.ScreenOrientation SettingToNativeOrientation(ScreenOrientation orientation)
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
                    return AndroidPM.ScreenOrientation.FullSensor;
                case ScreenOrientation.Auto:
                    return AndroidPM.ScreenOrientation.FullUser;
                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation), "Unknown ScreenOrientation enum member");
            }
        }

        protected override void OnScreenOrientationSettingChanged(ValueChangedEvent<ScreenOrientation> value)
        {
            gameActivity.RunOnUiThread(() =>
            {
                gameActivity.RequestedOrientation = SettingToNativeOrientation(value.NewValue);
            });
        }
        protected override void OnScreenOrientationLocked()
        {
            gameActivity.RunOnUiThread(() =>
            {
                gameActivity.RequestedOrientation = AndroidPM.ScreenOrientation.Locked;
            });
        }
    }
}
