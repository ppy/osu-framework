// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Android.App;
using Android.Content.PM;
using osu.Framework.Android;

namespace osu.Framework.Tests.Android
{
    [Activity(MainLauncher = true, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, Theme = "@android:style/Theme.NoTitleBar")]
    public class TestGameActivity : AndroidGameActivity
    {
        protected override Game CreateGame()
            => new VisualTestGame();
    }
}
