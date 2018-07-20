// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using osu.Framework.Logging;
using osu.Framework.Platform.MacOS.Native;

namespace osu.Framework.Platform.MacOS
{
    internal class MacOSGameWindow : DesktopGameWindow
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void FlagsChangedDelegate(IntPtr self, IntPtr cmd, IntPtr notification);

        private FlagsChangedDelegate flagsChangedHandler;

        private readonly IntPtr selModifierFlags = Selector.Get("modifierFlags");
        private readonly IntPtr selKeyCode = Selector.Get("keyCode");
        private MethodInfo methodKeyDown;
        private MethodInfo methodKeyUp;
        private MethodInfo methodInvalidateCursorRects;

        private object nativeWindow;

        public MacOSGameWindow()
        {
            Load += OnLoad;
            UpdateFrame += refreshCursorState;
        }

        protected void OnLoad(object sender, EventArgs e)
        {
            try
            {
                flagsChangedHandler = flagsChanged;

                var fieldImplementation = typeof(OpenTK.NativeWindow).GetRuntimeFields().Single(x => x.Name == "implementation");
                var typeCocoaNativeWindow = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "CocoaNativeWindow");
                var fieldWindowClass = typeCocoaNativeWindow.GetRuntimeFields().Single(x => x.Name == "windowClass");

                nativeWindow = fieldImplementation.GetValue(Implementation);
                var windowClass = (IntPtr)fieldWindowClass.GetValue(nativeWindow);

                Class.RegisterMethod(windowClass, flagsChangedHandler, "flagsChanged:", "v@:@");

                methodKeyDown = nativeWindow.GetType().GetRuntimeMethods().Single(x => x.Name == "OnKeyDown");
                methodKeyUp = nativeWindow.GetType().GetRuntimeMethods().Single(x => x.Name == "OnKeyUp");
                methodInvalidateCursorRects = nativeWindow.GetType().GetRuntimeMethods().Single(x => x.Name == "InvalidateCursorRects");
            }
            catch
            {
                Logger.Log("Window initialisation couldn't complete, likely due to the SDL backend being enabled.", LoggingTarget.Runtime, LogLevel.Important);
                Logger.Log("Execution will continue but keyboard functionality may be limited.", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

        private void refreshCursorState(object sender, OpenTK.FrameEventArgs e)
        {
            // If the cursor should be hidden, but something in the system has made it appear (such as a notification),
            // invalidate the cursor rects to hide it.  OpenTK has a private function that does this.
            if (CursorState.HasFlag(CursorState.Hidden) && Cocoa.CGCursorIsVisible())
                methodInvalidateCursorRects.Invoke(nativeWindow, new object[0]);
        }

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
}
