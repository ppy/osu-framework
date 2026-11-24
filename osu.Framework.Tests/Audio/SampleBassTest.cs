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
        private AudioTestComponents audio;
        private Sample sample;

        private SampleChannel channel;

        [TearDown]
        public void Teardown()
        {
            audio?.Dispose();
        }

        private void setupBackend(AudioTestComponents.Type id)
        {
            if (id == AudioTestComponents.Type.BASS)
            {
                audio = new BassTestComponents();
                sample = audio.GetSample();
            }
            else if (id == AudioTestComponents.Type.SDL3)
            {
                audio = new SDL3AudioTestComponents();
                sample = audio.GetSample();
            }
            else
            {
                throw new InvalidOperationException("not a supported id");
            }

            audio.Update();
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestGetChannelOnDisposed(AudioTestComponents.Type id)
        {
            setupBackend(id);

            sample.Dispose();

            sample.Update();

            Assert.Throws<ObjectDisposedException>(() => sample.GetChannel());
            Assert.Throws<ObjectDisposedException>(() => sample.Play());
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStart(AudioTestComponents.Type id)
        {
            setupBackend(id);

            channel = sample.Play();

            audio.Update();

            Thread.Sleep(50);

            audio.Update();

            Assert.IsTrue(channel.Playing);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStop(AudioTestComponents.Type id)
        {
            setupBackend(id);

            channel = sample.Play();
            audio.Update();

            channel.Stop();
            audio.Update();

            Assert.IsFalse(channel.Playing);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStopBeforeLoadFinished(AudioTestComponents.Type id)
        {
            setupBackend(id);

            channel = sample.Play();
            channel.Stop();

            audio.Update();

            Assert.IsFalse(channel.Playing);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStopsWhenFactoryDisposed(AudioTestComponents.Type id)
        {
            setupBackend(id);

            channel = sample.Play();
            audio.Update();

            audio.SampleStore.Dispose();
            audio.Update();

            Assert.IsFalse(channel.Playing);
        }

        /// <summary>
        /// Tests the case where a play call can be run inline due to already being on the audio thread.
        /// Because it's immediately executed, a `Bass.Update()` call is not required before the channel's state is updated.
        /// </summary>
        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestPlayingUpdatedAfterInlinePlay(AudioTestComponents.Type id)
        {
            setupBackend(id);

            audio.RunOnAudioThread(() => channel = sample.Play());
            Assert.That(channel.Playing, Is.True);
        }

        /// <summary>
        /// Tests the case where a stop call can be run inline due to already being on the audio thread.
        /// Because it's immediately executed, a `Bass.Update()` call is not required before the channel's state is updated.
        /// </summary>
        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestPlayingUpdatedAfterInlineStop(AudioTestComponents.Type id)
        {
            setupBackend(id);

            channel = sample.Play();
            audio.Update();

            audio.RunOnAudioThread(() => channel.Stop());
            Assert.That(channel.Playing, Is.False);
        }
    }
}
