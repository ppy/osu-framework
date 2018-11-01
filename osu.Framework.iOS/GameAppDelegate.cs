// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using UIKit;
using Foundation;
using System.Drawing;

namespace osu.Framework.iOS
{
    public abstract class GameAppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }

        private iOSGameView gameView;
        private iOSGameHost host;

        protected abstract Game CreateGame();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            Window = new UIWindow(UIScreen.MainScreen.Bounds);
            gameView = new iOSGameView(new RectangleF(0.0f, 0.0f, (float)Window.Frame.Size.Width, (float)Window.Frame.Size.Height));

            UIViewController viewController = new UIViewController
            {
                View = gameView
            };

            Window.RootViewController = viewController;
            Window.MakeKeyAndVisible();

            // gameView.RunWithFrameInterval(1);
            gameView.Run();

            host = new iOSGameHost(gameView);
            host.Run(CreateGame());

            return true;
        }
    }
}
