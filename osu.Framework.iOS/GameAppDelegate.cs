// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using UIKit;
using Foundation;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;

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
            aotImageSharp();

            Window = new UIWindow(UIScreen.MainScreen.Bounds);
            gameView = new iOSGameView(new RectangleF(0.0f, 0.0f, (float)Window.Frame.Size.Width, (float)Window.Frame.Size.Height));

            GameViewController viewController = new GameViewController
            {
                View = gameView
            };

            Window.RootViewController = viewController;
            Window.MakeKeyAndVisible();

            gameView.Run();

            host = new iOSGameHost(gameView);   
            host.Run(CreateGame());

            return true;
        }

        private void aotImageSharp()
        {
            System.Runtime.CompilerServices.Unsafe.SizeOf<Rgba32>();
            System.Runtime.CompilerServices.Unsafe.SizeOf<long>();
            try
            {
                new SixLabors.ImageSharp.Formats.Png.PngDecoder().Decode<Rgba32>(SixLabors.ImageSharp.Configuration.Default, null);
            } catch { }
        }
    }

    internal class GameViewController : UIViewController
    {
        public override void DidRotate(UIInterfaceOrientation fromInterfaceOrientation)
        {
            base.DidRotate(fromInterfaceOrientation);
            var gameView = View as iOSGameView;
            gameView?.RequestResizeFrameBuffer();
        }
    }
}
