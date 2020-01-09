// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Sdl2;

namespace osu.Framework.Platform
{
    internal static unsafe class Sdl2Functions
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SdlGLGetDrawableSizeDelegate(SDL_Window window, int* w, int* h);

        private static readonly SdlGLGetDrawableSizeDelegate sdl_gl_get_drawable_size = Sdl2Native.LoadFunction<SdlGLGetDrawableSizeDelegate>("SDL_GL_GetDrawableSize");

        public static Vector2 SDL_GL_GetDrawableSize(SDL_Window window)
        {
            int w, h;
            sdl_gl_get_drawable_size(window, &w, &h);
            return new Vector2(w, h);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SdlGLGetSwapIntervalDelegate();

        private static readonly SdlGLGetSwapIntervalDelegate sdl_gl_get_swap_interval = Sdl2Native.LoadFunction<SdlGLGetSwapIntervalDelegate>("SDL_GL_GetSwapInterval");

        public static int SDL_GL_GetSwapInterval() => sdl_gl_get_swap_interval();
    }
}
