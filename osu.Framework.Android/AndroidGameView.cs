// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Text;
using osuTK.Graphics;
using osu.Framework.Android.Input;

namespace osu.Framework.Android
{
    public class AndroidGameView : osuTK.Android.AndroidGameView
    {
        public AndroidGameHost Host { get; private set; }

        private readonly Game game;

        public new event Action<Keycode, KeyEvent> KeyDown;
        public new event Action<Keycode, KeyEvent> KeyUp;
        public event Action<Keycode, KeyEvent> KeyLongPress;
        public event Action<string> CommitText;

        public AndroidGameView(Context context, Game game)
            : base(context)
        {
            this.game = game;

            init();
        }

        public AndroidGameView(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {
            init();
        }

        public AndroidGameView(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
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

        public bool OnCommitText(string text)
        {
            CommitText?.Invoke(text);
            return false;
        }

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            switch (keyCode)
            {
                // Do not consume Volume keys, so the system can handle them
                case Keycode.VolumeDown:
                case Keycode.VolumeUp:
                case Keycode.VolumeMute:
                    return false;

                default:
                    KeyDown?.Invoke(keyCode, e);
                    return true;
            }
        }

        public override bool OnKeyLongPress([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            KeyLongPress?.Invoke(keyCode, e);
            return true;
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            KeyUp?.Invoke(keyCode, e);
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
            Host = new AndroidGameHost(this);
            Host.Run(game);
        }

        public override bool OnCheckIsTextEditor() => true;

        public override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
        {
            outAttrs.ImeOptions = ImeFlags.NoExtractUi;
            outAttrs.InputType = InputTypes.Null;
            return new AndroidInputConnection(this, true);
        }
    }
}
