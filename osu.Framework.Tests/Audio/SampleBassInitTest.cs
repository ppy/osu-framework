// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Audio.Sample;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleBassInitTest
    {
        private BassAudioPipeline pipeline;
        private Sample sample;

        [SetUp]
        public void Setup()
        {
            pipeline = new BassAudioPipeline(false);
            sample = pipeline.GetSample();

            pipeline.Update();
            pipeline.Init();
        }

        [TearDown]
        public void Teardown()
        {
            pipeline?.Dispose();
        }

        [Test]
        public void TestSampleInitialisesOnUpdateDevice()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
                Assert.Ignore("Test may be intermittent on linux (see AudioThread.FreeDevice()).");

            Assert.That(sample.IsLoaded, Is.False);
            pipeline.RunOnAudioThread(() => pipeline.SampleStore.UpdateDevice(0));
            Assert.That(sample.IsLoaded, Is.True);
        }
    }
}
