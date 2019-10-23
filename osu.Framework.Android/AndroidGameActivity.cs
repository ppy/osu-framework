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

        public override void OnBackPressed()
        {
            // Avoid the default implementation that does close the app.
            // This only happens when the back button could not be captured from OnKeyDown.
        }

        // On some devices and keyboard combinations the OnKeyDown event does not propagate the key event to the view.
        // Here it is done manually to ensure that the keys actually land in the view.

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyUp(keyCode, e);
        }

        public override bool OnKeyLongPress([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            return gameView.OnKeyLongPress(keyCode, e);
        }
    }
}
