// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Graphics.Video;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Backends.Video
{
    /// <summary>
    /// Headless implementation of <see cref="IVideoBackend"/> that can be used in non-visual tests.
    /// </summary>
    public class HeadlessVideoBackend : IVideoBackend
    {
        public VideoDecoder CreateVideoDecoder(Stream stream, Scheduler scheduler) => throw new NotImplementedException();

        public void Dispose()
        {
        }

        public void Initialise(IGameHost host)
        {
        }
    }
}
