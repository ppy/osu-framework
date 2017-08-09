// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

using osu.Framework.Desktop.Platform.MacOS.Native;

namespace osu.Framework.Desktop.Platform.MacOS
{
    internal class MacOSGameWindow : DesktopGameWindow
    {
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate IntPtr DraggingEnteredDelegate(IntPtr self, IntPtr cmd, IntPtr sender);

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate bool PerformDragOperationDelegate(IntPtr self, IntPtr cmd, IntPtr sender);

        private DraggingEnteredDelegate DraggingEnteredHandler;
        private PerformDragOperationDelegate PerformDragOperationHandler;

        private static IntPtr NSFilenamesPboardType;

        protected override void OnLoad(EventArgs e)
        {
            PerformDragOperationHandler = PerformDragOperation;
            DraggingEnteredHandler = DraggingEntered;

            var fieldImplementation = typeof(OpenTK.NativeWindow).GetRuntimeFields().Single(x => x.Name == "implementation");
            var typeCocoaNativeWindow = typeof(OpenTK.NativeWindow).Assembly.GetTypes().Single(x => x.Name == "CocoaNativeWindow");
            var nativeWindow = fieldImplementation.GetValue(this);
            var fieldWindowClass = typeCocoaNativeWindow.GetRuntimeFields().Single(x => x.Name == "windowClass");
            NSFilenamesPboardType = Cocoa.GetStringConstant(Cocoa.AppKitLibrary, "NSFilenamesPboardType");

            var windowClass = (IntPtr)fieldWindowClass.GetValue(nativeWindow);
            Class.RegisterMethod(windowClass, DraggingEnteredHandler, "draggingEntered:", "@@:@");
            Class.RegisterMethod(windowClass, PerformDragOperationHandler, "performDragOperation:", "b@:@");

            Cocoa.SendIntPtr(this.WindowInfo.Handle,
                             Selector.Get("registerForDraggedTypes:"),
                             Cocoa.SendIntPtr(
                                 Class.Get("NSArray"),
                                 Selector.Get("arrayWithObject:"),
                                 NSFilenamesPboardType));
            base.OnLoad(e);
        }

        private IntPtr DraggingEntered(IntPtr self, IntPtr cmd, IntPtr sender)
        {
            int mask = Cocoa.SendInt(sender, Selector.Get("draggingSourceOperationMask"));
            int NSDragOperation_Generic = 4;
            int NSDragOperation_None = 0;
            if ((mask & NSDragOperation_Generic) == NSDragOperation_Generic)
            {
                return new IntPtr(NSDragOperation_Generic);
            }

            return new IntPtr(NSDragOperation_None);
        }

        private bool PerformDragOperation(IntPtr self, IntPtr cmd, IntPtr sender)
        {
            IntPtr pboard = Cocoa.SendIntPtr(sender, Selector.Get("draggingPasteboard"));

            IntPtr files = Cocoa.SendIntPtr(pboard, Selector.Get("propertyListForType:"), NSFilenamesPboardType);

            var filenames = new List<String>();
            int count = Cocoa.SendInt(files, Selector.Get("count"));
            for (int i = 0; i < count; ++i)
            {
                IntPtr obj = Cocoa.SendIntPtr(files, Selector.Get("objectAtIndex:"), new IntPtr(i));
                IntPtr str = Cocoa.SendIntPtr(obj, Selector.Get("cStringUsingEncoding:"), new IntPtr(1));
                filenames.Add(Marshal.PtrToStringAuto(str));
            }
            OnFileDrop(filenames.ToArray());
            return true;
        }

        void OnFileDrop(string[] filenames)
        {
            var data = new DataObject(DataFormats.FileDrop, filenames);
            var args = new DragEventArgs(data, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.Copy);
            this.OnDragDrop(args);
        }
    }
}
