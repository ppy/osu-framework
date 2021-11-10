// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameWindow : OsuTKWindow
    {
        private readonly AndroidGameView view;

        public override IGraphicsContext Context => view.GraphicsContext;

        public override bool Focused => true;

        public override Platform.WindowState WindowState
        {
            get => Platform.WindowState.Normal;
            set { }
        }

        public AndroidGameWindow(AndroidGameView view)
            : base(view)
        {
            this.view = view;
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
            view.Run();
        }

        protected override DisplayDevice CurrentDisplayDevice
        {
            get => DisplayDevice.Default;
            set => throw new InvalidOperationException();
        }
    }
}
