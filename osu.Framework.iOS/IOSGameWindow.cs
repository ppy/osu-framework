// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osu.Framework.Graphics;
using System;
using System.Collections.Generic;
using osu.Framework.Bindables;

namespace osu.Framework.iOS
{
    public class IOSGameWindow : GameWindow
    {
        internal static IOSGameView GameView;

        private readonly BindableMarginPadding safeAreaPadding = new BindableMarginPadding();

        public override IBindable<MarginPadding> SafeAreaPadding => safeAreaPadding;

        public IOSGameWindow()
            : base(GameView)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            Resize += onResize;
            // for now, let's just say the cursor is always in the window.
            CursorInWindow = true;
        }

        public override IGraphicsContext Context => GameView.GraphicsContext;

        public override bool Focused => true;

        public override osuTK.WindowState WindowState
        {
            get => osuTK.WindowState.Normal;
            set { }
        }

        protected override IEnumerable<WindowMode> DefaultSupportedWindowModes => new[]
        {
            Configuration.WindowMode.Fullscreen,
        };

        public override void Run()
        {
            // do nothing for iOS
        }

        public override void Run(double updateRate)
        {
            // do nothing for iOS
        }

        private void onResize(object sender, EventArgs e)
        {
            safeAreaPadding.Value = new MarginPadding
            {
                Top = (float)GameView.SafeArea.Top * GameView.Scale,
                Left = (float)GameView.SafeArea.Left * GameView.Scale,
                Bottom = (float)GameView.SafeArea.Bottom * GameView.Scale,
                Right = (float)GameView.SafeArea.Right * GameView.Scale
            };
        }
    }
}
