// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Backends.Video;
using osu.Framework.Graphics.Video;
using osu.Framework.iOS.Graphics.Video;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.iOS.Backends.Video
{
    /// <summary>
    /// An <see cref="IVideoBackend"/> that creates video decoders using FFmpeg for iOS.
    /// </summary>
    public class IOSVideoBackend : IVideoBackend
    {
        public VideoDecoder CreateVideoDecoder(Stream stream, Scheduler scheduler) => new IOSVideoDecoder(stream, scheduler);

        public void Dispose()
        {
        }

        public void Initialise(IGameHost host)
        {
        }
    }
}
