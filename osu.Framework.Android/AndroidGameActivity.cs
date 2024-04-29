// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Java.Lang;
using ManagedBass;
using Org.Libsdl.App;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using Debug = System.Diagnostics.Debug;

namespace osu.Framework.Android
{
    // since `ActivityAttribute` can't be inherited, the below is only provided as an illustrative example of how to setup an activity for best compatibility.
    [Activity(ConfigurationChanges = DEFAULT_CONFIG_CHANGES, Exported = true, LaunchMode = DEFAULT_LAUNCH_MODE, MainLauncher = true)]
    public abstract class AndroidGameActivity : SDLActivity
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

        internal static AndroidGameSurface Surface => (AndroidGameSurface)MSurface!;

        private GameHost? host;

        protected abstract Game CreateGame();

        protected override string[] GetLibraries() => new string[] { "SDL3" };

        protected override SDLSurface CreateSDLSurface(Context? context) => new AndroidGameSurface(this, context);

        protected override IRunnable CreateSDLMainRunnable() => new Runnable(() =>
        {
            host = new AndroidGameHost(this);
            host.AllowScreenSuspension.Result.BindValueChanged(allow =>
            {
                RunOnUiThread(() =>
                {
                    if (!allow.NewValue)
                        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);
                    else
                        Window?.ClearFlags(WindowManagerFlags.KeepScreenOn);
                });
            }, true);

            host.Run(CreateGame());

            if (!IsFinishing)
                Finish();
        });

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            Debug.Assert(RuntimeInfo.EntryAssembly.IsNull(), "RuntimeInfo.EntryAssembly should be null on Android and therefore needs to be manually updated.");
            RuntimeInfo.EntryAssembly = GetType().Assembly;

            // The default current directory on android is '/'.
            // On some devices '/' maps to the app data directory. On others it maps to the root of the internal storage.
            // In order to have a consistent current directory on all devices the full path of the app data directory is set as the current directory.
            System.Environment.CurrentDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

            base.OnCreate(savedInstanceState);

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                Window.AsNonNull().Attributes.AsNonNull().LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }
        }

        public override void OnTrimMemory(TrimMemory level)
        {
            base.OnTrimMemory(level);
            host?.Collect();
        }

        protected override void OnStop()
        {
            base.OnStop();
            Bass.Pause();
        }

        protected override void OnRestart()
        {
            base.OnRestart();
            Bass.Start();
        }
    }
}
