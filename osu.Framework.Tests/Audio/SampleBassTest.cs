// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Audio.Sample;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleBassTest : BassTest
    {
        private SampleChannelBass sampleChannel;

        public override void Setup()
        {
            base.Setup();

            AudioComponent = sampleChannel = Manager.GetSampleStore(Store).Get("Samples.tone.wav") as SampleChannelBass;
        }

        [Test]
        public void TestLoopingRestart()
        {
            Assert.IsFalse(sampleChannel.Looping);

            sampleChannel.Looping = true;
            sampleChannel.Play();
            for (int i = 0; i < 60; i++)
                UpdateComponent();

            //WaitAudioFrame(600);
            Assert.IsTrue(sampleChannel.Looping);
            Assert.IsTrue(sampleChannel.Playing);
        }
    }
}
