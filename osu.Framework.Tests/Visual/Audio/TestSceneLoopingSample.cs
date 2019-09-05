// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

            AddWaitStep("wait", 1);
            AddAssert("is playing", () => sampleChannel.Playing);

            stopSample();
        }

        [Test, Ignore("Needs no audio device support")]
        public void TestStopWhileLooping()
        {
            AddStep("create sample", createSample);

            playSample();

            stopSample();
        }

        [Test, Ignore("Needs no audio device support")]
        public void TestZeroFrequency()
        {
            AddStep("create sample", createSample);

            playSample();

            setFrequency(0);
            setFrequency(1);

            stopSample();
        }

        [Test, Ignore("Needs no audio device support")]
        public void TestZeroFrequencyOnStart()
        {
            AddStep("create sample", createSample);
            setFrequency(0);

            playSample();

            setFrequency(1);

            stopSample();
        }

        private void playSample()
        {
            AddStep("enable looping", () => sampleChannel.Looping = true);
            AddStep("play sample", () => sampleChannel.Play());
            AddAssert("is playing", () => sampleChannel.Playing);
        }

        private void stopSample()
        {
            AddStep("stop playing", () => sampleChannel.Stop());
            AddAssert("not playing", () => !sampleChannel.Playing);
        }

        private void setFrequency(double freq)
        {
            AddStep($"set frequency to {freq}", () => sampleChannel.Frequency.Value = freq);
            AddAssert("is playing", () => sampleChannel.Playing);
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
