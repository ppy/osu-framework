// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;

namespace osu.Framework.Backends.Audio
{
    /// <summary>
    /// Abstract implementation of <see cref="IAudio"/> that will provide any base functionality required
    /// by backend subclasses that should not be exposed via the interface.
    /// </summary>
    public abstract class AudioBackend : Backend, IAudio
    {
        #region IAudio

        public abstract Track CreateTrack(Stream data, bool quick);

        public abstract Sample CreateSample(byte[] data, ConcurrentQueue<Task> customPendingActions, int concurrency);

        public abstract SampleChannel CreateSampleChannel(Sample sample, Action<SampleChannel> onPlay);

        #endregion
    }
}
