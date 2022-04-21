// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSDictionary
    {
        internal IntPtr Handle { get; }

#pragma warning disable IDE0052 // Unread private member
        private static IntPtr classPointer = Class.Get("NSDictionary");
#pragma warning restore IDE0052 //

        internal NSDictionary(IntPtr handle)
        {
            Handle = handle;
        }
    }
}
