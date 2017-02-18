// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Statistics;
using System;
using System.Diagnostics;

namespace osu.Framework.Audio.Sample
{
    public abstract class SampleChannel : AdjustableAudioComponent
    {
        protected bool WasStarted;

        public Sample Sample { get; protected set; }

        private Action<SampleChannel> onPlay;

        public SampleChannel(Sample sample, Action<SampleChannel> onPlay)
        {
            Debug.Assert(sample != null, "Can not use a null sample.");
            Sample = sample;
            this.onPlay = onPlay;
        }

        public virtual void Play(bool restart = true)
        {
            Debug.Assert(!IsDisposed, "Can not play disposed samples.");

            onPlay(this);
            WasStarted = true;
        }

        public virtual void Stop()
        {
            Debug.Assert(!IsDisposed, "Can not stop disposed samples.");
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
