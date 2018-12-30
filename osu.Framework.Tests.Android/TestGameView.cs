// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Android.Content;
using Android.Util;

using osu.Framework.Android;

namespace osu.Framework.Tests.Android
{
    public class TestGameView : AndroidGameView
    {
        public TestGameView(Context context) : base(context)
        {
        }

        public TestGameView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public override Game CreateGame() => new VisualTestGame();
    }
}
