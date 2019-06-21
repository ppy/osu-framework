// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.App;
using Android.OS;

namespace osu.Framework.Android
{
    public abstract class AndroidGameActivity : Activity
    {
        protected abstract Game CreateGame();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(new AndroidGameView(this, CreateGame()));
        }

        protected override void OnPause() {
            base.OnPause();
            // Because Android is not playing nice with Background - we just kill it
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
