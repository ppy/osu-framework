// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using SDL2;

namespace osu.Framework.Platform.SDL2
{
    /// <summary>
    /// Implementation of <see cref="PassthroughGraphicsBackend"/> that uses SDL's OpenGL bindings.
    /// </summary>
    public class SDL2GraphicsBackend : PassthroughGraphicsBackend
    {
        private IntPtr sdlWindowHandle;

        public override bool VerticalSync
        {
            get => SDL.SDL_GL_GetSwapInterval() != 0;
            set => SDL.SDL_GL_SetSwapInterval(value ? 1 : 0);
        }

        protected override IntPtr CreateContext()
        {
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, SDL.SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_COMPATIBILITY);

            IntPtr context = SDL.SDL_GL_CreateContext(sdlWindowHandle);
            if (context == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to create an SDL2 GL context ({SDL.SDL_GetError()})");

            return context;
        }

        protected override void MakeCurrent(IntPtr context)
        {
            int result = SDL.SDL_GL_MakeCurrent(sdlWindowHandle, context);
            if (result < 0)
                throw new InvalidOperationException($"Failed to acquire GL context ({SDL.SDL_GetError()})");
        }

        public override void SwapBuffers() => SDL.SDL_GL_SwapWindow(sdlWindowHandle);

        protected override IntPtr GetProcAddress(string symbol)
        {
            const int error_category = (int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_ERROR;
            SDL.SDL_LogPriority oldPriority = SDL.SDL_LogGetPriority(error_category);

            // Prevent logging calls to SDL_GL_GetProcAddress() that fail on systems which don't have the requested symbol (typically macOS).
            SDL.SDL_LogSetPriority(error_category, SDL.SDL_LogPriority.SDL_LOG_PRIORITY_INFO);

            IntPtr ret = SDL.SDL_GL_GetProcAddress(symbol);

            // Reset the logging behaviour.
            SDL.SDL_LogSetPriority(error_category, oldPriority);

            return ret;
        }

        public override void InitialiseBeforeWindowCreation()
        {
            SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_STENCIL_SIZE, 8);
        }

        public override void Initialise(IWindow window)
        {
            if (!(window is SDL2DesktopWindow sdlWindow))
                throw new ArgumentException("Unsupported window backend.", nameof(window));

            sdlWindowHandle = sdlWindow.SDLWindowHandle;
            base.Initialise(window);
        }
    }
}
