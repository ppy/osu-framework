// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Platform.MacOS.Native
{
    internal struct NSDictionary
    {
        internal IntPtr Handle { get; private set; }

        private static IntPtr classPointer = Class.Get("NSDictionary");

        internal NSDictionary(IntPtr handle)
        {
            Handle = handle;
        }
    }
}
