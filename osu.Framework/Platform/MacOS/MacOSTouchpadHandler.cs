// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Input.Handlers.Touchpad;
using osu.Framework.Platform.MacOS.Native;
using osuTK;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSTouchpadHandler : TouchpadHandler
    {
        private static readonly IntPtr sel_alltouches_ = Selector.Get("allTouches");
        private static readonly IntPtr sel_allobjects = Selector.Get("allObjects");

        private delegate void TouchesDelegate(IntPtr handle, IntPtr selector, IntPtr theEvent); // v@:@

        private TouchesDelegate touchesEventsHandler;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (!(host.Window is SDL2DesktopWindow sdl2DesktopWindow))
                return false;

            // todo may need to modify instance variable allowedTouchTypes in (WindowHandle (SDLView)) and null safety

            var viewClass = Class.Get("SDLView");

            // This ensures that we get resting touches
            IntPtr nsView = Cocoa.SendIntPtr(sdl2DesktopWindow.WindowHandle, Selector.Get("contentView"));
            if (nsView != IntPtr.Zero) Cocoa.SendVoid(nsView, Selector.Get("setWantsRestingTouches:"), true);

            // replace [SDLView touchesBeganWithEvent:(NSEvent *)] and other related events with our own version
            touchesEventsHandler = touchesEvents;
            Class.SwizzleMethod(viewClass, "touchesBeganWithEvent:", "v@:@", touchesEventsHandler);
            Class.SwizzleMethod(viewClass, "touchesMovedWithEvent:", "v@:@", touchesEventsHandler);
            Class.SwizzleMethod(viewClass, "touchesEndWithEvent:", "v@:@", touchesEventsHandler);
            Class.SwizzleMethod(viewClass, "touchesCancelledWithEvent:", "v@:@", touchesEventsHandler);

            return true;
        }

        /// <summary>
        /// Swizzled replacement of [SDLView touchesBegan:(NSEvent *)] that checks for touches on the MacOS trackpad.
        /// </summary>
        private void touchesEvents(IntPtr reciever, IntPtr selector, IntPtr theEvent)
        {
            IntPtr allTouches = Cocoa.SendIntPtr(theEvent, sel_alltouches_);

            IntPtr[] touchptrs = new NSArray(Cocoa.SendIntPtr(allTouches, sel_allobjects)).ToArray();

            Vector2[] touches = new Vector2[touchptrs.Length];

            foreach (IntPtr touchptr in touchptrs)
            {
                NSTouch touch = new NSTouch(touchptr);

                //Logger.Log($"{touch.NormalizedPosition()} {touch.Phase()}");

                // todo actually write an impl for this
                if (touch.Phase() != NSTouchPhase.NSTouchPhaseStationary)
                {
                    //ScheduleEvent(() => TriggerTrackpadPositionChanged(touch.NormalizedPosition()));
                }
            }

            // https://eternalstorms.wordpress.com/2015/11/16/how-to-detect-force-touch-capable-devices-on-the-mac/

            for (var i = 0; i < touchptrs.Length; i++)
            {
                NSTouch touch = new NSTouch(touchptrs[i]);
                touches[i] = touch.NormalizedPosition();
            }

            HandleTouchpadMove(touches);

            // todo I'm not too sure what to do, could possibly expose the TouchpadHandler via gamehost?
        }
    }
}
