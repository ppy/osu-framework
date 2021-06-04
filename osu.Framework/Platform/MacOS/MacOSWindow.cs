// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform.MacOS.Native;
using osuTK;
using osu.Framework.Logging;
using SDL2;

namespace osu.Framework.Platform.MacOS
{
    /// <summary>
    /// macOS-specific subclass of <see cref="SDL2DesktopWindow"/>.
    /// </summary>
    public class MacOSWindow : SDL2DesktopWindow
    {
        private static readonly IntPtr sel_hasprecisescrollingdeltas = Selector.Get("hasPreciseScrollingDeltas");
        private static readonly IntPtr sel_scrollingdeltax = Selector.Get("scrollingDeltaX");
        private static readonly IntPtr sel_scrollingdeltay = Selector.Get("scrollingDeltaY");
        private static readonly IntPtr sel_respondstoselector_ = Selector.Get("respondsToSelector:");

        private delegate void ScrollWheelDelegate(IntPtr handle, IntPtr selector, IntPtr theEvent); // v@:@

        private IntPtr originalScrollWheel;
        private ScrollWheelDelegate scrollWheelHandler;

        private static readonly IntPtr sel_alltouches_ = Selector.Get("allTouches");
        private static readonly IntPtr sel_allobjects = Selector.Get("allObjects");
        private static readonly IntPtr sel_normalizedposition = Selector.Get("normalizedPosition");

        private delegate void TouchesBeganDelegate(IntPtr handle, IntPtr selector, IntPtr theEvent); // v@:@

        private TouchesBeganDelegate touchesBeganHandler;

        public override void Create()
        {
            base.Create();

            // replace [SDLView scrollWheel:(NSEvent *)] with our own version
            var viewClass = Class.Get("SDLView");
            scrollWheelHandler = scrollWheel;
            originalScrollWheel = Class.SwizzleMethod(viewClass, "scrollWheel:", "v@:@", scrollWheelHandler);

            // todo may need to modify instance variable allowedTouchTypes in (WindowHandle (SDLView))
            // replace [SDLView touchesBeganWithEvent:(NSEvent *)] and other related events with our own version
            touchesBeganHandler = touchesBegan;
            Class.SwizzleMethod(viewClass, "touchesBeganWithEvent:", "v@:@", touchesBeganHandler);
            Class.SwizzleMethod(viewClass, "touchesEndWithEvent:", "v@:@", touchesBeganHandler);
            Class.SwizzleMethod(viewClass, "touchesMovedWithEvent:", "v@:@", touchesBeganHandler);
            Class.SwizzleMethod(viewClass, "touchesEndWithEvent:", "v@:@", touchesBeganHandler);
        }

        /// <summary>
        /// Swizzled replacement of [SDLView scrollWheel:(NSEvent *)] that checks for precise scrolling deltas.
        /// </summary>
        private void scrollWheel(IntPtr receiver, IntPtr selector, IntPtr theEvent)
        {
            var hasPrecise = Cocoa.SendBool(theEvent, sel_respondstoselector_, sel_hasprecisescrollingdeltas) &&
                             Cocoa.SendBool(theEvent, sel_hasprecisescrollingdeltas);

            if (!hasPrecise)
            {
                // calls the unswizzled [SDLView scrollWheel:(NSEvent *)] method if this is a regular scroll wheel event
                // the receiver may sometimes not be SDLView, ensure it has a scroll wheel selector implemented before attempting to call.
                if (Cocoa.SendBool(receiver, sel_respondstoselector_, originalScrollWheel))
                    Cocoa.SendVoid(receiver, originalScrollWheel, theEvent);

                return;
            }

            // according to osuTK, 0.1f is the scaling factor expected to be returned by CGEventSourceGetPixelsPerLine
            const float scale_factor = 0.1f;

            float scrollingDeltaX = Cocoa.SendFloat(theEvent, sel_scrollingdeltax);
            float scrollingDeltaY = Cocoa.SendFloat(theEvent, sel_scrollingdeltay);

            ScheduleEvent(() => TriggerMouseWheel(new Vector2(scrollingDeltaX * scale_factor, scrollingDeltaY * scale_factor), true));
        }

        /// <summary>
        /// Swizzled replacement of [SDLView touchesBegan:(NSEvent *)] that checks for touches on the MacOS trackpad.
        /// </summary>
        private void touchesBegan(IntPtr reciever, IntPtr selector, IntPtr theEvent)
        {
            // todo make a NSSet and use this for it
            IntPtr allTouches = Cocoa.SendIntPtr(theEvent, sel_alltouches_);

            foreach (IntPtr touch in new NSArray(Cocoa.SendIntPtr(allTouches, sel_allobjects)).ToArray())
            {
                NSPoint point = Cocoa.SendNSPoint(touch, sel_normalizedposition);

                // todo actually write an impl for this
                ScheduleEvent(() => TriggerTrackpadPositionChanged(point));
            }
        }
    }
}
