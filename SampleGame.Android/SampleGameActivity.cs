﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.App;
using osu.Framework;
using osu.Framework.Android;

namespace SampleGame.Android
{
    [Activity(ConfigurationChanges = DEFAULT_CONFIG_CHANGES, Exported = true, LaunchMode = DEFAULT_LAUNCH_MODE, MainLauncher = true)]
    public class SampleGameActivity : AndroidGameActivity
    {
        protected override Game CreateGame() => new SampleGameGame();
    }
}
