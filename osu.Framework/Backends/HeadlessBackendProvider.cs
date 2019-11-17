// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Backends.Audio;
using osu.Framework.Backends.Video;

namespace osu.Framework.Backends
{
    /// <summary>
    /// Implementation of <see cref="IBackendProvider"/> for headless testing.
    /// </summary>
    public class HeadlessBackendProvider : IBackendProvider
    {
        public IAudioBackend CreateAudioBackend() => new HeadlessAudioBackend();

        public IVideoBackend CreateVideoBackend() => new HeadlessVideoBackend();
    }
}
