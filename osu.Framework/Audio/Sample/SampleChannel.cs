// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio.Sample
{
    public abstract class SampleChannel : AdjustableAudioComponent, ISampleChannel
    {
        protected bool WasStarted;

        protected Sample Sample { get; set; }

        private readonly Action<SampleChannel> onPlay;

        protected SampleChannel(Sample sample, Action<SampleChannel> onPlay)
        {
            Sample = sample ?? throw new ArgumentNullException(nameof(sample));
            this.onPlay = onPlay;
        }

        public virtual void Play(bool restart = true)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not play disposed samples.");

            onPlay(this);
            WasStarted = true;
        }

        public virtual void Stop()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not stop disposed samples.");
        }

        protected override void Dispose(bool disposing)
        {
            Stop();
            base.Dispose(disposing);
        }

        protected override void UpdateState()
        {
            FrameStatistics.Increment(StatisticsCounterType.SChannels);
            base.UpdateState();
        }

        public abstract bool Playing { get; }

        public virtual bool Played => WasStarted && !Playing;

        public override bool IsAlive => base.IsAlive && !Played;

        public virtual ChannelAmplitudes CurrentAmplitudes { get; } = ChannelAmplitudes.Empty;
    }
}
