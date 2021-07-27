// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using ManagedBass.Mix;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class BassAudioMixerTest
    {
        private TestBassAudioPipeline pipeline;
        private TrackBass track;

        [SetUp]
        public void Setup()
        {
            pipeline = new TestBassAudioPipeline();
            track = track = pipeline.GetTrack();

            pipeline.Update();
            pipeline.Update();
        }

        [Test]
        public void TestMixerInitialised()
        {
            Assert.That(pipeline.Mixer.Handle, Is.Not.Zero);
        }

        [Test]
        public void TestAddedToDefaultMixerByDefault()
        {
            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(pipeline.Mixer.Handle));
        }

        [Test]
        public void TestCannotBeRemovedFromDefaultMixer()
        {
            pipeline.Mixer.Remove(track);
            pipeline.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(pipeline.Mixer.Handle));
        }

        [Test]
        public void TestTrackIsMovedBetweenMixers()
        {
            var secondMixer = pipeline.CreateMixer();

            secondMixer.Add(track);
            pipeline.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(secondMixer.Handle));

            pipeline.Mixer.Add(track);
            pipeline.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(pipeline.Mixer.Handle));
        }

        [Test]
        public void TestMovedToDefaultMixerWhenRemovedFromMixer()
        {
            var secondMixer = pipeline.CreateMixer();

            secondMixer.Add(track);
            secondMixer.Remove(track);
            pipeline.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(pipeline.Mixer.Handle));
        }

        [Test]
        public void TestVirtualTrackCanBeAddedAndRemoved()
        {
            var secondMixer = pipeline.CreateMixer();
            var virtualTrack = pipeline.TrackStore.GetVirtual();

            secondMixer.Add(virtualTrack);
            pipeline.Update();

            secondMixer.Remove(virtualTrack);
            pipeline.Update();
        }

        [Test]
        public void TestFreedChannelRemovedFromDefault()
        {
            track.Dispose();
            pipeline.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.Zero);
        }

        [Test]
        public void TestChannelMovedToDefaultMixerAfterDispose()
        {
            var secondMixer = pipeline.CreateMixer();

            secondMixer.Add(track);
            pipeline.Update();

            secondMixer.Dispose();
            pipeline.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(pipeline.Mixer.Handle));
        }

        [Test]
        public void TestPlayPauseStop()
        {
            Assert.That(!track.IsRunning);

            pipeline.RunOnAudioThread(() => track.Start());
            pipeline.Update();

            Assert.That(track.IsRunning);

            pipeline.RunOnAudioThread(() => track.Stop());
            pipeline.Update();

            Assert.That(!track.IsRunning);

            pipeline.RunOnAudioThread(() =>
            {
                track.Seek(track.Length - 1000);
                track.Start();
            });

            pipeline.Update();

            Assert.That(() =>
            {
                pipeline.Update();
                return !track.IsRunning;
            }, Is.True.After(3000));
        }

        [Test]
        public void TestChannelRetainsPlayingStateWhenMovedBetweenMixers()
        {
            var secondMixer = pipeline.CreateMixer();

            secondMixer.Add(track);
            pipeline.Update();

            Assert.That(!track.IsRunning);

            pipeline.RunOnAudioThread(() => track.Start());
            pipeline.Update();

            Assert.That(track.IsRunning);

            pipeline.Mixer.Add(track);
            pipeline.Update();

            Assert.That(track.IsRunning);
        }

        private int getHandle() => ((IBassAudioChannel)track).Handle;
    }
}
