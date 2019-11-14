// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Backends.Video;
using osu.Framework.Graphics.Video;
using osu.Framework.Android.Graphics.Video;
using osu.Framework.Threading;

namespace osu.Framework.iOS.Backends.Video
{
    public class AndroidVideoBackend : VideoBackend
    {
        public override VideoDecoder CreateVideoDecoder(Stream stream, Scheduler scheduler) => new AndroidVideoDecoder(stream, scheduler);
    }
}
