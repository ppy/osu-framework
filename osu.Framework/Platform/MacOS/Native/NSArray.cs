// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSArray
    {
        internal IntPtr Handle { get; }

        private static readonly IntPtr class_pointer = Class.Get("NSArray");
        private static readonly IntPtr mutable_class_pointer = Class.Get("NSMutableArray");
        private static readonly IntPtr sel_array_with_object = Selector.Get("arrayWithObject:");
        private static readonly IntPtr sel_array = Selector.Get("array");
        private static readonly IntPtr sel_add_object = Selector.Get("addObject:");
        private static readonly IntPtr sel_count = Selector.Get("count");
        private static readonly IntPtr sel_object_at_index = Selector.Get("objectAtIndex:");

        internal NSArray(IntPtr handle)
        {
            Handle = handle;
        }

        internal static NSArray ArrayWithObject(IntPtr obj) => new NSArray(Cocoa.SendIntPtr(class_pointer, sel_array_with_object, obj));

        internal static NSArray ArrayWithObjects(IntPtr[] objs)
        {
            var mutableArray = Cocoa.SendIntPtr(mutable_class_pointer, sel_array);
            foreach (IntPtr obj in objs)
                Cocoa.SendVoid(mutableArray, sel_add_object, obj);
            return new NSArray(mutableArray);
        }

        internal int Count() => Cocoa.SendInt(Handle, sel_count);

        internal IntPtr ObjectAtIndex(int index) => Cocoa.SendIntPtr(Handle, sel_object_at_index, index);

        internal IntPtr[] ToArray()
        {
            IntPtr[] result = new IntPtr[Count()];
            for (int i = 0; i < result.Length; i++)
                result[i] = ObjectAtIndex(i);
            return result;
        }
    }
}
