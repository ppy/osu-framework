// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using osu.Framework.Configuration;
using osu.Framework.Logging;
using osu.Framework.Platform.MacOS.Native;
using OpenTK;

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

        private MethodInfo methodKeyDown;
        private MethodInfo methodKeyUp;

        private object nativeWindow;

        internal Action<Action> InvokeOnInputThread;

        public MacOSGameWindow()
        {
            Load += OnLoad;
        }

        private NSWindowStyleMask styleMask => (NSWindowStyleMask)Cocoa.SendUint(WindowInfo.Handle, selStyleMask);

        private bool isFullScreen() => (styleMask & NSWindowStyleMask.FullScreen) != 0;

        protected void OnLoad(object sender, EventArgs e)
        {
            try
            {
                flagsChangedHandler = flagsChanged;
                windowWillUseFullScreenHandler = windowWillUseFullScreen;
                windowDidEnterFullScreenHandler = windowDidEnterFullScreen;
                windowDidExitFullScreenHandler = windowDidExitFullScreen;
                windowShouldZoomToFrameHandler = windowShouldZoomToFrame;

                var fieldImplementation = typeof(NativeWindow).GetRuntimeFields().Single(x => x.Name == "implementation");
                var typeCocoaNativeWindow = typeof(NativeWindow).Assembly.GetTypes().Single(x => x.Name == "CocoaNativeWindow");
                var fieldWindowClass = typeCocoaNativeWindow.GetRuntimeFields().Single(x => x.Name == "windowClass");

                nativeWindow = fieldImplementation.GetValue(Implementation);
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

                methodKeyDown = nativeWindow.GetType().GetRuntimeMethods().Single(x => x.Name == "OnKeyDown");
                methodKeyUp = nativeWindow.GetType().GetRuntimeMethods().Single(x => x.Name == "OnKeyUp");
            }
            catch
            {
                Logger.Log("Window initialisation couldn't complete, likely due to the SDL backend being enabled.", LoggingTarget.Runtime, LogLevel.Important);
                Logger.Log("Execution will continue but keyboard functionality may be limited.", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        private NSApplicationPresentationOptions presentationOptionsForWindowMode(WindowMode windowMode)
        {
            switch (windowMode)
            {
                case Configuration.WindowMode.Fullscreen:
                    return NSApplicationPresentationOptions.HideDock | NSApplicationPresentationOptions.HideMenuBar | NSApplicationPresentationOptions.FullScreen;
                case Configuration.WindowMode.Borderless:
                case Configuration.WindowMode.Windowed:
                    return NSApplicationPresentationOptions.AutoHideDock | NSApplicationPresentationOptions.AutoHideMenuBar | NSApplicationPresentationOptions.FullScreen;
                default:
                    return NSApplicationPresentationOptions.Default;
            }
        }

        private uint windowWillUseFullScreen(IntPtr self, IntPtr cmd, IntPtr window, uint options) => (uint)presentationOptionsForWindowMode(WindowMode.Value);

        private void windowDidEnterFullScreen(IntPtr self, IntPtr cmd, IntPtr notification)
        {
            if (WindowMode.Value == Configuration.WindowMode.Windowed)
                WindowMode.Value = Configuration.WindowMode.Borderless;
            NSApplication.PresentationOptions = presentationOptionsForWindowMode(WindowMode.Value);
        }

        private void windowDidExitFullScreen(IntPtr self, IntPtr cmd, IntPtr notification) => WindowMode.Value = Configuration.WindowMode.Windowed;

        private void flagsChanged(IntPtr self, IntPtr cmd, IntPtr sender)
        {
            var modifierFlags = (CocoaKeyModifiers)Cocoa.SendInt(sender, selModifierFlags);
            var keyCode = Cocoa.SendInt(sender, selKeyCode);

            bool keyDown;
            OpenTK.Input.Key key;

            switch ((MacOSKeyCodes)keyCode)
            {
                case MacOSKeyCodes.LShift:
                    key = OpenTK.Input.Key.LShift;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.LeftShift);
                    break;

                case MacOSKeyCodes.RShift:
                    key = OpenTK.Input.Key.RShift;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.RightShift);
                    break;

                case MacOSKeyCodes.LControl:
                    key = OpenTK.Input.Key.LControl;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.LeftControl);
                    break;

                case MacOSKeyCodes.RControl:
                    key = OpenTK.Input.Key.RControl;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.RightControl);
                    break;

                case MacOSKeyCodes.LAlt:
                    key = OpenTK.Input.Key.LAlt;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.LeftAlt);
                    break;

                case MacOSKeyCodes.RAlt:
                    key = OpenTK.Input.Key.RAlt;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.RightAlt);
                    break;

                case MacOSKeyCodes.LCommand:
                    key = OpenTK.Input.Key.LWin;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.LeftCommand);
                    break;

                case MacOSKeyCodes.RCommand:
                    key = OpenTK.Input.Key.RWin;
                    keyDown = modifierFlags.HasFlag(CocoaKeyModifiers.RightCommand);
                    break;

                default:
                    return;
            }

            if (keyDown)
                methodKeyDown.Invoke(nativeWindow, new object[] { key, false });
            else
                methodKeyUp.Invoke(nativeWindow, new object[] { key });
        }

        // FIXME: OpenTK's current window:shouldZoomToFrame: is broken and can't be overridden, so we replace it
        private bool windowShouldZoomToFrame(IntPtr self, IntPtr cmd, IntPtr nsWindow, RectangleF toFrame) => true;

        protected override void UpdateWindowMode(WindowMode newMode)
        {
            InvokeOnInputThread.Invoke(() =>
            {
                bool currentFullScreen = isFullScreen();
                bool toggleFullScreen = newMode == Configuration.WindowMode.Borderless || newMode == Configuration.WindowMode.Fullscreen ? !currentFullScreen : currentFullScreen;

                if (toggleFullScreen)
                    Cocoa.SendVoid(WindowInfo.Handle, selToggleFullScreen, IntPtr.Zero);
                else if (currentFullScreen)
                    NSApplication.PresentationOptions = presentationOptionsForWindowMode(newMode);
            });
        }

        // Apple recommends not changing the system resolution for fullscreen access
        protected override void ChangeResolution(Size newSize) => ClientSize = newSize;

        protected override void RestoreResolution(DisplayDevice displayDevice)
        {
        }
    }

    internal enum CocoaKeyModifiers
    {
        LeftControl = 1 << 0,
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
        Titled = 1 << 0,
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
        AutoHideDock = 1 << 0,
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
        AutoHideToolbar = 1 << 11
    }
}
