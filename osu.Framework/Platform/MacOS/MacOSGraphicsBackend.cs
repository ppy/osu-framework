// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform.MacOS.Native;
using osu.Framework.Platform.Sdl;
using SDL2;

namespace osu.Framework.Platform.MacOS
{
    /// <summary>
    /// A macOS-specific subclass of <see cref="Sdl2GraphicsBackend"/> that implements a minor fix to <see cref="SwapBuffers"/>.
    /// </summary>
    public class MacOSGraphicsBackend : Sdl2GraphicsBackend
    {
        private static readonly IntPtr sel_flushbuffer = Selector.Get("flushBuffer");

        /// <summary>
        /// There is a rare case where <see cref="SDL.SDL_GL_SwapWindow"/> can cause a crash due to
        /// SDL attempting to call [NSOpenGLContext update] on a thread other than the main thread.
        /// Instead we forcefully call [NSOpenGLContext flushBuffer].
        /// </summary>
        public override void SwapBuffers() => Cocoa.SendVoid(Context, sel_flushbuffer);
    }
}
