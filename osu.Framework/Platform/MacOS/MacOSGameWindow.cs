// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using osu.Framework.Platform.MacOS.Native;

namespace osu.Framework.Platform.MacOS
{
    internal class MacOSGameWindow : DesktopGameWindow
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate void FlagsChangedDelegate(IntPtr self, IntPtr cmd, IntPtr notification);

        private FlagsChangedDelegate FlagsChangedHandler;

        private IntPtr selModifierFlags = Selector.Get("modifierFlags");
        private IntPtr selKeyCode = Selector.Get("keyCode");
        private MethodInfo methodKeyDown;
        private MethodInfo methodKeyUp;

        private int modifierFlagLeftControl = 1 << 0;
        private int modifierFlagLeftShift = 1 << 1;
        private int modifierFlagRightShift = 1 << 2;
        private int modifierFlagLeftCommand = 1 << 3;
        private int modifierFlagRightCommand = 1 << 4;
        private int modifierFlagLeftAlt = 1 << 5;
        private int modifierFlagRightAlt = 1 << 6;
        private int modifierFlagRightControl = 1 << 13;

        private object nativeWindow;

        protected override void OnLoad(EventArgs e)
        {
            FlagsChangedHandler = flagsChanged;

            var fieldImplementation = typeof(OpenTK.NativeWindow).GetRuntimeFields().Single(x => x.Name == "implementation");
            var typeCocoaNativeWindow = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "CocoaNativeWindow");
            var fieldWindowClass = typeCocoaNativeWindow.GetRuntimeFields().Single(x => x.Name == "windowClass");

            nativeWindow = fieldImplementation.GetValue(this);
            var windowClass = (IntPtr)fieldWindowClass.GetValue(nativeWindow);

            Class.RegisterMethod(windowClass, FlagsChangedHandler, "flagsChanged:", "v@:@");

            methodKeyDown = nativeWindow.GetType().GetRuntimeMethods().Single(x => x.Name == "OnKeyDown");
            methodKeyUp = nativeWindow.GetType().GetRuntimeMethods().Single(x => x.Name == "OnKeyUp");

            base.OnLoad(e);
        }

        private void flagsChanged(IntPtr self, IntPtr cmd, IntPtr sender)
        {
            var modifierFlags = Cocoa.SendInt(sender, selModifierFlags);
            var keyCode = Cocoa.SendInt(sender, selKeyCode);

            bool keyDown = false;
            OpenTK.Input.Key key;

            switch ((MacOSKeyCodes)keyCode)
            {
                case MacOSKeyCodes.LShift:
                    key = OpenTK.Input.Key.LShift;
                    keyDown = (modifierFlags & modifierFlagLeftShift) > 0;
                    break;

                case MacOSKeyCodes.RShift:
                    key = OpenTK.Input.Key.RShift;
                    keyDown = (modifierFlags & modifierFlagRightShift) > 0;
                    break;

                case MacOSKeyCodes.LControl:
                    key = OpenTK.Input.Key.LControl;
                    keyDown = (modifierFlags & modifierFlagLeftControl) > 0;
                    break;

                case MacOSKeyCodes.RControl:
                    key = OpenTK.Input.Key.RControl;
                    keyDown = (modifierFlags & modifierFlagRightControl) > 0;
                    break;
                
                case MacOSKeyCodes.LAlt:
                    key = OpenTK.Input.Key.LAlt;
                    keyDown = (modifierFlags & modifierFlagLeftAlt) > 0;
                    break;

                case MacOSKeyCodes.RAlt:
                    key = OpenTK.Input.Key.RAlt;
                    keyDown = (modifierFlags & modifierFlagRightAlt) > 0;
                    break;

                case MacOSKeyCodes.LCommand:
                    key = OpenTK.Input.Key.LWin;
                    keyDown = (modifierFlags & modifierFlagLeftCommand) > 0;
                    break;

                case MacOSKeyCodes.RCommand:
                    key = OpenTK.Input.Key.RWin;
                    keyDown = (modifierFlags & modifierFlagRightCommand) > 0;
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
