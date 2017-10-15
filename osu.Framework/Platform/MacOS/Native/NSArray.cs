// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal struct NSArray
    {
        internal IntPtr Handle { get; private set; }

        private static IntPtr classPointer = Class.Get("NSArray");
        private static IntPtr mutableClassPointer = Class.Get("NSMutableArray");
        private static IntPtr selArrayWithObject = Selector.Get("arrayWithObject:");
        private static IntPtr selArray = Selector.Get("array");
        private static IntPtr selAddObject = Selector.Get("addObject:");
        private static IntPtr selCount = Selector.Get("count");
        private static IntPtr selObjectAtIndex = Selector.Get("objectAtIndex:");

        internal NSArray(IntPtr handle)
        {
            Handle = handle;
        }

        internal static NSArray ArrayWithObject(IntPtr obj) => new NSArray(Cocoa.SendIntPtr(classPointer, selArrayWithObject, obj));

        internal static NSArray ArrayWithObjects(IntPtr[] objs)
        {
            var mutableArray = Cocoa.SendIntPtr(mutableClassPointer, selArray);
            foreach (IntPtr obj in objs)
                Cocoa.SendVoid(mutableArray, selAddObject, obj);
            return new NSArray(mutableArray);
        }

        internal int Count() => Cocoa.SendInt(Handle, selCount);

        internal IntPtr ObjectAtIndex(int index) => Cocoa.SendIntPtr(Handle, selObjectAtIndex, index);

        internal IntPtr[] ToArray()
        {
            IntPtr[] result = new IntPtr[Count()];
            for (int i = 0; i < result.Length; i++)
                result[i] = ObjectAtIndex(i);
            return result;
        }
    }
}
