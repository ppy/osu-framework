// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics;
using System.Drawing;
using ObjCRuntime;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using UIKit;

namespace osu.Framework.iOS
{
    public class IOSWindow : SDL2Window
    {
        private UIWindow? window;

        public override Size Size
        {
            get => base.Size;
            protected set
            {
                base.Size = value;

                if (window != null)
                    updateSafeArea();
            }
        }

        public IOSWindow(GraphicsSurfaceType surfaceType)
            : base(surfaceType)
        {
        }

        public override void Create()
        {
            base.Create();

            window = Runtime.GetNSObject<UIWindow>(WindowHandle);
            updateSafeArea();
        }

        private void updateSafeArea()
        {
            Debug.Assert(window != null);

            SafeAreaPadding.Value = new MarginPadding
            {
                Top = (float)window.SafeAreaInsets.Top * Scale,
                Left = (float)window.SafeAreaInsets.Left * Scale,
                Bottom = (float)window.SafeAreaInsets.Bottom * Scale,
                Right = (float)window.SafeAreaInsets.Right * Scale,
            };
        }
    }
}
