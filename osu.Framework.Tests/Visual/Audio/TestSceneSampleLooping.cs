// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
                sample?.Dispose();
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
            AddWaitStep("wait for audio thread", 3);
            AddAssert("is still playing", () => channel.Playing);
        }

        [Test]
        public void TestZeroFrequencyOnStart()
        {
            AddStep("set frequency to 0", () => sample.Frequency.Value = 0);
            playAndCheckSample();

            AddStep("set frequency to 1", () => channel.Frequency.Value = 1);
            AddWaitStep("wait for audio thread", 3);
            AddAssert("is still playing", () => channel.Playing);
        }

        [Test]
        public void TestZeroFrequencyAfterStop()
        {
            stopAndCheckSample();

            AddStep("set frequency to 0", () => channel.Frequency.Value = 0);
            AddWaitStep("wait for audio thread", 3);
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
                channel = sample.GetChannel();
                channel.Looping = true;

                // reduce volume of the tone due to how loud it normally is.
                channel.Volume.Value = 0.05;
                channel.Play();
            });

            // ensures that it is in fact looping given that the loaded sample length is very short.
            AddWaitStep("wait", 10);
            AddAssert("is playing", () => channel.Playing);
        }

        private void stopAndCheckSample()
        {
            AddStep("stop playing", () => channel?.Stop());
            AddUntilStep("stopped", () => channel?.Playing != true);
        }

        protected override void Dispose(bool isDisposing)
        {
            sample?.Dispose();
            base.Dispose(isDisposing);
        }
    }
}
