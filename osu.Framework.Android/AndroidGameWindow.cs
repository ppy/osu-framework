// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;
using WindowState = osuTK.WindowState;

namespace osu.Framework.Android
{
    public class AndroidGameWindow : OsuTKWindow
    {
        public override IGraphicsContext Context
            => View.GraphicsContext;

        internal static AndroidGameView View;

        public override bool Focused
            => true;

        public override WindowState WindowState
        {
            get => WindowState.Normal;
            set { }
        }

        public AndroidGameWindow()
            : base(View)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
        }

        protected override IEnumerable<WindowMode> DefaultSupportedWindowModes => new[]
        {
            Configuration.WindowMode.Fullscreen,
        };

        public override void Run()
        {
            View.Run();
        }

        public override void Run(double updateRate)
        {
            View.Run(updateRate);
        }

        protected override DisplayDevice CurrentDisplayDevice
        {
            get => DisplayDevice.Default;
            set => throw new InvalidOperationException();
        }
    }
}
