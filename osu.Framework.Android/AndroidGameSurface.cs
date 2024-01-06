// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Views;
using Org.Libsdl.App;

namespace osu.Framework.Android
{
    internal class AndroidGameSurface : SDLSurface
    {
        public AndroidGameSurface(Context context) : base(context)
        {
        }

        public override void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {
            base.SurfaceChanged(holder, format, width, height);

            if (OperatingSystem.IsAndroidVersionAtLeast(26) && MIsSurfaceReady)
            {
                // disable ugly green border when view is focused via hardware keyboard/mouse.
                DefaultFocusHighlightEnabled = false;
            }
        }
    }
}
