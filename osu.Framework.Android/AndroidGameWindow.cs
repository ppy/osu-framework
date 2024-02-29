// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;

namespace osu.Framework.Android
{
    internal class AndroidGameWindow : SDL2Window
    {
        public override IntPtr DisplayHandle => AndroidGameActivity.Surface.NativeSurface?.Handle ?? IntPtr.Zero;

        public AndroidGameWindow(GraphicsSurfaceType surfaceType)
            : base(surfaceType)
        {
        }

        public override void Create()
        {
            base.Create();

            SafeAreaPadding.BindTo(AndroidGameActivity.Surface.SafeAreaPadding);

            // Same as cursorInWindow, Android SDL doesn't push these events at start, so it never receives focus until it comes back from background
            Focused = true;
        }
    }
}
