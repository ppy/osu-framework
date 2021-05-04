// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Linux.SDL2
{
    public class SDL2Clipboard : Clipboard
    {
        private const string lib = "libSDL2.so";

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_free", ExactSpelling = true)]
        internal static extern void SDL_free(IntPtr ptr);

        /// <returns>Returns the clipboard text on success or <see cref="IntPtr.Zero"/> on failure. </returns>
        [DllImport(lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_GetClipboardText", ExactSpelling = true)]
        internal static extern IntPtr SDL_GetClipboardText();

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_SetClipboardText", ExactSpelling = true)]
        internal static extern int SDL_SetClipboardText(string text);

        public override string GetText()
        {
            IntPtr ptrToText = SDL_GetClipboardText();
            string text = Marshal.PtrToStringAnsi(ptrToText);
            SDL_free(ptrToText);
            return text;
        }

        public override void SetText(string selectedText)
        {
            SDL_SetClipboardText(selectedText);
        }
    }
}
