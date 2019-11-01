// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using osu.Framework.Configuration;
using osu.Framework.Logging;
using osu.Framework.Platform.MacOS.Native;
using osuTK;
using System.Diagnostics;

namespace osu.Framework.Platform.MacOS
{
    internal class MacOSGameWindow : DesktopGameWindow
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void FlagsChangedDelegate(IntPtr self, IntPtr cmd, IntPtr notification);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate uint WindowWillUseFullScreenDelegate(IntPtr self, IntPtr cmd, IntPtr window, uint options);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void WindowDidEnterFullScreenDelegate(IntPtr self, IntPtr cmd, IntPtr notification);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void WindowDidExitFullScreenDelegate(IntPtr self, IntPtr cmd, IntPtr notification);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool WindowShouldZoomToFrameDelegate(IntPtr self, IntPtr cmd, IntPtr nsWindow, RectangleF toFrame);

        private FlagsChangedDelegate flagsChangedHandler;
        private WindowWillUseFullScreenDelegate windowWillUseFullScreenHandler;
        private WindowDidEnterFullScreenDelegate windowDidEnterFullScreenHandler;
        private WindowDidExitFullScreenDelegate windowDidExitFullScreenHandler;
        private WindowShouldZoomToFrameDelegate windowShouldZoomToFrameHandler;

        private readonly IntPtr selModifierFlags = Selector.Get("modifierFlags");
        private readonly IntPtr selKeyCode = Selector.Get("keyCode");
        private readonly IntPtr selStyleMask = Selector.Get("styleMask");
        private readonly IntPtr selToggleFullScreen = Selector.Get("toggleFullScreen:");
        private readonly IntPtr selMenuBarVisible = Selector.Get("menuBarVisible");
        private readonly IntPtr classNSMenu = Class.Get("NSMenu");

        private Action<osuTK.Input.Key, bool> actionKeyDown;
        private Action<osuTK.Input.Key> actionKeyUp;
        private Action actionInvalidateCursorRects;

        private WindowMode? pendingWindowMode;

        public MacOSGameWindow()
        {
            Load += OnLoad;
            UpdateFrame += OnUpdateFrame;
        }

        private NSWindowStyleMask styleMask => (NSWindowStyleMask)Cocoa.SendUint(WindowInfo.Handle, selStyleMask);

        private bool menuBarVisible => Cocoa.SendBool(classNSMenu, selMenuBarVisible);

        protected override IEnumerable<WindowMode> DefaultSupportedWindowModes => new[]
        {
            Configuration.WindowMode.Windowed,
            Configuration.WindowMode.Fullscreen,
        };

        protected void OnLoad(object sender, EventArgs e)
        {
            try
            {
                flagsChangedHandler = flagsChanged;
                windowWillUseFullScreenHandler = windowWillUseFullScreen;
                windowDidEnterFullScreenHandler = windowDidEnterFullScreen;
                windowDidExitFullScreenHandler = windowDidExitFullScreen;
                windowShouldZoomToFrameHandler = windowShouldZoomToFrame;

                const BindingFlags instance_member = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

                var fieldImplementation = typeof(NativeWindow).GetField("implementation", instance_member);
                Debug.Assert(fieldImplementation != null, "Reflection is broken!");

                var nativeWindow = fieldImplementation.GetValue(Implementation);
                Debug.Assert(nativeWindow != null, "Reflection is broken!");

                var typeCocoaNativeWindow = nativeWindow.GetType();
                Debug.Assert(typeCocoaNativeWindow.Name == "CocoaNativeWindow", "Reflection is broken!");

                var fieldWindowClass = typeCocoaNativeWindow.GetField("windowClass", instance_member);
                Debug.Assert(fieldWindowClass != null, "Reflection is broken!");

                var windowClass = (IntPtr)fieldWindowClass.GetValue(nativeWindow);

                // register new methods
                Class.RegisterMethod(windowClass, flagsChangedHandler, "flagsChanged:", "v@:@");
                Class.RegisterMethod(windowClass, windowWillUseFullScreenHandler, "window:willUseFullScreenPresentationOptions:", "I@:@I");
                Class.RegisterMethod(windowClass, windowDidEnterFullScreenHandler, "windowDidEnterFullScreen:", "v@:@");
                Class.RegisterMethod(windowClass, windowDidExitFullScreenHandler, "windowDidExitFullScreen:", "v@:@");

                // replace methods that currently break
                Class.RegisterMethod(windowClass, windowShouldZoomToFrameHandler, "windowShouldZoom:toFrame:", "b@:@{NSRect={NSPoint=ff}{NSSize=ff}}");

                NSNotificationCenter.AddObserver(WindowInfo.Handle, Selector.Get("windowDidEnterFullScreen:"), NSNotificationCenter.WINDOW_DID_ENTER_FULL_SCREEN, IntPtr.Zero);
                NSNotificationCenter.AddObserver(WindowInfo.Handle, Selector.Get("windowDidExitFullScreen:"), NSNotificationCenter.WINDOW_DID_EXIT_FULL_SCREEN, IntPtr.Zero);

                var methodKeyDown = typeCocoaNativeWindow.GetMethod("OnKeyDown", instance_member);
                Debug.Assert(methodKeyDown != null, "Reflection is broken!");
                actionKeyDown = (Action<osuTK.Input.Key, bool>)methodKeyDown.CreateDelegate(typeof(Action<osuTK.Input.Key, bool>), nativeWindow);

                var methodKeyUp = typeCocoaNativeWindow.GetMethod("OnKeyUp", instance_member);
                Debug.Assert(methodKeyUp != null, "Reflection is broken!");
                actionKeyUp = (Action<osuTK.Input.Key>)methodKeyUp.CreateDelegate(typeof(Action<osuTK.Input.Key>), nativeWindow);

                var methodInvalidateCursorRects = typeCocoaNativeWindow.GetMethod("InvalidateCursorRects", instance_member);
                Debug.Assert(methodInvalidateCursorRects != null, "Reflection is broken!");
                actionInvalidateCursorRects = (Action)methodInvalidateCursorRects.CreateDelegate(typeof(Action), nativeWindow);
            }
            catch
            {
                Logger.Log("Window initialisation couldn't complete, likely due to the SDL backend being enabled.", LoggingTarget.Runtime, LogLevel.Important);
                Logger.Log("Execution will continue but keyboard functionality may be limited.", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        private const NSApplicationPresentationOptions default_fullscreen_presentation_options =
            NSApplicationPresentationOptions.AutoHideDock | NSApplicationPresentationOptions.AutoHideMenuBar | NSApplicationPresentationOptions.FullScreen;

        private bool isCursorHidden => CursorState.HasFlag(CursorState.Hidden);

        private NSApplicationPresentationOptions fullscreenPresentationOptions =>
            default_fullscreen_presentation_options | (isCursorHidden ? NSApplicationPresentationOptions.DisableCursorLocationAssistance : 0);

        private uint windowWillUseFullScreen(IntPtr self, IntPtr cmd, IntPtr window, uint options) => (uint)fullscreenPresentationOptions;

        private void windowDidEnterFullScreen(IntPtr self, IntPtr cmd, IntPtr notification)
        {
            if ((pendingWindowMode ?? WindowMode.Value) == Configuration.WindowMode.Windowed)
                pendingWindowMode = Configuration.WindowMode.Fullscreen;
        }

        private void windowDidExitFullScreen(IntPtr self, IntPtr cmd, IntPtr notification) => pendingWindowMode = Configuration.WindowMode.Windowed;

        protected void OnUpdateFrame(object sender, FrameEventArgs e)
        {
            // update the window mode if we have an update queued
            WindowMode? mode = pendingWindowMode;

            if (mode.HasValue)
            {
                pendingWindowMode = null;

                bool currentFullScreen = styleMask.HasFlag(NSWindowStyleMask.FullScreen);
                bool toggleFullScreen = mode.Value == Configuration.WindowMode.Fullscreen ? !currentFullScreen : currentFullScreen;

                if (toggleFullScreen)
                    Cocoa.SendVoid(WindowInfo.Handle, selToggleFullScreen, IntPtr.Zero);
                else if (currentFullScreen)
                    NSApplication.PresentationOptions = fullscreenPresentationOptions;
                else if (isCursorHidden)
                    NSApplication.PresentationOptions = NSApplicationPresentationOptions.DisableCursorLocationAssistance;

                WindowMode.Value = mode.Value;
            }

            // If the cursor should be hidden, but something in the system has made it appear (such as a notification),
            // invalidate the cursor rects to hide it.  osuTK has a private function that does this.
            if (isCursorHidden && Cocoa.CGCursorIsVisible() && !menuBarVisible)
                actionInvalidateCursorRects();
        }

        private void flagsChanged(IntPtr self, IntPtr cmd, IntPtr sender)
        {
            var modifierFlags = (CocoaKeyModifiers)Cocoa.SendInt(sender, selModifierFlags);
            var keyCode = Cocoa.SendInt(sender, selKeyCode);

            bool keyDown;
            osuTK.Input.Key key;

            switch ((MacOSKeyCodes)keyCode)
            {
                case MacOSKeyCodes.LShift:
                    key = osuTK.Input.Key.LShift;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.LeftShift);
                    break;

                case MacOSKeyCodes.RShift:
                    key = osuTK.Input.Key.RShift;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.RightShift);
                    break;

                case MacOSKeyCodes.LControl:
                    key = osuTK.Input.Key.LControl;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.LeftControl);
                    break;

                case MacOSKeyCodes.RControl:
                    key = osuTK.Input.Key.RControl;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.RightControl);
                    break;

                case MacOSKeyCodes.LAlt:
                    key = osuTK.Input.Key.LAlt;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.LeftAlt);
                    break;

                case MacOSKeyCodes.RAlt:
                    key = osuTK.Input.Key.RAlt;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.RightAlt);
                    break;

                case MacOSKeyCodes.LCommand:
                    key = osuTK.Input.Key.LWin;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.LeftCommand);
                    break;

                case MacOSKeyCodes.RCommand:
                    key = osuTK.Input.Key.RWin;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.RightCommand);
                    break;

                default:
                    return;
            }

            if (keyDown)
                actionKeyDown(key, false);
            else
                actionKeyUp(key);
        }

        // FIXME: osuTK's current window:shouldZoomToFrame: is broken and can't be overridden, so we replace it
        private bool windowShouldZoomToFrame(IntPtr self, IntPtr cmd, IntPtr nsWindow, RectangleF toFrame) => true;

        protected override void UpdateWindowMode(WindowMode newMode)
        {
            pendingWindowMode = newMode;

            // local implementation is overriding Fullscreen/Borderless behaviour.
            if (newMode != Configuration.WindowMode.Windowed) return;

            base.UpdateWindowMode(newMode);
        }

        // Apple recommends not changing the system resolution for fullscreen access
        protected override void ChangeResolution(DisplayDevice display, Size newSize) => ClientSize = newSize;

        // Doesn't return any resolution for the reason mentioned above
        public override IEnumerable<DisplayResolution> AvailableResolutions => Enumerable.Empty<DisplayResolution>();

        protected override void RestoreResolution(DisplayDevice displayDevice)
        {
        }
    }

    internal enum CocoaKeyModifiers
    {
        LeftControl = 1,
        LeftShift = 1 << 1,
        RightShift = 1 << 2,
        LeftCommand = 1 << 3,
        RightCommand = 1 << 4,
        LeftAlt = 1 << 5,
        RightAlt = 1 << 6,
        RightControl = 1 << 13,
    }

    internal enum MacOSKeyCodes
    {
        LShift = 56,
        RShift = 60,
        LControl = 59,
        RControl = 62,
        LAlt = 58,
        RAlt = 61,
        LCommand = 55,
        RCommand = 54,
        CapsLock = 57,
        Function = 63
    }

    [Flags]
    internal enum NSWindowStyleMask
    {
        Borderless = 0,
        Titled = 1,
        Closable = 1 << 1,
        Miniaturizable = 1 << 2,
        Resizable = 1 << 3,
        TexturedBackground = 1 << 8,
        UnifiedTitleAndToolbar = 1 << 12,
        FullScreen = 1 << 14,
        FullSizeContentView = 1 << 15,
        UtilityWindow = 1 << 4,
        DocModalWindow = 1 << 6,
        NonactivatingPanel = 1 << 7,
        HUDWindow = 1 << 13
    }

    [Flags]
    internal enum NSApplicationPresentationOptions
    {
        Default = 0,
        AutoHideDock = 1,
        HideDock = 1 << 1,
        AutoHideMenuBar = 1 << 2,
        HideMenuBar = 1 << 3,
        DisableAppleMenu = 1 << 4,
        DisableProcessSwitching = 1 << 5,
        DisableForceQuit = 1 << 6,
        DisableSessionTermination = 1 << 7,
        DisableHideApplication = 1 << 8,
        DisableMenuBarTransparency = 1 << 9,
        FullScreen = 1 << 10,
        AutoHideToolbar = 1 << 11,
        DisableCursorLocationAssistance = 1 << 12
    }
}
