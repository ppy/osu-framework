// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameWindow : GameWindow
    {
        internal static AndroidGameView view;

        public override IGraphicsContext Context => view.GraphicsContext;

        public override bool Focused
            => true;

        public override osuTK.WindowState WindowState {
            get => osuTK.WindowState.Normal;
            set { }
        }

        public AndroidGameWindow() : base(view)
        {
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
        }

        public override void Run()
        {
        }

        public override void Run(double updateRate)
        {
        }
    }
}
