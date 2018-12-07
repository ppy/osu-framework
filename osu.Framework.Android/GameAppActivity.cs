using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace osu.Framework.Android
{
    [Activity(Label = "GameAppActivity")]
    public class GameAppActivity : Activity
    {
        private AndroidGameView view;
        private AndroidGameHost host;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
        }
    }
}
