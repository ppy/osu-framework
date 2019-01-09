// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameWindow : GameWindow
    {
        public override IGraphicsContext Context
            => View.GraphicsContext;

        internal static AndroidGameView View;

        public override bool Focused
            => true;

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
        }

        public override void Run()
        {
        }

        public override void Run(double updateRate)
        {
        }
    }
}
