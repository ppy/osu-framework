// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ObjCRuntime;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Platform.SDL3;
using static SDL.SDL3;
using UIKit;

namespace osu.Framework.iOS
{
    internal class IOSWindow : SDL3MobileWindow
    {
        private UIWindow? uiWindow;

        public UIWindow UIWindow => uiWindow!;

        public override Size Size
        {
            get => base.Size;
            protected set
            {
                base.Size = value;

                if (uiWindow != null)
                    updateSafeArea();
            }
        }

        public IOSWindow(GraphicsSurfaceType surfaceType, string appName)
            : base(surfaceType, appName)
        {
        }

        public override void Create()
        {
            SDL_SetHint(SDL_HINT_IOS_HIDE_HOME_INDICATOR, "2"u8);

            base.Create();

            uiWindow = Runtime.GetNSObject<UIWindow>(WindowHandle)!;
            updateSafeArea();
        }

        protected override unsafe void RunMainLoop()
        {
            // Delegate running the main loop to CADisplayLink.
            //
            // Note that this is most effective in single thread mode.
            // .. In multi-threaded mode it will time the *input* thread to the callbacks. This is kinda silly,
            // but users shouldn't be using multi-threaded mode in the first place. Disabling it completely on
            // iOS may be a good forward direction if this ever comes up, as a user may see a potentially higher
            // frame rate with multi-threaded mode turned on, but it is going to give them worse input latency
            // and higher power usage.

            SDL_SetiOSEventPump(false);
            SDL_SetiOSAnimationCallback(SDLWindowHandle, 1, &runFrame, ObjectHandle.Handle);
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void runFrame(IntPtr userdata)
        {
            var handle = new ObjectHandle<IOSWindow>(userdata);

            if (handle.GetTarget(out IOSWindow window))
                window.RunFrame();
        }

        private void updateSafeArea()
        {
            Debug.Assert(uiWindow != null);

            SafeAreaPadding.Value = new MarginPadding
            {
                Top = (float)uiWindow.SafeAreaInsets.Top * Scale,
                Left = (float)uiWindow.SafeAreaInsets.Left * Scale,
                Bottom = (float)uiWindow.SafeAreaInsets.Bottom * Scale,
                Right = (float)uiWindow.SafeAreaInsets.Right * Scale,
            };
        }
    }
}
