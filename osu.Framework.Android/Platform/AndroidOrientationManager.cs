// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Content.PM;
using osu.Framework.Platform;
using osu.Framework.Configuration;
using osu.Framework.Bindables;
using Android.App;

namespace osu.Framework.Android.Platform
{
    public class AndroidOrientationManager : OrientationManager
    {
        public ScreenOrientation CurrentScreenOrientation { get; internal set; }

        private AndroidOrientationManager() { }
        /// <summary>
        /// Initialize a manager class to control Orientation on Android
        /// </summary>
        /// <param name="config">Framework config manager used to get the orientation setting</param>
        /// <param name="gameActivity">Game activity to initialize orientation control on</param>
        public AndroidOrientationManager(FrameworkConfigManager config, Activity gameActivity)
            : base(config)
        {
            GameActivity = gameActivity;
        }

        /// <summary>
        /// Get/set the game activity that have its orientation controlled.
        /// </summary>
        protected Activity GameActivity { get; set; }

        /// <summary>
        /// Convert <see cref="Orientation"/> framework setting enum to Android's <see cref="ScreenOrientation"/> enum
        /// </summary>
        /// <remarks>
        /// Override this to customize orientation flags
        /// </remarks>
        /// <param name="orientation">Orientation</param>
        /// <returns><see cref="ScreenOrientation"/> enum to use with Android SDK</returns>
        public virtual ScreenOrientation OrientationToScreenOrientation(Orientation orientation)
        {
            switch (orientation)
            {
                case Orientation.Landscape:
                    return ScreenOrientation.UserLandscape;
                case Orientation.Portrait:
                    return ScreenOrientation.UserPortrait;
                case Orientation.Auto:
                default:
                    return ScreenOrientation.FullUser;
            }
        }

        /// <summary>
        /// Override this to customize behavior when orientation setting is changed
        /// </summary>
        public override void OrientationSettingChangedHandler(ValueChangedEvent<Orientation> value)
        {
            GameActivity.RunOnUiThread(() =>
            {
                CurrentScreenOrientation = OrientationToScreenOrientation(value.NewValue);
                GameActivity.RequestedOrientation = CurrentScreenOrientation;
            });
        }
    }
}
