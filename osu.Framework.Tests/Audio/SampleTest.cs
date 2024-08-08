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
    public class SampleTest
    {
        private BassTestComponents bass;
        private Sample sampleBass;

        private SDL3AudioTestComponents sdl3;
        private Sample sampleSDL3;

        private SampleChannel channel;

        [SetUp]
        public void Setup()
        {
            bass = new BassTestComponents();
            sampleBass = bass.GetSample();

            sdl3 = new SDL3AudioTestComponents();
            sampleSDL3 = sdl3.GetSample();

            bass.Update();
            sdl3.Update();
        }

        [TearDown]
        public void Teardown()
        {
            bass?.Dispose();
            sdl3?.Dispose();
        }

        private Sample getSample(AudioTestComponents.Type id)
        {
            if (id == AudioTestComponents.Type.BASS)
                return sampleBass;
            else if (id == AudioTestComponents.Type.SDL3)
                return sampleSDL3;
            else
                throw new InvalidOperationException("not a supported id");
        }

        private AudioTestComponents getTestComponents(AudioTestComponents.Type id)
        {
            if (id == AudioTestComponents.Type.BASS)
                return bass;
            else if (id == AudioTestComponents.Type.SDL3)
                return sdl3;
            else
                throw new InvalidOperationException("not a supported id");
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestGetChannelOnDisposed(AudioTestComponents.Type id)
        {
            var sample = getSample(id);

            sample.Dispose();

            sample.Update();

            Assert.Throws<ObjectDisposedException>(() => sample.GetChannel());
            Assert.Throws<ObjectDisposedException>(() => sample.Play());
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStart(AudioTestComponents.Type id)
        {
            var sample = getSample(id);
            var audio = getTestComponents(id);

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
            var sample = getSample(id);
            var audio = getTestComponents(id);

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
            var sample = getSample(id);
            var audio = getTestComponents(id);

            channel = sample.Play();
            channel.Stop();

            audio.Update();

            Assert.IsFalse(channel.Playing);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestStopsWhenFactoryDisposed(AudioTestComponents.Type id)
        {
            var sample = getSample(id);
            var audio = getTestComponents(id);

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
            var sample = getSample(id);
            var audio = getTestComponents(id);

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
            var sample = getSample(id);
            var audio = getTestComponents(id);

            var channel = sample.Play();
            audio.Update();

            audio.RunOnAudioThread(() => channel.Stop());
            Assert.That(channel.Playing, Is.False);
        }
    }
}
