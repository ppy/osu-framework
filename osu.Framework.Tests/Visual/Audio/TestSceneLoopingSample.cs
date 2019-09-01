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
            Steps.AddStep("create sample", createSample);
            Steps.AddAssert("not looping", () => !sampleChannel.Looping);

            Steps.AddStep("enable looping", () => sampleChannel.Looping = true);
            Steps.AddStep("play sample", () => sampleChannel.Play());
            Steps.AddAssert("is playing", () => sampleChannel.Playing);

            Steps.AddWaitStep("wait", 1);
            Steps.AddAssert("is still playing", () => sampleChannel.Playing);

            Steps.AddStep("disable looping", () => sampleChannel.Looping = false);
            Steps.AddUntilStep("ensure stops", () => !sampleChannel.Playing);
        }

        [Test, Ignore("Needs no audio device support")]
        public void TestStopWhileLooping()
        {
            Steps.AddStep("create sample", createSample);

            Steps.AddStep("enable looping", () => sampleChannel.Looping = true);
            Steps.AddStep("play sample", () => sampleChannel.Play());

            Steps.AddWaitStep("wait", 1);
            Steps.AddAssert("is playing", () => sampleChannel.Playing);

            Steps.AddStep("stop playing", () => sampleChannel.Stop());
            Steps.AddAssert("not playing", () => !sampleChannel.Playing);
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
