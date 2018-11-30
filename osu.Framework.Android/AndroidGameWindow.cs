// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;
using osu.Framework.Configuration;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameWindow : GameWindow
    {
        private readonly AndroidGameView view;

        protected new osuTK.GameWindow Implementation => (osuTK.GameWindow)base.Implementation;

        public AndroidGameWindow(AndroidGameView view) : base(new AndroidPlatformWindow(view))
        {
            this.view = view;

            Load += OnLoad;
        }

        public override void SetupWindow(FrameworkConfigManager config)
        {
            //throw new NotImplementedException();
        }

        public override IGraphicsContext Context => view.GraphicsContext;

        public override bool Focused => true;

        public override osuTK.WindowState WindowState { get => osuTK.WindowState.Normal; set { } }

        protected void OnLoad(object sender, EventArgs e)
        {
            var implementationField = typeof(osuTK.NativeWindow).GetRuntimeFields().Single(x => x.Name == "implementation");

            var windowImpl = implementationField.GetValue(Implementation);

            //isSdl = windowImpl.GetType().Name == "Sdl2NativeWindow";
        }
    }
}
