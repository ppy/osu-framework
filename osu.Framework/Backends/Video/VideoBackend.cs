// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Graphics.Video;
using osu.Framework.Threading;

namespace osu.Framework.Backends.Video
{
    /// <summary>
    /// Abstract implementation of <see cref="IVideo"/> that will provide any base functionality required
    /// by backend subclasses that should not be exposed via the interface.
    /// </summary>
    public abstract class VideoBackend : Backend, IVideo
    {
        public abstract VideoDecoder CreateVideoDecoder(Stream stream, Scheduler scheduler);
    }
}
