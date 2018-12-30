// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Android.Content;
using Android.Util;
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

        public SampleGameView(Context context) : base(context)
        {
            CreateGame();
        }

        public override Game CreateGame() => new SampleGameGame();
    }
}
