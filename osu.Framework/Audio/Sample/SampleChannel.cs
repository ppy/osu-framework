// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio.Sample
{
    public abstract class SampleChannel : AdjustableAudioComponent, ISampleChannel
    {
        public virtual void Stop()
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed)
                Stop();

            base.Dispose(disposing);
        }

        protected override void UpdateState()
        {
            FrameStatistics.Increment(StatisticsCounterType.SChannels);
            base.UpdateState();
        }

        public abstract bool Playing { get; }

        public override bool IsAlive => base.IsAlive && Playing;

        public virtual ChannelAmplitudes CurrentAmplitudes { get; } = ChannelAmplitudes.Empty;
    }
}
