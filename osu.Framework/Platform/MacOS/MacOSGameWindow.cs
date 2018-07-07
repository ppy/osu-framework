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
        private delegate uint WillUseFullScreenDelegate(IntPtr self, IntPtr cmd, IntPtr window, uint options);

        private FlagsChangedDelegate flagsChangedHandler;
        private WillUseFullScreenDelegate willUseFullScreenHandler;

        private readonly IntPtr selModifierFlags = Selector.Get("modifierFlags");
        private readonly IntPtr selKeyCode = Selector.Get("keyCode");
        private readonly IntPtr selStyleMask = Selector.Get("styleMask");
        private readonly IntPtr selToggleFullScreen = Selector.Get("toggleFullScreen:");

        private MethodInfo methodKeyDown;
        private MethodInfo methodKeyUp;

        private const int modifier_flag_left_control = 1 << 0;
        private const int modifier_flag_left_shift = 1 << 1;
        private const int modifier_flag_right_shift = 1 << 2;
        private const int modifier_flag_left_command = 1 << 3;
        private const int modifier_flag_right_command = 1 << 4;
        private const int modifier_flag_left_alt = 1 << 5;
        private const int modifier_flag_right_alt = 1 << 6;
        private const int modifier_flag_right_control = 1 << 13;

        private object nativeWindow;

        internal Action<Action> InvokeOnInputThread;

        public MacOSGameWindow()
        {
            Load += OnLoad;
        }

        protected void OnLoad(object sender, EventArgs e)
        {
            try
            {
                flagsChangedHandler = flagsChanged;
                willUseFullScreenHandler = willUseFullScreen;

                var fieldImplementation = typeof(OpenTK.NativeWindow).GetRuntimeFields().Single(x => x.Name == "implementation");
                var typeCocoaNativeWindow = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "CocoaNativeWindow");
                var fieldWindowClass = typeCocoaNativeWindow.GetRuntimeFields().Single(x => x.Name == "windowClass");

                nativeWindow = fieldImplementation.GetValue(Implementation);
                var windowClass = (IntPtr)fieldWindowClass.GetValue(nativeWindow);

                Class.RegisterMethod(windowClass, flagsChangedHandler, "flagsChanged:", "v@:@");
                Class.RegisterMethod(windowClass, willUseFullScreenHandler, "window:willUseFullScreenPresentationOptions:", "I@:@I");

                methodKeyDown = nativeWindow.GetType().GetRuntimeMethods().Single(x => x.Name == "OnKeyDown");
                methodKeyUp = nativeWindow.GetType().GetRuntimeMethods().Single(x => x.Name == "OnKeyUp");
            }
            catch
            {
                Logger.Log("Window initialisation couldn't complete, likely due to the SDL backend being enabled.", LoggingTarget.Runtime, LogLevel.Important);
                Logger.Log("Execution will continue but keyboard functionality may be limited.", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        private uint willUseFullScreen(IntPtr self, IntPtr cmd, IntPtr window, uint options) =>
            (uint)(NSApplicationPresentationOptions.HideDock | NSApplicationPresentationOptions.HideMenuBar | NSApplicationPresentationOptions.FullScreen);

        private void flagsChanged(IntPtr self, IntPtr cmd, IntPtr sender)
        {
            var modifierFlags = Cocoa.SendInt(sender, selModifierFlags);
            var keyCode = Cocoa.SendInt(sender, selKeyCode);

            bool keyDown;
            OpenTK.Input.Key key;

            switch ((MacOSKeyCodes)keyCode)
            {
                case MacOSKeyCodes.LShift:
                    key = OpenTK.Input.Key.LShift;
                    keyDown = (modifierFlags & modifier_flag_left_shift) > 0;
                    break;

                case MacOSKeyCodes.RShift:
                    key = OpenTK.Input.Key.RShift;
                    keyDown = (modifierFlags & modifier_flag_right_shift) > 0;
                    break;

                case MacOSKeyCodes.LControl:
                    key = OpenTK.Input.Key.LControl;
                    keyDown = (modifierFlags & modifier_flag_left_control) > 0;
                    break;

                case MacOSKeyCodes.RControl:
                    key = OpenTK.Input.Key.RControl;
                    keyDown = (modifierFlags & modifier_flag_right_control) > 0;
                    break;

                case MacOSKeyCodes.LAlt:
                    key = OpenTK.Input.Key.LAlt;
                    keyDown = (modifierFlags & modifier_flag_left_alt) > 0;
                    break;

                case MacOSKeyCodes.RAlt:
                    key = OpenTK.Input.Key.RAlt;
                    keyDown = (modifierFlags & modifier_flag_right_alt) > 0;
                    break;

                case MacOSKeyCodes.LCommand:
                    key = OpenTK.Input.Key.LWin;
                    keyDown = (modifierFlags & modifier_flag_left_command) > 0;
                    break;

                case MacOSKeyCodes.RCommand:
                    key = OpenTK.Input.Key.RWin;
                    keyDown = (modifierFlags & modifier_flag_right_command) > 0;
                    break;

                default:
                    return;
            }

            if (keyDown)
                methodKeyDown.Invoke(nativeWindow, new object[] { key, false });
            else
                methodKeyUp.Invoke(nativeWindow, new object[] { key });
        }

        protected override void UpdateWindowMode(WindowMode newMode)
        {
            InvokeOnInputThread.Invoke(() =>
            {
                bool toggleFullscreen;
                if (newMode == Configuration.WindowMode.Borderless || newMode == Configuration.WindowMode.Fullscreen)
                    toggleFullscreen = (Cocoa.SendUint(WindowInfo.Handle, selStyleMask) & (uint)NSWindowStyleMask.FullScreen) == 0;
                else
                    toggleFullscreen = (Cocoa.SendUint(WindowInfo.Handle, selStyleMask) & (uint)NSWindowStyleMask.FullScreen) != 0;
                if (toggleFullscreen)
                    Cocoa.SendVoid(WindowInfo.Handle, selToggleFullScreen, IntPtr.Zero);
            });
        }

        // Apple recommends not changing the system resolution for fullscreen access
        protected override void ChangeResolution(Size newSize) => ClientSize = newSize;

        protected override void RestoreResolution(DisplayDevice displayDevice)
        {
        }
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
