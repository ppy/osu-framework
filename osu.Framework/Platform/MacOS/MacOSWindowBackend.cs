// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform.MacOS.Native;
using osu.Framework.Platform.Sdl;
using osuTK;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSWindowBackend : Sdl2WindowBackend
    {
        private static readonly IntPtr sel_hasprecisescrollingdeltas = Selector.Get("hasPreciseScrollingDeltas");
        private static readonly IntPtr sel_scrollingdeltax = Selector.Get("scrollingDeltaX");
        private static readonly IntPtr sel_scrollingdeltay = Selector.Get("scrollingDeltaY");
        private static readonly IntPtr sel_respondstoselector_ = Selector.Get("respondsToSelector:");

        private delegate void ScrollWheelDelegate(IntPtr handle, IntPtr selector, IntPtr theEvent); // v@:@

        private IntPtr swizzledScrollWheel;
        private ScrollWheelDelegate scrollWheelHandler;

        public override void Create()
        {
            base.Create();

            var viewClass = Class.Get("SDLView");
            scrollWheelHandler = scrollWheel;
            swizzledScrollWheel = Class.SwizzleMethod(viewClass, "scrollWheel:", "v@:@", scrollWheelHandler);
        }

        private void scrollWheel(IntPtr handle, IntPtr selector, IntPtr theEvent)
        {
            var hasPrecise = Cocoa.SendBool(theEvent, sel_respondstoselector_, sel_hasprecisescrollingdeltas) && Cocoa.SendBool(theEvent, sel_hasprecisescrollingdeltas);

            if (hasPrecise)
            {
                const float scale_factor = 0.1f;
                var scrollingDeltaX = Cocoa.SendFloat(theEvent, sel_scrollingdeltax);
                var scrollingDeltaY = Cocoa.SendFloat(theEvent, sel_scrollingdeltay);
                OnMouseWheel(new Vector2(scrollingDeltaX * scale_factor, scrollingDeltaY * scale_factor), true);
            }
            else
                Cocoa.SendVoid(handle, swizzledScrollWheel, theEvent);
        }
    }
}
