// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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

using osu.Framework.Android;

namespace osu.Framework.Tests.Android
{
    public class TestGameView : AndroidGameView
    {
        public override Game CreateGame() => new VisualTestGame();
    }
}
