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
        private Sample sample;
        private SampleChannel channel;

        [Resolved]
        private AudioManager audioManager { get; set; }

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddUntilStep("audio device ready", () => audioManager.IsLoaded);
            AddStep("create looping sample", () =>
            {
                channel?.Dispose();
                sample = audioManager.Samples.Get("tone.wav");
            });
        }

        [Test]
        public void TestDisableLoopingFlag()
        {
            playAndCheckSample();

            AddStep("disable looping", () => channel.Looping = false);
            AddUntilStep("ensure stops", () => !channel.Playing);
        }

        [Test]
        public void TestZeroFrequency()
        {
            playAndCheckSample();

            AddStep("set frequency to 0", () => channel.Frequency.Value = 0);
            AddAssert("is still playing", () => channel.Playing);
        }

        [Test]
        public void TestZeroFrequencyOnStart()
        {
            AddStep("set frequency to 0", () => channel.Frequency.Value = 0);
            playAndCheckSample();

            AddStep("set frequency to 1", () => channel.Frequency.Value = 1);
            AddAssert("is still playing", () => channel.Playing);
        }

        [Test]
        public void TestZeroFrequencyAfterStop()
        {
            stopAndCheckSample();

            AddStep("set frequency to 0", () => channel.Frequency.Value = 0);
            AddAssert("still stopped", () => !channel.Playing);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            stopAndCheckSample();
        }

        private void playAndCheckSample()
        {
            AddStep("play sample", () =>
            {
                channel = sample.Play();

                // reduce volume of the tone due to how loud it normally is.
                channel.Volume.Value = 0.05;
                channel.Looping = true;
            });

            // ensures that it is in fact looping given that the loaded sample length is very short.
            AddWaitStep("wait", 10);
            AddAssert("is playing", () => channel.Playing);
        }

        private void stopAndCheckSample()
        {
            AddStep("stop playing", () => channel.Stop());
            AddUntilStep("stopped", () => !channel.Playing);
        }

        protected override void Dispose(bool isDisposing)
        {
            channel?.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
