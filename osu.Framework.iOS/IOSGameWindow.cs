// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using WindowState = osuTK.WindowState;

namespace osu.Framework.iOS
{
    public class IOSGameWindow : OsuTKWindow
    {
        internal static IOSGameView GameView;

        public IOSGameWindow()
            : base(GameView)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            Resize += onResize;
        }

        public override IGraphicsContext Context => GameView.GraphicsContext;

        public override bool Focused => true;

        public override WindowState WindowState
        {
            get => WindowState.Normal;
            set { }
        }

        protected override DisplayDevice CurrentDisplayDevice
        {
            get => DisplayDevice.Default;
            set => throw new InvalidOperationException();
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
            SafeAreaPadding.Value = new MarginPadding
            {
                Top = (float)GameView.SafeArea.Top * GameView.Scale,
                Left = (float)GameView.SafeArea.Left * GameView.Scale,
                Bottom = (float)GameView.SafeArea.Bottom * GameView.Scale,
                Right = (float)GameView.SafeArea.Right * GameView.Scale
            };
        }
    }
}
