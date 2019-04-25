// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Configuration;
using osu.Framework.Platform;
using osuTK.Graphics;
using System;
using System.Collections.Generic;

namespace osu.Framework.Android
{
    public class AndroidGameWindow : GameWindow
    {
        public override IGraphicsContext Context
            => View.GraphicsContext;

        internal static AndroidGameView View;

        private bool activityInForeground = true;
        public bool ActivityInForeground
        {
            get => activityInForeground;
            set {
                activityInForeground = value;
                OnFocusedChanged(this, EventArgs.Empty);
            }
        }

        public override bool Focused
            => ActivityInForeground;

        public override osuTK.WindowState WindowState {
            get => osuTK.WindowState.Normal;
            set { }
        }

        public AndroidGameWindow() : base(View)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            // Let's just say the cursor is always in the window.
            CursorInWindow = true;
            OnFocusedChanged(this, EventArgs.Empty); // Ensures that the isActive is set correctly on initial setup
        }

        protected override IEnumerable<WindowMode> DefaultSupportedWindowModes => new WindowMode[]
        {
            Configuration.WindowMode.Fullscreen,
        };

        public override void Run()
        {
        }

        public override void Run(double updateRate)
        {
        }
    }
}
