// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Android.App;
using Android.OS;
using Android.Widget;
using osu.Framework.Android;
using System;

namespace SampleGame.Android
{
    [Activity(Label = "SampleGame", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            //Button button = FindViewById<Button>(Resource.Id.button1);
            //button.Click += OnClick;
        }
        /*private void OnClick(object sender, EventArgs e)
        {
            SampleGameView view = new SampleGameView(this);
            SetContentView(view);
        }*/
    }
}
