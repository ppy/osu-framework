// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Graphics.Video;
using osu.Framework.Threading;

namespace osu.Framework.Backends.Video.Ffmpeg
{
    /// <summary>
    /// Provides an <see cref="IVideo"/> backend that creates decoders using FFmpeg.
    /// </summary>
    public class FfmpegVideoBackend : VideoBackend
    {
        public override VideoDecoder CreateVideoDecoder(Stream stream, Scheduler scheduler) => new VideoDecoder(stream, scheduler);
    }
}
