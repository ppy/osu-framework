// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows.Native
{
    internal static class Window
    {
        [DllImport("user32.dll")]
        internal static extern uint InvalidateRect(IntPtr hwnd, IntPtr lpRect, bool bErase);
    }
}
