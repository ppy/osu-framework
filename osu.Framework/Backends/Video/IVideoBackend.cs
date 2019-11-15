// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Graphics.Video;
using osu.Framework.Threading;

namespace osu.Framework.Backends.Video
{
    /// <summary>
    /// Interface for an <see cref="IBackend"/> that creates video decoders.
    /// </summary>
    public interface IVideoBackend : IBackend
    {
        /// <summary>
        /// Creates a <see cref="VideoDecoder"/> using the given <see cref="Stream"/> and <see cref="Scheduler"/>.
        /// </summary>
        /// <param name="stream">The stream source of the video</param>
        /// <param name="scheduler">The scheduler to use when updating the decoder state</param>
        VideoDecoder CreateVideoDecoder(Stream stream, Scheduler scheduler);
    }
}
