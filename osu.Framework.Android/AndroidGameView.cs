// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameView : osuTK.Android.AndroidGameView
    {
        private AndroidGameHost host;
        private Game game;

        public AndroidGameView(Context context, Game game) : base(context)
        {
            this.game = game;
            init();
        }

        public AndroidGameView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            init();
        }

        public AndroidGameView(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            init();
        }

        private void init()
        {
            AutoSetContextOnRenderFrame = true;
            ContextRenderingApi = GLVersion.ES3;
        }

        protected override void CreateFrameBuffer()
        {
            try
            {
                base.CreateFrameBuffer();
                Log.Verbose("AndroidGameView", "Successfully created the framebuffer");
            }
            catch (Exception e)
            {
                Log.Verbose("AndroidGameView", "{0}", e);
                throw new Exception("Can't load egl, aborting", e);
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            RenderGame();
        }

        [STAThread]
        public void RenderGame()
        {
            host = new AndroidGameHost(this);
            host.Run(game);
        }
    }
}
