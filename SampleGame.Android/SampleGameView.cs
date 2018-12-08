using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using osu.Framework;
using osu.Framework.Android;

namespace SampleGame.Android
{
    public class SampleGameView : AndroidGameView
    {
        public SampleGameView(Context context, IAttributeSet attrs) :
            base(context, attrs)
        {
            CreateGame();
        }

        public override Game CreateGame() => new SampleGameGame();
    }
}
