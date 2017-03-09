// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Statistics;
using System;

namespace osu.Framework.Audio.Sample
{
    public abstract class SampleChannel : AdjustableAudioComponent
    {
        protected bool WasStarted;

        protected Sample Sample { get; set; }

        private Action<SampleChannel> onPlay;

        protected SampleChannel(Sample sample, Action<SampleChannel> onPlay)
        {
            if (sample == null)
                throw new ArgumentNullException(nameof(sample));
            Sample = sample;
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

        public override void Update()
        {
            FrameStatistics.Increment(StatisticsCounterType.SChannels);
            base.Update();
        }

        public abstract bool Playing { get; }

        public virtual bool Played => WasStarted && !Playing;

        public override bool HasCompleted => base.HasCompleted || Played;
    }
}
