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

        private const int modifier_flag_left_control = 1 << 0;
        private const int modifier_flag_left_shift = 1 << 1;
        private const int modifier_flag_right_shift = 1 << 2;
        private const int modifier_flag_left_command = 1 << 3;
        private const int modifier_flag_right_command = 1 << 4;
        private const int modifier_flag_left_alt = 1 << 5;
        private const int modifier_flag_right_alt = 1 << 6;
        private const int modifier_flag_right_control = 1 << 13;

        private object nativeWindow;

        public MacOSGameWindow()
        {
            Load += OnLoad;
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
            }
            catch
            {
                Logger.Log("Window initialisation couldn't complete, likely due to the SDL backend being enabled.", LoggingTarget.Runtime, LogLevel.Important);
                Logger.Log("Execution will continue but keyboard functionality may be limited.", LoggingTarget.Runtime, LogLevel.Important);
            }
        }

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
