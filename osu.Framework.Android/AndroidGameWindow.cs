// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.StateChanges;
using osu.Framework.Platform;
using osu.Framework.Platform.SDL3;
using SDL;

namespace osu.Framework.Android
{
    internal class AndroidGameWindow : SDL3MobileWindow
    {
        public override IntPtr SurfaceHandle => AndroidGameActivity.Surface.NativeSurface?.Handle ?? IntPtr.Zero;

        public AndroidGameWindow(GraphicsSurfaceType surfaceType, string appName)
            : base(surfaceType, appName)
        {
        }

        protected override TabletPenDeviceType GetPenDeviceType(SDL_PenID id) => AndroidGameActivity.Surface.LastPenDeviceType;

        public override void Create()
        {
            base.Create();

            SafeAreaPadding.BindTo(AndroidGameActivity.Surface.SafeAreaPadding);
        }
    }
}
