// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

            // for now, let's just say the cursor is always in the window.
            CursorInWindow = true;
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
