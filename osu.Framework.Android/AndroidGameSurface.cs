// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.Content;
using Android.Runtime;
using Org.Libsdl.App;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Bindables;
using Android.Views;
using AndroidX.Core.View;
using AndroidX.Window.Layout;

namespace osu.Framework.Android
{
    internal class AndroidGameSurface : SDLSurface
    {
        private AndroidGameActivity activity { get; } = null!;

        public BindableSafeArea SafeAreaPadding { get; } = new BindableSafeArea();

        public AndroidGameSurface(AndroidGameActivity activity, Context? context)
            : base(context)
        {
            init();
            this.activity = activity;
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

        private volatile bool isSurfaceReady;

        public bool IsSurfaceReady => isSurfaceReady;

        protected override void HandlePause()
        {
            base.HandlePause();
            isSurfaceReady = false;
        }

        protected override void HandleResume()
        {
            base.HandleResume();
            isSurfaceReady = true;
        }

        public override WindowInsets? OnApplyWindowInsets(View? view, WindowInsets? insets)
        {
            updateSafeArea(insets);
            return base.OnApplyWindowInsets(view, insets);
        }

        /// <summary>
        /// Updates the <see cref="IWindow.SafeAreaPadding"/>, taking into account screen insets that may be obstructing this <see cref="AndroidGameSurface"/>.
        /// </summary>
        private void updateSafeArea(WindowInsets? windowInsets)
        {
            var metrics = WindowMetricsCalculator.Companion.OrCreate.ComputeCurrentWindowMetrics(activity);
            var windowArea = metrics.Bounds.ToRectangleI();
            var usableWindowArea = windowArea;

            if (OperatingSystem.IsAndroidVersionAtLeast(28))
            {
                var cutout = windowInsets?.DisplayCutout;

                if (cutout != null)
                    usableWindowArea = usableWindowArea.Shrink(cutout.SafeInsetLeft, cutout.SafeInsetRight, cutout.SafeInsetTop, cutout.SafeInsetBottom);
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(31) && windowInsets != null)
            {
                var topLeftCorner = windowInsets.GetRoundedCorner((int)RoundedCornerPosition.TopLeft);
                var topRightCorner = windowInsets.GetRoundedCorner((int)RoundedCornerPosition.TopRight);
                var bottomLeftCorner = windowInsets.GetRoundedCorner((int)RoundedCornerPosition.BottomLeft);
                var bottomRightCorner = windowInsets.GetRoundedCorner((int)RoundedCornerPosition.BottomRight);

                int cornerInsetLeft = Math.Max(topLeftCorner?.Radius ?? 0, bottomLeftCorner?.Radius ?? 0);
                int cornerInsetRight = Math.Max(topRightCorner?.Radius ?? 0, bottomRightCorner?.Radius ?? 0);
                int cornerInsetTop = Math.Max(topLeftCorner?.Radius ?? 0, topRightCorner?.Radius ?? 0);
                int cornerInsetBottom = Math.Max(bottomLeftCorner?.Radius ?? 0, bottomRightCorner?.Radius ?? 0);

                var radiusInsetArea = windowArea.Width >= windowArea.Height
                    ? windowArea.Shrink(cornerInsetLeft, cornerInsetRight, 0, 0)
                    : windowArea.Shrink(0, 0, cornerInsetTop, cornerInsetBottom);

                usableWindowArea = usableWindowArea.Intersect(radiusInsetArea);
            }

            if (OperatingSystem.IsAndroidVersionAtLeast(24) && activity.IsInMultiWindowMode && windowInsets != null)
            {
                // if we are in multi-window mode, the status bar is always visible (even if we request to hide it) and could be obstructing our view.
                // if multi-window mode is not active, we can assume the status bar is hidden so we shouldn't consider it for safe area calculations.
                var insetsCompat = WindowInsetsCompat.ToWindowInsetsCompat(windowInsets, this);
                int statusBarHeight = insetsCompat.GetInsets(WindowInsetsCompat.Type.StatusBars()).Top;
                usableWindowArea = usableWindowArea.Intersect(windowArea.Shrink(0, 0, statusBarHeight, 0));
            }

            SafeAreaPadding.Value = new MarginPadding
            {
                Left = usableWindowArea.Left - windowArea.Left,
                Top = usableWindowArea.Top - windowArea.Top,
                Right = windowArea.Right - usableWindowArea.Right,
                Bottom = windowArea.Bottom - usableWindowArea.Bottom,
            };
        }
    }
}
