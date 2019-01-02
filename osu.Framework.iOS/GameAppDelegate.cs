// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using UIKit;
using Foundation;
using System.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using osuTK.Input;

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

            gameView.FileDrop += fileDrop;

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


        public override bool OpenUrl(UIApplication app, NSUrl url, NSDictionary options)
        {
            if (url.IsFileUrl) // Built-in check whether Url is actually a file path. 
            {
                gameView.OnFileDrop(url.Path);
            } else if (url.ToString().StartsWith("osu://"))
            {
                //TODO: Handle osu URLs
            }
            return true;
        }

        public abstract void fileDrop(object sender, FileDropEventArgs e);
    }
}
