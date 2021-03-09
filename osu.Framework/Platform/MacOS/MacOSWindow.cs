// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform.MacOS.Native;
using osuTK;

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

        /// <summary>
        /// The default fullscreen presentation options SDL uses: https://github.com/libsdl-org/SDL/blob/a4ddb175f1f1d832960c830191daaab7eb25638f/src/video/cocoa/SDL_cocoawindow.m#L899-L906.
        /// </summary>
        private const NSApplicationPresentationOptions default_fullscreen_presentation_options =
            NSApplicationPresentationOptions.HideDock | NSApplicationPresentationOptions.HideMenuBar | NSApplicationPresentationOptions.FullScreen;

        private delegate uint WindowWillUseFullScreenDelegate(IntPtr self, IntPtr cmd, IntPtr window, uint options);

        private delegate void ScrollWheelDelegate(IntPtr handle, IntPtr selector, IntPtr theEvent); // v@:@

        private WindowWillUseFullScreenDelegate windowWillUseFullScreenHandler;

        private IntPtr originalScrollWheel;
        private ScrollWheelDelegate scrollWheelHandler;

        public override bool CursorVisible
        {
            get => base.CursorVisible;
            set
            {
                base.CursorVisible = value;
                updateCursorAssistanceState();
            }
        }

        public override void Create()
        {
            base.Create();

            // replace [SDLView scrollWheel:(NSEvent *)] with our own version
            var viewClass = Class.Get("SDLView");
            scrollWheelHandler = scrollWheel;
            originalScrollWheel = Class.SwizzleMethod(viewClass, "scrollWheel:", "v@:@", scrollWheelHandler);

            // handle invisible cursor when providing presentation options for "fullscreen desktop" mode.
            // as SDL overwrites the options there, see https://github.com/libsdl-org/SDL/blob/a4ddb175f1f1d832960c830191daaab7eb25638f/src/video/cocoa/SDL_cocoawindow.m#L899-L906.
            var windowClass = Class.Get("Cocoa_WindowListener");
            windowWillUseFullScreenHandler = windowWillUseFullScreen;
            Class.RegisterMethod(windowClass, windowWillUseFullScreenHandler, "window:willUseFullScreenPresentationOptions:", "I@:@I");

            CursorInWindow.BindValueChanged(_ => updateCursorAssistanceState(), true);
        }

        private bool shouldDisableCursorAssistance => CursorInWindow.Value && !CursorVisible;

        private uint windowWillUseFullScreen(IntPtr self, IntPtr cmd, IntPtr window, uint options)
        {
            var fullscreenOptions = default_fullscreen_presentation_options;

            if (shouldDisableCursorAssistance)
                fullscreenOptions |= NSApplicationPresentationOptions.DisableCursorLocationAssistance;

            return (uint)fullscreenOptions;
        }

        private void updateCursorAssistanceState()
        {
            if (shouldDisableCursorAssistance)
                NSApplication.PresentationOptions |= NSApplicationPresentationOptions.DisableCursorLocationAssistance;
            else
                NSApplication.PresentationOptions &= ~NSApplicationPresentationOptions.DisableCursorLocationAssistance;
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
                Cocoa.SendVoid(receiver, originalScrollWheel, theEvent);
                return;
            }

            // according to osuTK, 0.1f is the scaling factor expected to be returned by CGEventSourceGetPixelsPerLine
            const float scale_factor = 0.1f;

            float scrollingDeltaX = Cocoa.SendFloat(theEvent, sel_scrollingdeltax);
            float scrollingDeltaY = Cocoa.SendFloat(theEvent, sel_scrollingdeltay);

            ScheduleEvent(() => OnMouseWheel(new Vector2(scrollingDeltaX * scale_factor, scrollingDeltaY * scale_factor), true));
        }
    }
}
