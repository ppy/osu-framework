// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
    }
}
