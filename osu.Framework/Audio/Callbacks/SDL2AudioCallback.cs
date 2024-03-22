// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.CompilerServices;
using osu.Framework.Allocation;
using osu.Framework.Platform;
using SDL2;

namespace osu.Framework.Audio.Callbacks
{
    internal class SDL2AudioCallback : BassCallback
    {
        public SDL.SDL_AudioCallback Callback => RuntimeFeature.IsDynamicCodeSupported ? AudioCallback : audioCallback;

        public readonly SDL.SDL_AudioCallback AudioCallback;

        public SDL2AudioCallback(SDL.SDL_AudioCallback callback)
        {
            AudioCallback = callback;
        }

        [MonoPInvokeCallback(typeof(SDL.SDL_AudioCallback))]
        private static void audioCallback(IntPtr userdata, IntPtr stream, int len)
        {
            var ptr = new ObjectHandle<SDL2AudioCallback>(userdata);
            if (ptr.GetTarget(out var target))
                target.AudioCallback(userdata, stream, len);
        }
    }
}
