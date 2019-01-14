using Foundation;
using ObjCRuntime;
using UIKit;
using osu.Framework.Logging;
using osu.Framework.Input;
using System;
using System.Runtime.InteropServices;

namespace osu.Framework.iOS {

    [Register("GameUIApplication")]
    public class GameUIApplication : UIApplication {

        private static bool IS_IOS7 = new NSProcessInfo().IsOperatingSystemAtLeastVersion(new NSOperatingSystemVersion(7, 0, 0));
        private static bool IS_IOS9 = new NSProcessInfo().IsOperatingSystemAtLeastVersion(new NSOperatingSystemVersion(9, 0, 0));

        private static bool IS_64BIT = IntPtr.Size == 8;

        private static int GSEVENT_TYPE = 2;

        private static int GSEVENT_KEYCODE = IS_64BIT ? (IS_IOS9 ? 13 : 19) : (IS_IOS7 ? 17 : 15);

        private static int GSEVENT_TYPE_KEYDOWN = 10;
        private static int GSEVENT_TYPE_KEYUP = 11;
        private static int GSEVENT_TYPE_MODIFIER = 12;

        public delegate void KeyHandler(int keyCode, bool isDown);
        public event KeyHandler keyEvent;

        // https://github.com/xamarin/xamarin-macios/blob/master/src/ObjCRuntime/Messaging.iOS.cs
        internal const string LIBOBJC_DYLIB = "/usr/lib/libobjc.dylib";

        [DllImport(LIBOBJC_DYLIB, EntryPoint = "objc_msgSendSuper")]
        public extern static void void_objc_msgSendSuper_intptr(IntPtr receiver, IntPtr selector, IntPtr arg1);

        unsafe void decodeKeyEvent(NSObject eventMem) {
            IntPtr* eventPtr = (IntPtr*)eventMem.Handle.ToPointer();

            IntPtr eventType = eventPtr[GSEVENT_TYPE];
            IntPtr eventScanCode = eventPtr[GSEVENT_KEYCODE];

            Logger.Log(string.Format("{0} : {1}", eventType, eventScanCode));

            // General key, modifiers ignored for now
            if ((int)eventType == GSEVENT_TYPE_KEYDOWN) {
                keyEvent((int)eventScanCode, true);
            } else if ((int)eventType == GSEVENT_TYPE_KEYUP) {
                keyEvent((int)eventScanCode, false);
            }
        }

        Selector gsSelector = new Selector("_gsEvent");
        Selector handleSelector = new Selector("handleKeyUIEvent:");

        [Export("handleKeyUIEvent:")]
        void handleKeyUIEvent(UIEvent evt) {
            if (evt.RespondsToSelector(gsSelector)) {
                var eventMem = evt.PerformSelector(gsSelector);
                if (eventMem != null) {
                    decodeKeyEvent(eventMem);
                }
            }
            void_objc_msgSendSuper_intptr(this.SuperHandle, handleSelector.Handle, evt.Handle);
        }
    }
}