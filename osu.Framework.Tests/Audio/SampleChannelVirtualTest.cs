// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Audio.Sample;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleChannelVirtualTest
    {
        private Sample sample;

        [SetUp]
        public void Setup()
        {
            sample = new SampleVirtual("virtual");
            updateSample();
        }

        [Test]
        public void TestStart()
        {
            var channel = sample.Play();
            Assert.IsTrue(channel.Playing);
            Assert.IsFalse(channel.HasCompleted);

            updateSample();

            Assert.IsFalse(channel.Playing);
            Assert.IsTrue(channel.HasCompleted);
        }

        [Test]
        public void TestLooping()
        {
            var channel = sample.Play();
            channel.Looping = true;
            Assert.IsTrue(channel.Playing);
            Assert.IsFalse(channel.HasCompleted);

            updateSample();

            Assert.IsTrue(channel.Playing);
            Assert.False(channel.HasCompleted);

            channel.Stop();

            Assert.False(channel.Playing);
            Assert.IsTrue(channel.HasCompleted);
        }

        private void updateSample() => AudioTestHelper.RunOnAudioThread(() => sample.Update());
    }
}
