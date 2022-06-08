﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using ManagedBass;
using osu.Framework.Bindables;
using Debug = System.Diagnostics.Debug;
using Res = Android.Content.Res;

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
        /// Whether this <see cref="AndroidGameActivity"/> is active (in the foreground).
        /// </summary>
        public BindableBool IsActive { get; } = new BindableBool();

        /// <summary>
        /// Whether the current screen is tablet-sized.
        /// </summary>
        /// <remarks>
        /// This can potentially change when switching displays (eg. on folding phones).
        /// Screens with at least medium width (as defined in https://developer.android.com/guide/topics/large-screens/support-different-screen-sizes)
        /// are considered tablet-sized.
        /// </remarks>
        public BindableBool IsTablet { get; } = new BindableBool();

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
            System.Environment.CurrentDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

            base.OnCreate(savedInstanceState);

            SetContentView(gameView = new AndroidGameView(this, CreateGame()));

            UIVisibilityFlags = SystemUiFlags.LayoutFlags | SystemUiFlags.ImmersiveSticky | SystemUiFlags.HideNavigation | SystemUiFlags.Fullscreen;

            updateIsTablet();

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

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                Debug.Assert(Window.Attributes != null);
                Window.Attributes.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }

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
            };
        }

        protected override void OnStop()
        {
            base.OnStop();
            gameView.Host?.Suspend();
            Bass.Pause();
        }

        protected override void OnRestart()
        {
            base.OnRestart();
            gameView.Host?.Resume();
            Bass.Start();
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            IsActive.Value = hasFocus;
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

        public override void OnConfigurationChanged(Res.Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            updateIsTablet();
        }

        /// <summary>
        /// Update <see cref="IsTablet"/> value.
        /// </summary>
        private void updateIsTablet()
        {
            if (WindowManager?.DefaultDisplay == null || Resources?.DisplayMetrics == null) return;

            // Width computed according to https://developer.android.com/guide/topics/large-screens/support-different-screen-sizes#java.
            // Except that we use Display.GetRealSize() instead of WindowMetrics to support older devices and to get actual display size.

            Point displaySize = new Point();
            WindowManager.DefaultDisplay.GetRealSize(displaySize);
            float smallestWidthDp = Math.Min(displaySize.X, displaySize.Y) / Resources.DisplayMetrics.Density;
            IsTablet.Value = smallestWidthDp >= 600f;
        }
    }
}
