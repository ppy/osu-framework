// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Android.App;
using Android.OS;
using Android.Content.PM;
using Android.Views;

namespace SampleGame.Android
{
    [Activity(Label = "SampleGame", ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, MainLauncher = true, Theme = "@android:style/Theme.NoTitleBar")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Window.AddFlags(WindowManagerFlags.Fullscreen);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            SetContentView(new SampleGameView(this));
        }
    }
}
