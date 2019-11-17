// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Backends;
using osu.Framework.Backends.Audio;
using osu.Framework.Backends.Audio.Bass;
using osu.Framework.Backends.Video;
using osu.Framework.iOS.Backends.Video;

namespace osu.Framework.iOS.Backends
{
    /// <summary>
    /// Implementation of <see cref="IBackendProvider"/> for iOS.
    /// </summary>
    public class IOSBackendProvider : IBackendProvider
    {
        public IAudioBackend CreateAudioBackend() => new BassAudioBackend();

        public IVideoBackend CreateVideoBackend() => new IOSVideoBackend();
    }
}
