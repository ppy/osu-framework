// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Platform;

namespace osu.Framework.Android
{
    internal class AndroidGameWindow : SDL3MobileWindow
    {
        public override IntPtr DisplayHandle => AndroidGameActivity.Surface.NativeSurface?.Handle ?? IntPtr.Zero;

        public AndroidGameWindow(GraphicsSurfaceType surfaceType, string appName)
            : base(surfaceType, appName)
        {
        }

        public override void Create()
        {
            base.Create();

            SafeAreaPadding.BindTo(AndroidGameActivity.Surface.SafeAreaPadding);

            // Android SDL doesn't receive these events at start, so it never receives focus until it comes back from background
            ((BindableBool)CursorInWindow).Value = true;
            Focused = true;
        }
    }
}
