// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.App;
using osu.Framework.Android;
using TemplateGame.Game;

namespace TemplateGame.Android
{
    [Activity(ConfigurationChanges = DEFAULT_CONFIG_CHANGES, Exported = true, LaunchMode = DEFAULT_LAUNCH_MODE, MainLauncher = true)]
    public class TemplateGameActivity : AndroidGameActivity
    {
        protected override osu.Framework.Game CreateGame() => new TemplateGameGame();
    }
}
