// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio.Sample;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleBassInitTest
    {
        private TestBassAudioPipeline pipeline;
        private Sample sample;

        [SetUp]
        public void Setup()
        {
            pipeline = new TestBassAudioPipeline(false);
            sample = pipeline.GetSample();

            pipeline.Update();
            pipeline.Init();
        }

        [TearDown]
        public void Teardown()
        {
            // See AudioThread.FreeDevice().
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Linux)
                Bass.Free();
        }

        [Test]
        public void TestSampleInitialisesOnUpdateDevice()
        {
            // if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
            //     Assert.Ignore("Test may be intermittent on linux (see AudioThread.FreeDevice()).");

            Assert.That(sample.IsLoaded, Is.False);
            pipeline.RunOnAudioThread(() => pipeline.SampleStore.UpdateDevice(0));
            Assert.That(sample.IsLoaded, Is.True);
        }
    }
}
