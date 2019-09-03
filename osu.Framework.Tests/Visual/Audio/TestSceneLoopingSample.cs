// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneLoopingSample : FrameworkTestScene
    {
        private SampleChannel sampleChannel;
        private ISampleStore samples;

        [BackgroundDependencyLoader]
        private void load(ISampleStore samples)
        {
            this.samples = samples;
        }

        [Test, Ignore("Needs no audio device support")]
        public void TestLoopingToggle()
        {
            AddStep("create sample", createSample);
            AddAssert("not looping", () => !sampleChannel.Looping);

            playSample();
            checkChannelState(true);

            AddWaitStep("wait", 1);
            checkChannelState(true);

            stopSample();
        }

        [Test, Ignore("Needs no audio device support")]
        public void TestStopWhileLooping()
        {
            AddStep("create sample", createSample);

            playSample();
            checkChannelState(true);

            stopSample();
        }

        [Test, Ignore("Needs no audio device support")]
        public void TestZeroFrequency()
        {
            AddStep("create sample", createSample);

            playSample();
            checkChannelState(true);

            AddStep("set frequency to 0", () => sampleChannel.Frequency.Value = 0);
            checkChannelState(true, PlaybackState.Paused);

            AddStep("set frequency to 1", () => sampleChannel.Frequency.Value = 1);
            checkChannelState(true);

            stopSample();
        }

        [Test, Ignore("Needs no audio device support")]
        public void TestZeroFrequencyOnStart()
        {
            AddStep("create sample", createSample);
            AddStep("set frequency to 0", () => sampleChannel.Frequency.Value = 0);

            playSample();
            checkChannelState(true, PlaybackState.Paused);

            AddStep("set frequency to 1", () => sampleChannel.Frequency.Value = 1);
            checkChannelState(true);

            stopSample();
        }

        private void playSample()
        {
            AddStep("enable looping", () => sampleChannel.Looping = true);
            AddStep("play sample", () => sampleChannel.Play());
        }

        private void stopSample()
        {
            AddStep("stop playing", () => sampleChannel.Stop());
            AddAssert("not playing", () => !sampleChannel.Playing);
        }

        private void checkChannelState(bool isPlaying, PlaybackState channelState = PlaybackState.Playing)
        {
            AddAssert("is playing", () => sampleChannel.Playing == isPlaying);
            AddAssert($"is channel {channelState.ToString().ToLowerInvariant()}", () => (sampleChannel as SampleChannelBass)?.ChannelState == channelState);
        }

        private void createSample()
        {
            sampleChannel?.Dispose();
            sampleChannel = samples.Get("tone.wav");

            // reduce volume of the tone due to how loud it normally is.
            if (sampleChannel != null)
                sampleChannel.Volume.Value = 0.05;
        }

        protected override void Dispose(bool isDisposing)
        {
            sampleChannel?.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
