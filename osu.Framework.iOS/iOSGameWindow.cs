// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osuTK.Graphics;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Framework.iOS
{
    public class iOSGameWindow : GameWindow
    {
        public static iOSGameView GameView;

        public iOSGameWindow() : base(GameView)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            //throw new NotImplementedException();
        }


        public override IGraphicsContext Context => GameView.GraphicsContext;
    }
}
