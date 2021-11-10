// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSDictionary
    {
        internal IntPtr Handle { get; }

        private static IntPtr classPointer = Class.Get("NSDictionary");

        internal NSDictionary(IntPtr handle)
        {
            Handle = handle;
        }
    }
}
