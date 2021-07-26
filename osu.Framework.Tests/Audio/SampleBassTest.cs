// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio.Sample;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleBassTest
    {
        private TestBassAudioPipeline pipeline;
        private Sample sample;
        private SampleChannel channel;

        [SetUp]
        public void Setup()
        {
            pipeline = new TestBassAudioPipeline();
            sample = pipeline.GetSample();

            pipeline.Update();
        }

        [TearDown]
        public void Teardown()
        {
            // See AudioThread.freeDevice().
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Linux)
                Bass.Free();
        }

        [Test]
        public void TestGetChannelOnDisposed()
        {
            sample.Dispose();

            sample.Update();

            Assert.Throws<ObjectDisposedException>(() => sample.GetChannel());
            Assert.Throws<ObjectDisposedException>(() => sample.Play());
        }

        [Test]
        public void TestStart()
        {
            channel = sample.Play();
            pipeline.Update();

            Thread.Sleep(50);

            pipeline.Update();

            Assert.IsTrue(channel.Playing);
        }

        [Test]
        public void TestStop()
        {
            channel = sample.Play();
            pipeline.Update();

            channel.Stop();
            pipeline.Update();

            Assert.IsFalse(channel.Playing);
        }

        [Test]
        public void TestStopBeforeLoadFinished()
        {
            channel = sample.Play();
            channel.Stop();

            pipeline.Update();

            Assert.IsFalse(channel.Playing);
        }

        [Test]
        public void TestStopsWhenFactoryDisposed()
        {
            channel = sample.Play();
            pipeline.Update();

            pipeline.SampleStore.Dispose();
            pipeline.Update();

            Assert.IsFalse(channel.Playing);
        }
    }
}
