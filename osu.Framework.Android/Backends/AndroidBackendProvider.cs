// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Android.Backends.Video;
using osu.Framework.Backends;
using osu.Framework.Backends.Audio;
using osu.Framework.Backends.Audio.Bass;
using osu.Framework.Backends.Video;

namespace osu.Framework.Android.Backends
{
    /// <summary>
    /// Implementation of <see cref="IBackendProvider"/> for Android.
    /// </summary>
    public class AndroidBackendProvider : IBackendProvider
    {
        public IAudioBackend CreateAudio() => new BassAudioBackend();

        public IVideoBackend CreateVideo() => new AndroidVideoBackend();
    }
}
