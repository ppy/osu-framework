// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameWindow : GameWindow
    {
        public static AndroidGameView GameView;

        public AndroidGameWindow() : base(null)//base(GameView)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            //throw new NotImplementedException();
        }


        public override IGraphicsContext Context => throw new NotImplementedException();// GameView.GraphicsContext;
    }
}
