// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal readonly struct NSDictionary
    {
        internal IntPtr Handle { get; }

        internal NSDictionary(IntPtr handle)
        {
            Handle = handle;
        }
    }
}
