// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Linux.Sdl
{
    public class SdlClipboard : Clipboard
    {
        #if ANDROID
        const string lib = "libSDL2.so";
        #elif IPHONE
        const string lib = "__Internal";
        #else
        private const string lib = "libSDL2-2.0.so.0";
        #endif

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_GetClipboardText", ExactSpelling = true)]
        internal static extern string SDL_GetClipboardText();

        [DllImport(lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "SDL_SetClipboardText", ExactSpelling = true)]
        internal static extern int SDL_SetClipboardText(string text);

        public override string GetText()
        {
            return SDL_GetClipboardText();
        }

        public override void SetText(string selectedText)
        {
            SDL_SetClipboardText(selectedText);
        }
    }
}
