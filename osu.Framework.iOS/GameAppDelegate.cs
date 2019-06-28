// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using UIKit;
using Foundation;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.iOS
{
    public abstract class GameAppDelegate : UIApplicationDelegate
    {
        public override UIWindow Window { get; set; }

        private IOSGameView gameView;
        private IOSGameHost host;

        protected abstract Game CreateGame();

        public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
        {
            aotImageSharp();

            Window = new UIWindow(UIScreen.MainScreen.Bounds);
            gameView = new IOSGameView(new RectangleF(0.0f, 0.0f, (float)Window.Frame.Size.Width, (float)Window.Frame.Size.Height));

            GameViewController viewController = new GameViewController
            {
                View = gameView
            };

            Window.RootViewController = viewController;
            Window.MakeKeyAndVisible();

            gameView.Run();

            host = new IOSGameHost(gameView);
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
            }
            catch
            {
            }
        }
    }
}
