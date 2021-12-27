// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.Content;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using osu.Framework.Android.Input;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameView : osuTK.Android.AndroidGameView
    {
        public AndroidGameHost Host { get; private set; }

        private readonly Game game;

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
                throw new InvalidOperationException("Can't load egl, aborting", e);
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
            }

            // some implementations might send Mouse1 and Mouse2 as keyboard keycodes, so forward those only to the mouse event.
            switch (e.Source)
            {
                case InputSourceType.Keyboard:
                    KeyDown?.Invoke(keyCode, e);
                    return true;

                case InputSourceType.Mouse:
                case InputSourceType.Touchpad:
                    MouseKeyDown?.Invoke(keyCode, e);
                    return true;
            }

            return base.OnKeyDown(keyCode, e);
        }

        public override bool OnKeyLongPress([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            switch (e.Source)
            {
                case InputSourceType.Keyboard:
                    KeyLongPress?.Invoke(keyCode, e);
                    return true;

                case InputSourceType.Mouse:
                case InputSourceType.Touchpad:
                    MouseKeyLongPress?.Invoke(keyCode, e);
                    return true;
            }

            return base.OnKeyLongPress(keyCode, e);
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent e)
        {
            switch (e.Source)
            {
                case InputSourceType.Keyboard:
                    KeyUp?.Invoke(keyCode, e);
                    return true;

                case InputSourceType.Mouse:
                case InputSourceType.Touchpad:
                    MouseKeyUp?.Invoke(keyCode, e);
                    return true;
            }

            return base.OnKeyUp(keyCode, e);
        }

        public override bool OnHoverEvent(MotionEvent e)
        {
            switch (e.Source)
            {
                case InputSourceType.BluetoothStylus:
                case InputSourceType.Stylus:
                case InputSourceType.Touchscreen:
                    Hover?.Invoke(e);
                    return true;

                case InputSourceType.Mouse:
                case InputSourceType.Touchpad:
                    MouseHover?.Invoke(e);
                    return true;
            }

            return base.OnHoverEvent(e);
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            switch (e.Source)
            {
                case InputSourceType.BluetoothStylus:
                case InputSourceType.Stylus:
                case InputSourceType.Touchscreen:
                    Touch?.Invoke(e);
                    return true;

                case InputSourceType.Mouse:
                case InputSourceType.Touchpad:
                    MouseTouch?.Invoke(e);
                    return true;
            }

            return base.OnTouchEvent(e);
        }

        public override bool OnGenericMotionEvent(MotionEvent e)
        {
            switch (e.Source)
            {
                case InputSourceType.Mouse:
                case InputSourceType.Touchpad:
                    MouseGenericMotion?.Invoke(e);
                    return true;
            }

            return base.OnGenericMotionEvent(e);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // osuTK calls `OnLoad()` every time the application surface is created, which will also happen upon a resume,
            // at which point the host is already present and running, so there is no reason to create another one.
            if (Host == null)
                RenderGame();
        }

        [STAThread]
        public void RenderGame()
        {
            Host = new AndroidGameHost(this);
            Host.ExceptionThrown += handleException;
            Host.Run(game);
            HostStarted?.Invoke(Host);
        }

        private bool handleException(Exception ex)
        {
            // suppress exceptions related to MobileAuthenticatedStream disposal
            // (see: https://github.com/ppy/osu/issues/6264 and linked related mono/xamarin issues)
            // to be removed when upstream fixes come in
            return ex is AggregateException ae
                   && ae.InnerException is ObjectDisposedException ode
                   && ode.ObjectName == "MobileAuthenticatedStream";
        }

        public override bool OnCheckIsTextEditor() => true;

        public override IInputConnection OnCreateInputConnection(EditorInfo outAttrs)
        {
            outAttrs.ImeOptions = ImeFlags.NoExtractUi | ImeFlags.NoFullscreen;
            outAttrs.InputType = InputTypes.TextVariationVisiblePassword | InputTypes.TextFlagNoSuggestions;
            return new AndroidInputConnection(this, true);
        }

        #region Events

        /// <summary>
        /// Invoked on a key down event sourced from a <see cref="InputSourceType.Keyboard"/>.
        /// </summary>
        public new event Action<Keycode, KeyEvent> KeyDown;

        /// <summary>
        /// Invoked on a key up event sourced from a <see cref="InputSourceType.Keyboard"/>.
        /// </summary>
        public new event Action<Keycode, KeyEvent> KeyUp;

        /// <summary>
        /// Invoked on a key long press event sourced from a <see cref="InputSourceType.Keyboard"/>.
        /// </summary>
        public event Action<Keycode, KeyEvent> KeyLongPress;

        /// <summary>
        /// Invoked on a hover event sourced from a touch-type device.
        /// </summary>
        /// <remarks>
        /// Invoked if the source is <see cref="InputSourceType.BluetoothStylus"/>, <see cref="InputSourceType.Stylus"/>
        /// or <see cref="InputSourceType.Touchscreen"/>.
        /// </remarks>
        public new event Action<MotionEvent> Hover;

        /// <summary>
        /// Invoked on a touch event sourced from a touch-type device.
        /// </summary>
        /// <remarks>
        /// Invoked if the source is <see cref="InputSourceType.BluetoothStylus"/>, <see cref="InputSourceType.Stylus"/>
        /// or <see cref="InputSourceType.Touchscreen"/>.
        /// </remarks>
        public new event Action<MotionEvent> Touch;

        /// <summary>
        /// Invoked on a key down event sourced from a mouse-type device.
        /// </summary>
        /// <remarks>Invoked if the source is <see cref="InputSourceType.Mouse"/> or <see cref="InputSourceType.Touchpad"/>.</remarks>
        public event Action<Keycode, KeyEvent> MouseKeyDown;

        /// <summary>
        /// Invoked on a key up event sourced from a mouse-type device.
        /// </summary>
        /// <remarks>Invoked if the source is <see cref="InputSourceType.Mouse"/> or <see cref="InputSourceType.Touchpad"/>.</remarks>
        public event Action<Keycode, KeyEvent> MouseKeyUp;

        /// <summary>
        /// Invoked on a key long press event sourced from a mouse-type device.
        /// </summary>
        /// <remarks>Invoked if the source is <see cref="InputSourceType.Mouse"/> or <see cref="InputSourceType.Touchpad"/>.</remarks>
        public event Action<Keycode, KeyEvent> MouseKeyLongPress;

        /// <summary>
        /// Invoked on a hover event sourced from a mouse-type device.
        /// </summary>
        /// <remarks>
        /// Similar to <see cref="MouseTouch"/> but invoked when no buttons are pressed.
        /// Invoked if the source is <see cref="InputSourceType.Mouse"/> or <see cref="InputSourceType.Touchpad"/>.
        /// </remarks>
        public event Action<MotionEvent> MouseHover;

        /// <summary>
        /// Invoked on a touch sourced from a mouse-type device.
        /// </summary>
        /// <remarks>
        /// Similar to <see cref="MouseHover"/> but invoked when one or more buttons are pressed.
        /// Invoked if the source is <see cref="InputSourceType.Mouse"/> or <see cref="InputSourceType.Touchpad"/>.
        /// </remarks>
        public event Action<MotionEvent> MouseTouch;

        /// <summary>
        /// Invoked on a generic motion sourced from a mouse-type device.
        /// </summary>
        /// <remarks>Invoked if the source is <see cref="InputSourceType.Mouse"/> or <see cref="InputSourceType.Touchpad"/>.</remarks>
        public event Action<MotionEvent> MouseGenericMotion;

        /// <summary>
        /// Invoked when text is committed by an <see cref="AndroidInputConnection"/>.
        /// </summary>
        public event Action<string> CommitText;

        /// <summary>
        /// Invoked when the <see cref="game"/> has been started on the <see cref="Host"/>.
        /// </summary>
        public event Action<AndroidGameHost> HostStarted;

        #endregion
    }
}
