// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.App;
using FlappyDon.Game;
using osu.Framework.Android;

namespace FlappyDon.Android
{
    [Activity(ConfigurationChanges = DEFAULT_CONFIG_CHANGES, Exported = true, LaunchMode = DEFAULT_LAUNCH_MODE, MainLauncher = true)]
    public class FlappyDonActivity : AndroidGameActivity
    {
        protected override osu.Framework.Game CreateGame() => new FlappyDonGame();
    }
}
