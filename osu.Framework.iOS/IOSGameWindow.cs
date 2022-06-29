// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using WindowState = osu.Framework.Platform.WindowState;

namespace osu.Framework.iOS
{
    public class IOSGameWindow : OsuTKWindow
    {
        [NotNull]
        private readonly IOSGameView gameView;

        public override void SetupWindow(FrameworkConfigManager config)
        {
            Resize += onResize;
        }

        public IOSGameWindow([NotNull] IOSGameView gameView)
            : base(gameView)
        {
            this.gameView = gameView;
        }

        public override IGraphicsContext Context => gameView.GraphicsContext;

        public override bool Focused => true;

        public override IBindable<bool> IsActive { get; } = new BindableBool(true);

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

        private void onResize(object sender, EventArgs e)
        {
            SafeAreaPadding.Value = new MarginPadding
            {
                Top = (float)gameView.SafeArea.Top * gameView.Scale,
                Left = (float)gameView.SafeArea.Left * gameView.Scale,
                Bottom = (float)gameView.SafeArea.Bottom * gameView.Scale,
                Right = (float)gameView.SafeArea.Right * gameView.Scale
            };
        }
    }
}
