// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;

namespace osu.Framework.Android
{
    public abstract class AndroidGameActivity : Activity
    {
        protected abstract Game CreateGame();

        private AndroidGameView gameView;

        public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
        {
            base.OnTrimMemory(level);
            gameView.Host?.Collect();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(gameView = new AndroidGameView(this, CreateGame()));
        }

        protected override void OnPause() {
            base.OnPause();
            // Because Android is not playing nice with Background - we just kill it
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        /// <summary>
        /// Avoid the default implementation that does close the app.
        /// </summary>
        public override void OnBackPressed()
        {
        }

        /// <summary>
        /// There is the rare situation that the view does not get the events. So the view just gets triggered directly.
        /// </summary>
        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyDown(keyCode, e);
        }

        /// <summary>
        /// There is the rare situation that the view does not get the events. So the view just gets triggered directly.
        /// </summary>
        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyUp(keyCode, e);
        }

        /// <summary>
        /// There is the rare situation that the view does not get the events. So the view just gets triggered directly.
        /// </summary>
        public override bool OnKeyLongPress([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyLongPress(keyCode, e);
        }
    }
}
