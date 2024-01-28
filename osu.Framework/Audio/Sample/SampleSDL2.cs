// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using osu.Framework.Audio.Mixing.SDL2;
using osu.Framework.Bindables;

namespace osu.Framework.Audio.Sample
{
    internal sealed class SampleSDL2 : Sample
    {
        public override bool IsLoaded => factory.IsLoaded;

        private readonly SampleSDL2Factory factory;
        private readonly SDL2AudioMixer mixer;

        public SampleSDL2(SampleSDL2Factory factory, SDL2AudioMixer mixer)
            : base(factory, factory.Name)
        {
            this.factory = factory;
            this.mixer = mixer;

            maxConcurrentCount = PlaybackConcurrency.Value;
            PlaybackConcurrency.BindValueChanged(updatePlaybackConcurrency);
        }

        private volatile int maxConcurrentCount;

        private volatile int concurrentCount;

        private void updatePlaybackConcurrency(ValueChangedEvent<int> concurrency)
        {
            maxConcurrentCount = concurrency.NewValue;
        }

        internal bool StartPlayingChannel()
        {
            if (Interlocked.Increment(ref concurrentCount) > maxConcurrentCount)
            {
                Interlocked.Decrement(ref concurrentCount);
                return false;
            }

            return true;
        }

        internal void DonePlayingChannel() => Interlocked.Decrement(ref concurrentCount);

        protected override SampleChannel CreateChannel()
        {
            var channel = new SampleChannelSDL2(this, factory.CreatePlayer());
            mixer.Add(channel);
            return channel;
        }
    }
}
