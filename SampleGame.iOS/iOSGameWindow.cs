using System;
using OpenTK.Platform.iPhoneOS;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace SampleGame.iOS
{
    public class iOSGameWindow : GameWindow
    {
        public iOSGameWindow(iPhoneOSGameView gameView)
            : base(new iOSPlatformGameWindow(gameView))
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            //throw new NotImplementedException();
        }
    }
}
