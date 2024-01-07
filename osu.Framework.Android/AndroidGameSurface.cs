// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Org.Libsdl.App;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osuTK.Android;
using osu.Framework.Bindables;
using Android.Views;

namespace osu.Framework.Android
{
    internal class AndroidGameSurface : SDLSurface
    {
        public AndroidGameActivity Activity { get; } = null!;

        public BindableSafeArea SafeAreaPadding { get; } = new BindableSafeArea();

        public AndroidGameSurface(AndroidGameActivity activity, Context context)
            : base(context)
        {
            init();
            Activity = activity;
        }

        protected AndroidGameSurface(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
            init();
        }

        private void init()
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26))
            {
                // disable ugly green border when view is focused via hardware keyboard/mouse.
                DefaultFocusHighlightEnabled = false;
            }
        }

        private bool registered;

        public override void SurfaceCreated(ISurfaceHolder p0)
        {
            base.SurfaceCreated(p0);

            if (!registered)
            {
                registered = true;

                RootView!.LayoutChange += (_, _) => UpdateSafeArea();

                Activity.IsActive.BindValueChanged(active =>
                {
                    // When rotating device 180 degrees in the background,
                    // LayoutChange doesn't trigger after returning to game.
                    // So update safe area once active again.
                    if (active.NewValue)
                        UpdateSafeArea();
                });

                UpdateSafeArea();
            }
        }

        /// <summary>
        /// Updates the <see cref="IWindow.SafeAreaPadding"/>, taking into account screen insets that may be obstructing this <see cref="AndroidGameView"/>.
        /// </summary>
        public void UpdateSafeArea()
        {
            if (RootView == null)
                return;

            // compute the usable screen area.

            var screenSize = new Point();
#pragma warning disable 618 // GetRealSize is deprecated
            RootView.Display!.GetRealSize(screenSize);
#pragma warning restore 618
            var screenArea = new RectangleI(0, 0, screenSize.X, screenSize.Y);
            var usableScreenArea = screenArea;

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                var cutout = RootView.RootWindowInsets?.DisplayCutout;

                if (cutout != null)
                    usableScreenArea = usableScreenArea.Shrink(cutout.SafeInsetLeft, cutout.SafeInsetRight, cutout.SafeInsetTop, cutout.SafeInsetBottom);
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(24) && Activity.IsInMultiWindowMode)
            {
                // if we are in multi-window mode, the status bar is always visible (even if we request to hide it) and could be obstructing our view.
                // if multi-window mode is not active, we can assume the status bar is hidden so we shouldn't consider it for safe area calculations.

                // `SystemWindowInsetTop` should be the correct inset here, but it doesn't correctly work (gives `0` even if the view is obstructed).
#pragma warning disable 618 // StableInsetTop is deprecated
                int statusBarHeight = RootView.RootWindowInsets?.StableInsetTop ?? 0;
#pragma warning restore 618 //
                usableScreenArea = usableScreenArea.Intersect(screenArea.Shrink(0, 0, statusBarHeight, 0));
            }

            // TODO: add rounded corners support (Android 12): https://developer.android.com/guide/topics/ui/look-and-feel/rounded-corners

            // compute the location/area of this view on the screen.

            int[] location = new int[2];
            RootView.GetLocationOnScreen(location);
            var viewArea = new RectangleI(location[0], location[1], RootView.Width, RootView.Height);

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
