// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Java.Lang;
using ManagedBass;
using Org.Libsdl.App;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Platform;
using SDL;
using Debug = System.Diagnostics.Debug;

namespace osu.Framework.Android
{
    // since `ActivityAttribute` can't be inherited, the below is only provided as an illustrative example of how to setup an activity for best compatibility.
    [Activity(ConfigurationChanges = DEFAULT_CONFIG_CHANGES,
              Exported = true,
              LaunchMode = DEFAULT_LAUNCH_MODE,
              HardwareAccelerated = true,
              MainLauncher = true,
              ScreenOrientation = ScreenOrientation.Landscape)]
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

        protected abstract Game CreateGame();

        private Game? game;

        private GameHost? host;

        /// <summary>
        /// Whether this <see cref="AndroidGameActivity"/> is active (in the foreground).
        /// </summary>
        public BindableBool IsActive { get; } = new BindableBool();

        /// <summary>
        /// The visibility flags for the system UI (status and navigation bars)
        /// </summary>
        public SystemUiFlags UIVisibilityFlags
        {
#pragma warning disable 618 // SystemUiVisibility is deprecated
            get => (SystemUiFlags)Window.AsNonNull().DecorView.SystemUiVisibility;
            set
            {
                systemUiFlags = value;
                Window.AsNonNull().DecorView.SystemUiVisibility = (StatusBarVisibility)value;
#pragma warning restore 618
            }
        }

        private SystemUiFlags systemUiFlags;

        public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
        {
            base.OnTrimMemory(level);
            host?.Collect();
        }

        protected override string[] GetLibraries() => new string[] { "SDL3" };

        protected override SDLSurface CreateSDLSurface(Context? context) => new AndroidGameSurface(this, context);

        protected override IRunnable CreateSDLMainRunnable() => new Runnable(() =>
        {
            // blocks back button
            SDL3.SDL_SetHint(SDL3.SDL_HINT_ANDROID_TRAP_BACK_BUTTON, "1"u8);

            // hints are here because they don't apply well in another location such as SDL2Window

            host = new AndroidGameHost(this);
            game = CreateGame();

            host.Run(game);

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

            UIVisibilityFlags = SystemUiFlags.LayoutFlags | SystemUiFlags.LayoutFullscreen | SystemUiFlags.ImmersiveSticky | SystemUiFlags.HideNavigation | SystemUiFlags.Fullscreen;

            RequestedOrientation = ScreenOrientation.Landscape;

            // Firing up the on-screen keyboard (eg: interacting with textboxes) may cause the UI visibility flags to be altered thus showing the navigation bar and potentially the status bar
            // This sets back the UI flags to hidden once the interaction with the on-screen keyboard has finished.
            Window.AsNonNull().DecorView.SystemUiVisibilityChange += (_, e) =>
            {
                if ((SystemUiFlags)e.Visibility != systemUiFlags)
                {
                    UIVisibilityFlags = systemUiFlags;
                }
            };

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                Window.AsNonNull().Attributes.AsNonNull().LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
            }

            base.OnCreate(savedInstanceState);
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

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            IsActive.Value = hasFocus;
        }
    }
}
