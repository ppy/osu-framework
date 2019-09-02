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

            AddStep("enable looping", () => sampleChannel.Looping = true);
            AddStep("play sample", () => sampleChannel.Play());
            AddAssert("is playing", () => sampleChannel.Playing);

            AddWaitStep("wait", 1);
            AddAssert("is still playing", () => sampleChannel.Playing);

            AddStep("disable looping", () => sampleChannel.Looping = false);
            AddUntilStep("ensure stops", () => !sampleChannel.Playing);
        }

        [Test, Ignore("Needs no audio device support")]
        public void TestStopWhileLooping()
        {
            AddStep("create sample", createSample);

            AddStep("enable looping", () => sampleChannel.Looping = true);
            AddStep("play sample", () => sampleChannel.Play());

            AddWaitStep("wait", 1);
            AddAssert("is playing", () => sampleChannel.Playing);

            AddStep("stop playing", () => sampleChannel.Stop());
            AddAssert("not playing", () => !sampleChannel.Playing);
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
