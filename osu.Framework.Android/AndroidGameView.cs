// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameView : osuTK.Android.AndroidGameView
    {
        private AndroidGameHost host;
        private readonly Game game;

        public event Action<Keycode> KeyDown;
        public event Action<Keycode> KeyUp;
        public event Action<Keycode> KeyLongPress;

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

            // enable soft and hardware keyboard
            // this needs to happen in the constructor
            Focusable = true;
            FocusableInTouchMode = true;
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

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            KeyDown?.Invoke(keyCode);
            return true;
        }

        public override bool OnKeyLongPress([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            KeyLongPress?.Invoke(keyCode);
            return true;
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            KeyUp?.Invoke(keyCode);
            return true;
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
