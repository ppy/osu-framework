// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Input;
using osu.Framework.Input.States;
using osu.Framework.Input.Handlers.Touchpad;
using osu.Framework.Platform.MacOS.Native;
using osuTK;

namespace osu.Framework.Platform.MacOS
{
    public class MacOSTouchpadHandler : TouchpadHandler
    {
        IntPtr[] activeTouches = new IntPtr[TouchState.MAX_TOUCH_COUNT];

        // todo need to make an index of all identities, for touchpad, unfortunately tho, the god damn event doesn't have a cancel event?!?!?
        private static readonly IntPtr sel_alltouches_ = Selector.Get("allTouches");
        private static readonly IntPtr sel_allobjects = Selector.Get("allObjects");

        private delegate void TouchesDelegate(IntPtr handle, IntPtr selector, IntPtr theEvent); // v@:@

        private TouchesDelegate touchesMoveEventHandler;
        private TouchesDelegate touchesBeginEventHandler;
        private TouchesDelegate touchesEndEventHandler;

        public override bool IsActive => Enabled.Value;
        
        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            if (!(host.Window is SDL2DesktopWindow desktopWindow))
                return false;

            // todo may need to modify instance variable allowedTouchTypes in (WindowHandle (SDLView)) and null safety

            var viewClass = Class.Get("SDLView");

            // This ensures that we get resting touches
            IntPtr nsView = Cocoa.SendIntPtr(desktopWindow.WindowHandle, Selector.Get("contentView"));
            if (nsView != IntPtr.Zero) Cocoa.SendVoid(nsView, Selector.Get("setWantsRestingTouches:"), true);

            // replace [SDLView touchesBeganWithEvent:(NSEvent *)] and other related events with our own version
            touchesBeginEventHandler = touchesBeginEvent;
            touchesMoveEventHandler = touchesMoveEvent;
            touchesEndEventHandler = touchesEndEvent;
            Class.SwizzleMethod(viewClass, "touchesBeganWithEvent:", "v@:@", touchesBeginEventHandler);
            Class.SwizzleMethod(viewClass, "touchesMovedWithEvent:", "v@:@", touchesMoveEventHandler);
            Class.SwizzleMethod(viewClass, "touchesEndedWithEvent:", "v@:@", touchesEndEventHandler);
            Class.SwizzleMethod(viewClass, "touchesCancelledWithEvent:", "v@:@", touchesEndEventHandler);

            return true;
        }

        private TouchSource? primaryTouchSource;
        
        private void touchesBeginEvent(IntPtr reciever, IntPtr selector, IntPtr theEvent)
        {
            foreach (NSTouch touch in getTouches(theEvent))
            {
                if (touch.Phase() == NSTouchPhase.NSTouchPhaseBegan)
                {
                    TouchSource? source = assignNextAvailableTouchSource(touch);
                    if (primaryTouchSource == null)
                    {
                        primaryTouchSource = source;
                        Vector2 currentTouch = touch.NormalizedPosition();
                        currentTouch.Y = 1 - currentTouch.Y;
                        HandleTouchpadMove(new Vector2[] { currentTouch });
                    }
                }
            }
        }

        /// <summary>
        /// Swizzled replacement of [SDLView touchesBegan:(NSEvent *)] that checks for touches on the MacOS trackpad.
        /// </summary>
        private void touchesMoveEvent(IntPtr reciever, IntPtr selector, IntPtr theEvent)
        {
            if (!Enabled.Value) return;

            Vector2 primaryTouch = default(Vector2);

            foreach (NSTouch touch in getTouches(theEvent))
            {
                TouchSource? source = getTouchSource(touch);
                
                if (source == primaryTouchSource) 
                {
                    primaryTouch = touch.NormalizedPosition();
                }

                switch(touch.Phase())
                {
                    case NSTouchPhase.NSTouchPhaseMoved:
                        break;
                }
            }

            primaryTouch.Y = 1 - primaryTouch.Y;
            HandleTouchpadMove(new Vector2[] { primaryTouch });
        }

        private void touchesEndEvent(IntPtr reciever, IntPtr selector, IntPtr theEvent)
        {
            if (!Enabled.Value) return;

            NSTouch? firstNotEndingTouch = null;

            foreach (NSTouch touch in getTouches(theEvent))
            {
                TouchSource? source = getTouchSource(touch);
               
                if (primaryTouchSource == source) primaryTouchSource = null;

                switch(touch.Phase())
                {
                    case NSTouchPhase.NSTouchPhaseEnded:
                        activeTouches[(int) source] = IntPtr.Zero;
                        break;
                    case NSTouchPhase.NSTouchPhaseCancelled:
                        activeTouches[(int) source] = IntPtr.Zero;
                        break;
                    default:
                        if (firstNotEndingTouch == null) firstNotEndingTouch = touch;
                        break;
                }
            }

            if (primaryTouchSource == null && firstNotEndingTouch != null)
            {
                primaryTouchSource = getTouchSource( firstNotEndingTouch.Value );
                Vector2 touchLocation = firstNotEndingTouch.Value.NormalizedPosition();
                touchLocation.Y = 1 - touchLocation.Y;
                HandleTouchpadMove(new Vector2[] { touchLocation });
            }
        }

        private IEnumerable<NSTouch> getTouches(IntPtr theEvent)
        {
            IntPtr allTouches = Cocoa.SendIntPtr(theEvent, sel_alltouches_);

            IntPtr[] touchptrs = new NSArray(Cocoa.SendIntPtr(allTouches, sel_allobjects)).ToArray();

            NSTouch[] touches = new NSTouch[touchptrs.Length]; 

            for (var i = 0; i < touchptrs.Length; i++)
            {
                NSTouch touch = new NSTouch(touchptrs[i]);
                touches[i] = touch;
            }

            return touches;
        }

        private TouchSource? assignNextAvailableTouchSource(NSTouch touch)
        {
            int index = Array.IndexOf(activeTouches, IntPtr.Zero);
            if (index == -1) return null;
            activeTouches[index] = touch.CopyOfIdentity();
            return (TouchSource) index;
        }

        private TouchSource? getTouchSource(NSTouch touch)
        {
            for (var i = 0; i < activeTouches.Length; i++)
            {
                if (touch.IsIdentityEqual(activeTouches[i])) return (TouchSource) i;
            }

            return null;
        }
    }
}
