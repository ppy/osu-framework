// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osuTK.Graphics;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace osu.Framework.iOS
{
    public class IOSGameWindow : GameWindow
    {
        internal static IOSGameView GameView;

        public IOSGameWindow() : base(GameView)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            // TODO
        }

        public override IGraphicsContext Context => GameView.GraphicsContext;

        public override bool Focused => true;

        public override osuTK.WindowState WindowState { get => osuTK.WindowState.Normal; set { } }

        public override void Run()
        {
            // do nothing for iOS
        }

        public override void Run(double updateRate)
        {
            // do nothing for iOS
        }
    }
}
