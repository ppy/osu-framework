// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Audio.Sample;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleBassInitTest
    {
        private BassTestComponents bass;
        private Sample sample;

        [SetUp]
        public void Setup()
        {
            bass = new BassTestComponents(false);
            sample = bass.GetSample();

            bass.Update();
            bass.Init();
        }

        [TearDown]
        public void Teardown()
        {
            bass?.Dispose();
        }

        [Test]
        public void TestSampleInitialisesOnUpdateDevice()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
                Assert.Ignore("Test may be intermittent on linux (see AudioThread.FreeDevice()).");

            Assert.That(sample.IsLoaded, Is.False);
            bass.RunOnAudioThread(() => bass.SampleStore.UpdateDevice(0));
            Assert.That(sample.IsLoaded, Is.True);
        }
    }
}
