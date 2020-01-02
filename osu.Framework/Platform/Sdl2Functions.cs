// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid.Sdl2;

// ReSharper disable InconsistentNaming

namespace osu.Framework.Platform
{
    public static unsafe class Sdl2Functions
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void SDL_GL_GetDrawableSize_t(SDL_Window window, int* w, int* h);

        private static readonly SDL_GL_GetDrawableSize_t s_glGetDrawableSize = Sdl2Native.LoadFunction<SDL_GL_GetDrawableSize_t>("SDL_GL_GetDrawableSize");

        public static Vector2 SDL_GL_GetDrawableSize(SDL_Window window)
        {
            int w, h;
            s_glGetDrawableSize(window, &w, &h);
            return new Vector2(w, h);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int SDL_GL_GetSwapInterval_t();

        private static readonly SDL_GL_GetSwapInterval_t s_gl_getSwapInterval = Sdl2Native.LoadFunction<SDL_GL_GetSwapInterval_t>("SDL_GL_GetSwapInterval");

        public static int SDL_GL_GetSwapInterval() => s_gl_getSwapInterval();
    }
}
