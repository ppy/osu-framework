// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Text;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using osu.Framework.Android.Input;
using osu.Framework.Logging;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Platform;
using osuTK.Graphics;

namespace osu.Framework.Android
{
    public class AndroidGameView : osuTK.Android.AndroidGameView
    {
        public AndroidGameHost? Host { get; private set; }

        public AndroidGameActivity Activity { get; } = null!;

        public BindableSafeArea SafeAreaPadding { get; } = new BindableSafeArea();

        /// <summary>
        /// Represents whether the mouse pointer is captured, as reported by Android through <see cref="OnPointerCaptureChange"/>.
        /// </summary>
        private bool pointerCaptured;

        /// <summary>
        /// Set Android's pointer capture.
        /// </summary>
        /// <remarks>
        /// Only available in Android 8.0 Oreo (<see cref="BuildVersionCodes.O"/>) and up.
        /// </remarks>
        public bool PointerCapture
        {
            get => pointerCaptured;
            set
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.O)
                {
                    Logger.Log($"Tried to set {nameof(PointerCapture)} on an unsupported Android version.", level: LogLevel.Important);
                    return;
                }

                if (pointerCaptured == value) return;

                if (value)
                    RequestPointerCapture();
                else
                    ReleasePointerCapture();
            }
        }

        private readonly Game game = null!;

        private InputMethodManager? inputMethodManager;

        /// <summary>
        /// Whether <see cref="AndroidTextInput"/> is active.
        /// </summary>
        private bool textInputActive;

        public AndroidGameView(AndroidGameActivity activity, Game game)
            : base(activity)
        {
            Activity = activity;
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

            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                // disable ugly green border when view is focused via hardware keyboard/mouse.
                DefaultFocusHighlightEnabled = false;
            }

            inputMethodManager = Activity.GetSystemService(Context.InputMethodService) as InputMethodManager;
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

        public override bool OnKeyDown([GeneratedEnum] Keycode keyCode, KeyEvent? e)
        {
            if (e == null) return base.OnKeyDown(keyCode, e);

            switch (keyCode)
            {
                // Do not consume Volume keys, so the system can handle them
                case Keycode.VolumeDown:
                case Keycode.VolumeUp:
                case Keycode.VolumeMute:
                    return false;

                default:
                    KeyDown?.Invoke(keyCode, e);

                    // Releasing backspace on a physical keyboard when text input is active will not send a key up event.
                    // Manually send one to prevent the key from getting stuck.
                    // This does mean that key repeat is handled by the OS, instead of by the usual `InputManager` handling.
                    if (keyCode == Keycode.Del && e.IsFromSource(InputSourceType.Keyboard) && textInputActive)
                        KeyUp?.Invoke(Keycode.Del, new KeyEvent(e.DownTime, e.EventTime, KeyEventActions.Up, Keycode.Del, 0, e.MetaState, e.DeviceId, e.ScanCode, e.Flags, e.Source));

                    return true;
            }
        }

        public override bool OnKeyLongPress([GeneratedEnum] Keycode keyCode, KeyEvent? e)
        {
            if (e == null) return base.OnKeyLongPress(keyCode, e);

            KeyLongPress?.Invoke(keyCode, e);
            return true;
        }

        public override bool OnKeyUp([GeneratedEnum] Keycode keyCode, KeyEvent? e)
        {
            if (e == null) return base.OnKeyUp(keyCode, e);

            KeyUp?.Invoke(keyCode, e);
            return true;
        }

        public override void OnPointerCaptureChange(bool hasCapture)
        {
            base.OnPointerCaptureChange(hasCapture);
            pointerCaptured = hasCapture;
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
            // request focus so that joystick input can immediately work.
            RequestFocus();

            LayoutChange += (_, _) => updateSafeArea();
            updateSafeArea();

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

        /// <summary>
        /// Updates the <see cref="IWindow.SafeAreaPadding"/>, taking into account screen insets that may be obstructing this <see cref="AndroidGameView"/>.
        /// </summary>
        private void updateSafeArea()
        {
            // compute the usable screen area.

            var screenSize = new Point();
            Display.AsNonNull().GetRealSize(screenSize);
            var screenArea = new RectangleI(0, 0, screenSize.X, screenSize.Y);
            var usableScreenArea = screenArea;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
            {
                var cutout = RootWindowInsets?.DisplayCutout;

                if (cutout != null)
                    usableScreenArea = usableScreenArea.Shrink(cutout.SafeInsetLeft, cutout.SafeInsetRight, cutout.SafeInsetTop, cutout.SafeInsetBottom);
            }

            if (Build.VERSION.SdkInt >= BuildVersionCodes.N && Activity.IsInMultiWindowMode)
            {
                // if we are in multi-window mode, the status bar is always visible (even if we request to hide it) and could be obstructing our view.
                // if multi-window mode is not active, we can assume the status bar is hidden so we shouldn't consider it for safe area calculations.

                // `SystemWindowInsetTop` should be the correct inset here, but it doesn't correctly work (gives `0` even if the view is obstructed).
                int statusBarHeight = RootWindowInsets?.StableInsetTop ?? 0;
                usableScreenArea = usableScreenArea.Intersect(screenArea.Shrink(0, 0, statusBarHeight, 0));
            }

            // TODO: add rounded corners support (Android 12): https://developer.android.com/guide/topics/ui/look-and-feel/rounded-corners

            // compute the location/area of this view on the screen.

            int[] location = new int[2];
            GetLocationOnScreen(location);
            var viewArea = new RectangleI(location[0], location[1], ((View)this).Width, ((View)this).Height);

            // intersect with the usable area and treat the the difference as unsafe.

            var usableViewArea = viewArea.Intersect(usableScreenArea);

            SafeAreaPadding.Value = new MarginPadding
            {
                Left = usableViewArea.Left - viewArea.Left,
                Top = usableViewArea.Top - viewArea.Top,
                Right = viewArea.Right - usableViewArea.Right,
                Bottom = viewArea.Bottom - usableViewArea.Bottom,
            };
        }

        public override bool OnCheckIsTextEditor() => textInputActive;

        /// <returns><c>null</c> to disable input methods</returns>
        public override IInputConnection? OnCreateInputConnection(EditorInfo? outAttrs)
        {
            if (outAttrs == null) throw new ArgumentNullException(nameof(outAttrs));

            // Properly disable native input methods so that the software keyboard doesn't unexpectedly open.
            // Eg. when pressing keys on a hardware keyboard.
            if (!textInputActive)
                return null;

            outAttrs.ImeOptions = ImeFlags.NoExtractUi | ImeFlags.NoFullscreen;
            outAttrs.InputType = InputTypes.TextVariationVisiblePassword | InputTypes.TextFlagNoSuggestions;
            return new AndroidInputConnection(this, true);
        }

        internal void StartTextInput()
        {
            textInputActive = true;
            Activity.RunOnUiThread(() =>
            {
                inputMethodManager?.RestartInput(this); // this syncs the Android input method state with `OnCreateInputConnection()`.
                RequestFocus();
                inputMethodManager?.ShowSoftInput(this, 0);
            });
        }

        internal void StopTextInput()
        {
            textInputActive = false;
            Activity.RunOnUiThread(() =>
            {
                inputMethodManager?.RestartInput(this);
                inputMethodManager?.HideSoftInputFromWindow(WindowToken, HideSoftInputFlags.None);
            });
        }

        public override void SwapBuffers()
        {
            try
            {
                base.SwapBuffers();
            }
            catch (GraphicsContextException ex)
            {
                // sometimes buffers will spontaneously fail to swap with BAD_SURFACE
                // just before the activity is suspended to background or just after it has been resumed,
                // but will continue operating correctly after that transitionary period.
                // despite some testing it is unclear which view callback can be used to tell whether it is safe to swap buffers,
                // so for now just catch and suppress these errors.
                if (ex.Message.Contains("BAD_SURFACE", StringComparison.Ordinal))
                    Logger.Log($"BAD_SURFACE failure in {nameof(SwapBuffers)} suppressed");
                else
                    throw;
            }
        }

        #region Events

        /// <summary>
        /// Invoked on a key down event.
        /// </summary>
        public new event Action<Keycode, KeyEvent>? KeyDown;

        /// <summary>
        /// Invoked on a key up event.
        /// </summary>
        public new event Action<Keycode, KeyEvent>? KeyUp;

        /// <summary>
        /// Invoked on a key long press event.
        /// </summary>
        public event Action<Keycode, KeyEvent>? KeyLongPress;

        /// <summary>
        /// Invoked when text is committed by an <see cref="AndroidInputConnection"/>.
        /// </summary>
        public event Action<string>? CommitText;

        /// <summary>
        /// Invoked when the <see cref="game"/> has been started on the <see cref="Host"/>.
        /// </summary>
        public event Action<AndroidGameHost>? HostStarted;

        #endregion
    }
}
