// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneSampleLooping : FrameworkTestScene
    {
        private SampleChannel sampleChannel;

        [Resolved]
        private AudioManager audioManager { get; set; }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddUntilStep("audio device ready", () => audioManager.IsLoaded);
            AddStep("create looping sample", createLoopingSample);
        }

        [Test]
        public void TestDisableLoopingFlag()
        {
            playAndCheckSample();

            AddStep("disable looping", () => sampleChannel.Looping = false);
            AddUntilStep("ensure stops", () => !sampleChannel.Playing);
        }

        [Test]
        public void TestZeroFrequency()
        {
            playAndCheckSample();

            AddStep("set frequency to 0", () => sampleChannel.Frequency.Value = 0);
            AddAssert("is still playing", () => sampleChannel.Playing);
        }

        [Test]
        public void TestZeroFrequencyOnStart()
        {
            AddStep("set frequency to 0", () => sampleChannel.Frequency.Value = 0);
            playAndCheckSample();

            AddStep("set frequency to 1", () => sampleChannel.Frequency.Value = 1);
            AddAssert("is still playing", () => sampleChannel.Playing);
        }

        [Test]
        public void TestZeroFrequencyAfterStop()
        {
            stopAndCheckSample();

            AddStep("set frequency to 0", () => sampleChannel.Frequency.Value = 0);
            AddAssert("still stopped", () => !sampleChannel.Playing);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            stopAndCheckSample();
        }

        private void playAndCheckSample()
        {
            AddStep("play sample", () => sampleChannel.Play());

            // ensures that it is in fact looping given that the loaded sample length is very short.
            AddWaitStep("wait", 10);
            AddAssert("is playing", () => sampleChannel.Playing);
        }

        private void stopAndCheckSample()
        {
            AddStep("stop playing", () => sampleChannel.Stop());
            AddUntilStep("stopped", () => !sampleChannel.Playing);
        }

        private void createLoopingSample()
        {
            sampleChannel?.Dispose();
            sampleChannel = audioManager.Samples.Get("tone.wav");

            // reduce volume of the tone due to how loud it normally is.
            sampleChannel.Volume.Value = 0.05;
            sampleChannel.Looping = true;
        }

        protected override void Dispose(bool isDisposing)
        {
            sampleChannel?.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
