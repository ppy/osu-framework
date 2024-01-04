// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics;
using System;
using osu.Framework.Platform;
using osuTK.Android;
using Android.Graphics;
using SDL2;
using Android.Views;

namespace osu.Framework.Android
{
    internal class AndroidGameWindow : SDL2Window
    {
        private readonly AndroidGameActivity activity;

        private View rootView => AndroidGameActivity.Surface.RootView!;

        public override IntPtr DisplayHandle => Org.Libsdl.App.SDLActivity.NativeSurface?.Handle ?? IntPtr.Zero;

        public AndroidGameWindow(GraphicsSurfaceType surfaceType, AndroidGameActivity activity)
            : base(surfaceType)
        {
            this.activity = activity;
        }

        protected override void UpdateWindowStateAndSize(WindowState state, Platform.Display display, DisplayMode displayMode)
        {
            // This sets the status bar to hidden.
            SDL.SDL_SetWindowFullscreen(SDLWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);

            // Don't run base logic at all. Let's keep things simple.
        }

        public override void Create()
        {
            base.Create();

            rootView.LayoutChange += (_, _) => updateSafeArea();

            activity.IsActive.BindValueChanged(active =>
            {
                // When rotating device 180 degrees in the background,
                // LayoutChange doesn't trigger after returning to game.
                // So update safe area once active again.
                if (active.NewValue)
                    updateSafeArea();
            });

            updateSafeArea();

            // Same as cursorInWindow, Android SDL doesn't push these events at start, so it never receives focus until it comes back from background
            Focused = true;
        }

        /// <summary>
        /// Updates the <see cref="IWindow.SafeAreaPadding"/>, taking into account screen insets that may be obstructing this <see cref="AndroidGameView"/>.
        /// </summary>
        private void updateSafeArea()
        {
            // compute the usable screen area.

            var screenSize = new Point();
#pragma warning disable 618 // GetRealSize is deprecated
            rootView.Display!.GetRealSize(screenSize);
#pragma warning restore 618
            var screenArea = new RectangleI(0, 0, screenSize.X, screenSize.Y);
            var usableScreenArea = screenArea;

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                var cutout = rootView.RootWindowInsets?.DisplayCutout;

                if (cutout != null)
                    usableScreenArea = usableScreenArea.Shrink(cutout.SafeInsetLeft, cutout.SafeInsetRight, cutout.SafeInsetTop, cutout.SafeInsetBottom);
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(24) && activity.IsInMultiWindowMode)
            {
                // if we are in multi-window mode, the status bar is always visible (even if we request to hide it) and could be obstructing our view.
                // if multi-window mode is not active, we can assume the status bar is hidden so we shouldn't consider it for safe area calculations.

                // `SystemWindowInsetTop` should be the correct inset here, but it doesn't correctly work (gives `0` even if the view is obstructed).
#pragma warning disable 618 // StableInsetTop is deprecated
                int statusBarHeight = rootView.RootWindowInsets?.StableInsetTop ?? 0;
#pragma warning restore 618 //
                usableScreenArea = usableScreenArea.Intersect(screenArea.Shrink(0, 0, statusBarHeight, 0));
            }

            // TODO: add rounded corners support (Android 12): https://developer.android.com/guide/topics/ui/look-and-feel/rounded-corners

            // compute the location/area of this view on the screen.

            int[] location = new int[2];
            rootView.GetLocationOnScreen(location);
            var viewArea = new RectangleI(location[0], location[1], rootView.Width, rootView.Height);

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
    }
}
