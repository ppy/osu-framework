// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public abstract class AndroidGameView : osuTK.Android.AndroidGameView
    {
        private int viewportWidth, viewportHeight;
        private AndroidGameHost host;

        public abstract Game CreateGame();

        public AndroidGameView(Context context) : base(context)
        {
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
                Log.Verbose("AndroidGameView", "Successfully loaded");
                return;
            }
            catch (Exception e)
            {
                Log.Verbose("AndroidGameView", "{0}", e);
            }
            throw new Exception("Can't load egl, aborting");
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            viewportHeight = Height;
            viewportWidth = Width;

            RenderGame();
        }

        protected override void OnResize(EventArgs e)
        {
            viewportHeight = Height;
            viewportWidth = Width;
        }

        [STAThread]
        public void RenderGame()
        {
            host = new AndroidGameHost(this);
            host.Run(CreateGame());
        }
    }
}
