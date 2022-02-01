// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using ManagedBass;
using Debug = System.Diagnostics.Debug;
using Environment = System.Environment;

namespace osu.Framework.Android
{
    // since `ActivityAttribute` can't be inherited, the below is only provided as an illustrative example of how to setup an activity for best compatibility.
    [Activity(ConfigurationChanges = DEFAULT_CONFIG_CHANGES, LaunchMode = DEFAULT_LAUNCH_MODE, MainLauncher = true)]
    public abstract class AndroidGameActivity : Activity
    {
        protected const ConfigChanges DEFAULT_CONFIG_CHANGES = ConfigChanges.Keyboard
                                                               | ConfigChanges.KeyboardHidden
                                                               | ConfigChanges.Navigation
                                                               | ConfigChanges.Orientation
                                                               | ConfigChanges.ScreenLayout
                                                               | ConfigChanges.ScreenSize
                                                               | ConfigChanges.SmallestScreenSize
                                                               | ConfigChanges.Touchscreen
                                                               | ConfigChanges.UiMode;

        protected const LaunchMode DEFAULT_LAUNCH_MODE = LaunchMode.SingleInstance;

        protected abstract Game CreateGame();

        /// <summary>
        /// The visibility flags for the system UI (status and navigation bars)
        /// </summary>
        public SystemUiFlags UIVisibilityFlags
        {
            get
            {
                Debug.Assert(Window != null);

                return (SystemUiFlags)Window.DecorView.SystemUiVisibility;
            }
            set
            {
                Debug.Assert(Window != null);

                systemUiFlags = value;
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)value;
            }
        }

        private SystemUiFlags systemUiFlags;

        private AndroidGameView gameView;

        public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
        {
            base.OnTrimMemory(level);
            gameView.Host?.Collect();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            // The default current directory on android is '/'.
            // On some devices '/' maps to the app data directory. On others it maps to the root of the internal storage.
            // In order to have a consistent current directory on all devices the full path of the app data directory is set as the current directory.
            Environment.CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

            base.OnCreate(savedInstanceState);

            SetContentView(gameView = new AndroidGameView(this, CreateGame()));

            UIVisibilityFlags = SystemUiFlags.LayoutFlags | SystemUiFlags.ImmersiveSticky | SystemUiFlags.HideNavigation;

            Debug.Assert(Window != null);

            // Firing up the on-screen keyboard (eg: interacting with textboxes) may cause the UI visibility flags to be altered thus showing the navigation bar and potentially the status bar
            // This sets back the UI flags to hidden once the interaction with the on-screen keyboard has finished.
            Window.DecorView.SystemUiVisibilityChange += (_, e) =>
            {
                if ((SystemUiFlags)e.Visibility != systemUiFlags)
                {
                    UIVisibilityFlags = systemUiFlags;
                }
            };

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P && Window.Attributes != null)
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;

            gameView.HostStarted += host =>
            {
                host.AllowScreenSuspension.Result.BindValueChanged(allow =>
                {
                    RunOnUiThread(() =>
                    {
                        if (!allow.NewValue)
                            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
                        else
                            Window.ClearFlags(WindowManagerFlags.KeepScreenOn);
                    });
                }, true);

                host.ScreenOrientation.BindValueChanged(e => updateOrientation(), true);

                host.LockScreenOrientation.BindValueChanged(e =>
                {
                    if (host.ScreenOrientation.Disabled)
                        throw new InvalidOperationException("Can't change screen orientation lock when setting is disabled");

                    updateOrientation();
                });
            };
        }

        private void updateOrientation() => RunOnUiThread(() =>
        {
            RequestedOrientation = gameView.Host.LockScreenOrientation.Value
                ? ScreenOrientation.Locked
                : configToNativeOrientationEnum(gameView.Host.ScreenOrientation.Value);
        });

        protected override void OnPause()
        {
            base.OnPause();
            gameView.Host?.Suspend();
            Bass.Pause();
        }

        protected override void OnResume()
        {
            base.OnResume();
            gameView.Host?.Resume();
            Bass.Start();
        }

        public override void OnBackPressed()
        {
            // Avoid the default implementation that does close the app.
            // This only happens when the back button could not be captured from OnKeyDown.
        }

        // On some devices and keyboard combinations the OnKeyDown event does not propagate the key event to the view.
        // Here it is done manually to ensure that the keys actually land in the view.

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyUp(keyCode, e);
        }

        public override bool OnKeyLongPress([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyLongPress(keyCode, e);
        }

        /// <summary>
        /// Convert screen orientation framework config enum to equivalent Android's native orientation enum
        /// </summary>
        /// <param name="orientation">Framework setting enum to convert</param>
        /// <returns><see cref="ScreenOrientation"/> enum to use with Android SDK</returns>
        private static ScreenOrientation configToNativeOrientationEnum(Configuration.ScreenOrientation orientation)
        {
            switch (orientation)
            {
                case Configuration.ScreenOrientation.AnyLandscape:
                    return ScreenOrientation.SensorLandscape;

                case Configuration.ScreenOrientation.AnyPortrait:
                    return ScreenOrientation.SensorPortrait;

                case Configuration.ScreenOrientation.LandscapeLeft:
                    return ScreenOrientation.ReverseLandscape;

                case Configuration.ScreenOrientation.LandscapeRight:
                    return ScreenOrientation.Landscape;

                case Configuration.ScreenOrientation.Portrait:
                    return ScreenOrientation.Portrait;

                case Configuration.ScreenOrientation.ReversePortrait:
                    return ScreenOrientation.ReversePortrait;

                case Configuration.ScreenOrientation.Any:
                    return ScreenOrientation.FullSensor;

                case Configuration.ScreenOrientation.Auto:
                    return ScreenOrientation.FullUser;

                default:
                    throw new ArgumentOutOfRangeException(nameof(orientation), "Unknown framework config ScreenOrientation enum member");
            }
        }
    }
}
