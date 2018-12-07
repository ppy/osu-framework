// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using osu.Framework;
using osu.Framework.Graphics.OpenGL;
using osuTK;
using osuTK.Graphics;
using osuTK.Graphics.ES30;
using osuTK.Platform.Android;

namespace osu.Framework.Android
{
    public abstract class AndroidGameView : osuTK.Android.AndroidGameView
    {
        int viewportWidth, viewportHeight;
        int program;

        private AndroidGameHost host;

        public AndroidGameView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init();
        }
        public AndroidGameView(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            Init();
        }
        void Init()
        {
            AutoSetContextOnRenderFrame = true;
            ContextRenderingApi = GLVersion.ES3;
            RenderThreadRestartRetries = 1;
        }
        protected override void CreateFrameBuffer()
        {
            try
            {
                //GraphicsMode = new GraphicsMode();
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
            base.OnResize(e);

            viewportHeight = Height;
            viewportWidth = Width;

            MakeCurrent();
        }

        void RenderGame()
        {
            Run();
            host = new AndroidGameHost(this);
            host.Run(CreateGame());

            SwapBuffers();
        }
        public abstract Game CreateGame();
    }
}
