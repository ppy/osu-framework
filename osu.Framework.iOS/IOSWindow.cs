// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Platform.SDL3;
using SDL;
using static SDL.SDL3;
using UIKit;

namespace osu.Framework.iOS
{
    internal class IOSWindow : SDL3MobileWindow
    {
        private UIWindow? window;
        private IOSCallObserver callObserver;

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

        public IOSWindow(GraphicsSurfaceType surfaceType, string appName)
            : base(surfaceType, appName)
        {
        }

        public override void Create()
        {
            SDL_SetHint(SDL_HINT_IOS_HIDE_HOME_INDICATOR, "2"u8);

            base.Create();

            window = Runtime.GetNSObject<UIWindow>(WindowHandle)!;
            updateSafeArea();

            UIApplication.Notifications.ObserveWillResignActive(onWillResignActive);
            UIApplication.Notifications.ObserveDidBecomeActive(onDidBecomeActive);

            // osu! cannot operate when a call takes place, as the audio is completely cut from the game, making it behave in unexpected manner.
            // while this is o!f code, it's simpler to do this here rather than in osu!.
            // we can reconsider this later on if there are framework consumers which find this behaviour undesirable.
            callObserver = new IOSCallObserver(onCall, onCallEnded);
            updateFocused();
        }

        private bool inCall;
        private bool active = true;

        private void onCall()
        {
            inCall = true;
            updateFocused();
        }

        private void onCallEnded()
        {
            inCall = false;
            updateFocused();
        }

        private void onDidBecomeActive(object? sender, NSNotificationEventArgs e)
        {
            active = true;
            updateFocused();
        }

        private void onWillResignActive(object? sender, NSNotificationEventArgs e)
        {
            active = false;
            updateFocused();
        }

        private void updateFocused() => Focused = active && !inCall;

        protected override void HandleEvent(SDL_Event e)
        {
            if (e.Type == SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED || e.Type == SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST)
                // "focus gained" triggers incorrectly at startup, overriding the state update logic above
                // in the case where a call was active before the app started.
                return;

            base.HandleEvent(e);
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
