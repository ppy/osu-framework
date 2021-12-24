// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.App;
using Android.OS;
using Android.Views;
using osu.Framework.Android;

namespace osu.Framework.Tests.Android
{
    [Activity(ConfigurationChanges = DEFAULT_CONFIG_CHANGES, LaunchMode = DEFAULT_LAUNCH_MODE, MainLauncher = true)]
    public class TestGameActivity : AndroidGameActivity
    {
        protected override Game CreateGame()
            => new VisualTestGame();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Window.AddFlags(WindowManagerFlags.Fullscreen);
        }
    }
}
