// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace osu.Framework.iOS
{
    [Register("GameUIApplication")]
    public class GameUIApplication : UIApplication
    {
        private const int gsevent_type = 2;
        private const int gsevent_flags = 10;

        private const int gsevent_keycode = 13;

        private const int gsevent_type_keydown = 10;
        private const int gsevent_type_keyup = 11;
        private const int gsevent_type_modifier = 12;

        public delegate void KeyHandler(int keyCode, bool isDown);

        /// <summary>
        /// Occurs when key event.
        /// </summary>
        public event KeyHandler KeyEvent;

        // https://github.com/xamarin/xamarin-macios/blob/master/src/ObjCRuntime/Messaging.iOS.cs
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSendSuper")]
        private static extern void send_super(IntPtr receiver, IntPtr selector, IntPtr arg1);

        private int lastEventFlags;

        private unsafe bool decodeKeyEvent(NSObject eventMem)
        {
            if (eventMem == null) return false;

            var eventPtr = (IntPtr*)eventMem.Handle.ToPointer();

            int eventType = (int)eventPtr[gsevent_type];
            int eventModifier = (int)eventPtr[gsevent_flags];
            int eventScanCode = (int)eventPtr[gsevent_keycode];
            int eventLastModifier = lastEventFlags;

            static bool isBlockKey(int keyCode)
                => keyCode == 79 || // Right
                   keyCode == 80 || // Left
                   keyCode == 81 || // Down
                   keyCode == 82; // Up

            switch (eventType)
            {
                case gsevent_type_keydown:
                case gsevent_type_keyup:
                    KeyEvent?.Invoke(eventScanCode, eventType == gsevent_type_keydown);
                    if (isBlockKey(eventScanCode))
                        return true;

                    break;

                case gsevent_type_modifier:
                    KeyEvent?.Invoke(eventScanCode, eventModifier != 0 && eventModifier > eventLastModifier);
                    lastEventFlags = eventModifier;
                    break;
            }

            return false;
        }

        private readonly Selector gsSelector = new Selector("_gsEvent");
        private readonly Selector handleSelector = new Selector("handleKeyUIEvent:");

        [Export("handleKeyUIEvent:")]
        private void handleKeyUIEvent(UIEvent evt)
        {
            if (evt.RespondsToSelector(gsSelector) && decodeKeyEvent(evt.PerformSelector(gsSelector)))
                return;

            send_super(SuperHandle, handleSelector.Handle, evt.Handle);
        }
    }
}
