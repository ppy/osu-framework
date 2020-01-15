// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Platform
{
    public class SDLWindow : Window
    {
        /// <summary>
        /// Convenience constructor that uses <see cref="Sdl2WindowBackend"/> and
        /// <see cref="PassthroughGraphicsBackend"/>.
        /// </summary>
        public SDLWindow()
            : base(new Sdl2WindowBackend(), new PassthroughGraphicsBackend())
        {
        }
    }
}
