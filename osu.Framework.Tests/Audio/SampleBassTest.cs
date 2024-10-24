// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio.Sample;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleBassTest
    {
        private BassTestComponents bass;
        private Sample sample;
        private SampleChannel channel;

        [SetUp]
        public void Setup()
        {
            bass = new BassTestComponents();
            sample = bass.GetSample();

            bass.Update();
        }

        [TearDown]
        public void Teardown()
        {
            bass?.Dispose();
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
            bass.Update();

            Thread.Sleep(50);

            bass.Update();

            Assert.IsTrue(channel.Playing);
        }

        [Test]
        public void TestStop()
        {
            channel = sample.Play();
            bass.Update();

            channel.Stop();
            bass.Update();

            Assert.IsFalse(channel.Playing);
        }

        [Test]
        public void TestStopBeforeLoadFinished()
        {
            channel = sample.Play();
            channel.Stop();

            bass.Update();

            Assert.IsFalse(channel.Playing);
        }

        [Test]
        public void TestStopsWhenFactoryDisposed()
        {
            channel = sample.Play();
            bass.Update();

            bass.SampleStore.Dispose();
            bass.Update();

            Assert.IsFalse(channel.Playing);
        }

        /// <summary>
        /// Tests the case where a play call can be run inline due to already being on the audio thread.
        /// Because it's immediately executed, a `Bass.Update()` call is not required before the channel's state is updated.
        /// </summary>
        [Test]
        public void TestPlayingUpdatedAfterInlinePlay()
        {
            bass.RunOnAudioThread(() => channel = sample.Play());
            Assert.That(channel.Playing, Is.True);
        }

        /// <summary>
        /// Tests the case where a stop call can be run inline due to already being on the audio thread.
        /// Because it's immediately executed, a `Bass.Update()` call is not required before the channel's state is updated.
        /// </summary>
        [Test]
        public void TestPlayingUpdatedAfterInlineStop()
        {
            channel = sample.Play();
            bass.Update();

            bass.RunOnAudioThread(() => channel.Stop());
            Assert.That(channel.Playing, Is.False);
        }
    }
}
