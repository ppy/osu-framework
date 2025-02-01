// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    internal class IOSWindow : SDL3MobileWindow, IIOSWindow
    {
        public UIWindow UIWindow { get; private set; } = null!;

        public UIViewController ViewController => UIWindow.RootViewController!;

        private IOSCallObserver callObserver = null!;

        public override Size Size
        {
            get => base.Size;
            protected set
            {
                base.Size = value;
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

            UIWindow = Runtime.GetNSObject<UIWindow>(WindowHandle)!;
            updateSafeArea();

            var appDelegate = (GameApplicationDelegate)UIApplication.SharedApplication.Delegate;
            appDelegate.DragDrop += TriggerDragDrop;

            // osu! cannot operate when a call takes place, as the audio is completely cut from the game, making it behave in unexpected manner.
            // while this is o!f code, it's simpler to do this here rather than in osu!.
            // we can reconsider this if there are framework consumers which find this behaviour undesirable.
            callObserver = new IOSCallObserver();
            callObserver.OnCall += onCall;
            callObserver.OnCallEnded += onCallEnded;

            updateFocused();
        }

        private bool appInForeground;
        private bool inCall;

        protected override void HandleEvent(SDL_Event e)
        {
            switch (e.Type)
            {
                case SDL_EventType.SDL_EVENT_WINDOW_MINIMIZED:
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:
                    appInForeground = false;
                    updateFocused();
                    break;

                case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:
                case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:
                    appInForeground = true;
                    updateFocused();
                    break;

                default:
                    base.HandleEvent(e);
                    break;
            }
        }

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

        private void updateFocused() => Focused = appInForeground && !inCall;

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
            if (!Exists)
                return;

            SafeAreaPadding.Value = new MarginPadding
            {
                Top = (float)UIWindow.SafeAreaInsets.Top * Scale,
                Left = (float)UIWindow.SafeAreaInsets.Left * Scale,
                Bottom = (float)UIWindow.SafeAreaInsets.Bottom * Scale,
                Right = (float)UIWindow.SafeAreaInsets.Right * Scale,
            };
        }
    }
}
