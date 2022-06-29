// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;
using SDL2;

// ReSharper disable MemberCanBePrivate.Global
// (Some members not currently used)

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
// (Mimics SDL and SDL2-CS naming)

#pragma warning disable IDE1006 // Naming style

namespace osu.Framework.Platform.SDL2
{
    internal static class SDL2Structs
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct INTERNAL_windows_wmmsg
        {
            public IntPtr hwnd;
            public uint msg;
            public ulong wParam;
            public long lParam;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct INTERNAL_SysWMmsgUnion
        {
            [FieldOffset(0)]
            public INTERNAL_windows_wmmsg win;

            // could add more native events here if required
        }

        /// <summary>
        /// Member <c>msg</c> of <see cref="SDL.SDL_SysWMEvent"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SDL_SysWMmsg
        {
            public SDL.SDL_version version;
            public SDL.SDL_SYSWM_TYPE subsystem;
            public INTERNAL_SysWMmsgUnion msg;
        }
    }
}
