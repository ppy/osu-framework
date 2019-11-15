// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Backends.Audio;
using osu.Framework.Backends.Audio.Bass;
using osu.Framework.Backends.Video;
using osu.Framework.Backends.Video.Ffmpeg;

namespace osu.Framework.Backends
{
    /// <summary>
    /// Implementation of <see cref="IBackendProvider"/> for desktop platforms.
    /// </summary>
    public class DesktopBackendProvider : IBackendProvider
    {
        public IAudioBackend CreateAudio() => new BassAudioBackend();

        public IVideoBackend CreateVideo() => new FfmpegVideoBackend();
    }
}
