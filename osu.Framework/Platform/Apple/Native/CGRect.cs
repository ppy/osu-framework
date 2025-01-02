// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CGRect
    {
        internal CGPoint Origin;
        internal CGSize Size;
    }
}
