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
using SDL;
using UIKit;

namespace osu.Framework.iOS
{
    internal class IOSWindow : SDL3Window
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

        protected override unsafe void UpdateWindowStateAndSize(WindowState state, Display display, DisplayMode displayMode)
        {
            // This sets the status bar to hidden.
            SDL3.SDL_SetWindowFullscreen(SDLWindowHandle, SDL_bool.SDL_TRUE);

            // Don't run base logic at all. Let's keep things simple.
        }

        public override void Create()
        {
            base.Create();

            window = Runtime.GetNSObject<UIWindow>(WindowHandle);
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
            SDL3.SDL_iPhoneSetEventPump(SDL_bool.SDL_FALSE);
            SDL3.SDL_iPhoneSetAnimationCallback(SDLWindowHandle, 1, &runFrame, ObjectHandle.Handle);
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
