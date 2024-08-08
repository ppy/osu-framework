// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using ManagedBass;
using ManagedBass.Mix;
using NUnit.Framework;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Audio.Mixing.SDL3;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class AudioMixerTest
    {
        private BassTestComponents bass;
        private BassAudioMixer mixerBass => (BassAudioMixer)bass.Mixer;
        private TrackBass trackBass;
        private SampleBass sampleBass;

        private SDL3AudioTestComponents sdl3;
        private SDL3AudioMixer mixerSDL3 => (SDL3AudioMixer)sdl3.Mixer;
        private TrackSDL3 trackSDL3;
        private SampleSDL3 sampleSDL3;

        private AudioTestComponents.Type type;
        private AudioTestComponents audio;
        private AudioMixer mixer;
        private Track track;
        private Sample sample;

        [SetUp]
        public void Setup()
        {
            bass = new BassTestComponents();
            trackBass = (TrackBass)bass.GetTrack();
            sampleBass = (SampleBass)bass.GetSample();

            sdl3 = new SDL3AudioTestComponents();
            trackSDL3 = (TrackSDL3)sdl3.GetTrack();
            sampleSDL3 = (SampleSDL3)sdl3.GetSample();

            // TrackSDL3 doesn't have data readily available right away after constructed.
            while (!trackSDL3.IsCompletelyLoaded)
            {
                sdl3.Update();
                Thread.Sleep(10);
            }

            bass.Update();
            sdl3.Update();
        }

        [TearDown]
        public void Teardown()
        {
            bass?.Dispose();
            sdl3?.Dispose();
        }

        private void setupBackend(AudioTestComponents.Type id)
        {
            type = id;

            if (id == AudioTestComponents.Type.BASS)
            {
                audio = bass;
                mixer = mixerBass;
                track = trackBass;
                sample = sampleBass;
            }
            else if (id == AudioTestComponents.Type.SDL3)
            {
                audio = sdl3;
                mixer = mixerSDL3;
                track = trackSDL3;
                sample = sampleSDL3;
            }
            else
            {
                throw new InvalidOperationException("not a supported id");
            }
        }

        private void assertThatMixerContainsChannel(AudioMixer mixer, IAudioChannel channel)
        {
            TestContext.WriteLine($"{channel.Mixer.GetHashCode()} ({channel.Mixer.Identifier}) and {mixer.GetHashCode()} ({mixer.Identifier})");

            if (type == AudioTestComponents.Type.BASS)
                Assert.That(BassMix.ChannelGetMixer(((IBassAudioChannel)channel).Handle), Is.EqualTo(((BassAudioMixer)mixer).Handle));
            else
                Assert.That(channel.Mixer == mixer, Is.True);
        }

        [Test]
        public void TestMixerInitialised()
        {
            Assert.That(mixerBass.Handle, Is.Not.Zero);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestAddedToGlobalMixerByDefault(AudioTestComponents.Type id)
        {
            setupBackend(id);

            assertThatMixerContainsChannel(mixer, track);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestCannotBeRemovedFromGlobalMixerBass(AudioTestComponents.Type id)
        {
            setupBackend(id);

            mixer.Remove(track);
            audio.Update();

            assertThatMixerContainsChannel(mixer, track);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestTrackIsMovedBetweenMixers(AudioTestComponents.Type id)
        {
            setupBackend(id);

            var secondMixer = audio.CreateMixer();

            secondMixer.Add(track);
            audio.Update();

            assertThatMixerContainsChannel(secondMixer, track);

            mixer.Add(track);
            audio.Update();

            assertThatMixerContainsChannel(mixer, track);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestMovedToGlobalMixerWhenRemovedFromMixer(AudioTestComponents.Type id)
        {
            setupBackend(id);

            var secondMixer = audio.CreateMixer();

            secondMixer.Add(track);
            secondMixer.Remove(track);
            audio.Update();

            assertThatMixerContainsChannel(mixer, track);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestVirtualTrackCanBeAddedAndRemoved(AudioTestComponents.Type id)
        {
            setupBackend(id);

            var secondMixer = audio.CreateMixer();
            var virtualTrack = audio.TrackStore.GetVirtual();

            secondMixer.Add(virtualTrack);
            audio.Update();

            secondMixer.Remove(virtualTrack);
            audio.Update();
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestFreedChannelRemovedFromDefault(AudioTestComponents.Type id)
        {
            setupBackend(id);

            track.Dispose();
            audio.Update();

            if (id == AudioTestComponents.Type.BASS)
                Assert.That(BassMix.ChannelGetMixer(((IBassAudioChannel)trackBass).Handle), Is.Zero);
            else
                Assert.That(((IAudioChannel)track).Mixer, Is.Null);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestChannelMovedToGlobalMixerAfterDispose(AudioTestComponents.Type id)
        {
            setupBackend(id);

            var secondMixer = audio.CreateMixer();

            secondMixer.Add(track);
            audio.Update();

            secondMixer.Dispose();
            audio.Update();

            assertThatMixerContainsChannel(mixer, track);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestPlayPauseStop(AudioTestComponents.Type id)
        {
            setupBackend(id);

            Assert.That(!track.IsRunning);

            audio.RunOnAudioThread(() => track.Start());
            audio.Update();

            Assert.That(track.IsRunning);

            audio.RunOnAudioThread(() => track.Stop());
            audio.Update();

            Assert.That(!track.IsRunning);

            audio.RunOnAudioThread(() =>
            {
                track.Seek(track.Length - 1000);
                track.Start();
            });

            audio.Update();

            Assert.That(() =>
            {
                audio.Update();
                return !track.IsRunning;
            }, Is.True.After(3000));
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestChannelRetainsPlayingStateWhenMovedBetweenMixers(AudioTestComponents.Type id)
        {
            setupBackend(id);

            var secondMixer = audio.CreateMixer();

            secondMixer.Add(track);
            audio.Update();

            Assert.That(!track.IsRunning);

            audio.RunOnAudioThread(() => track.Start());
            audio.Update();

            Assert.That(track.IsRunning);

            mixer.Add(track);
            audio.Update();

            Assert.That(track.IsRunning);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestTrackReferenceLostWhenTrackIsDisposed(AudioTestComponents.Type id)
        {
            setupBackend(id);

            var trackReference = testDisposeTrackWithoutReference();

            // The first update disposes the track, the second one removes the track from the TrackStore.
            audio.Update();
            audio.Update();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.That(!trackReference.TryGetTarget(out _));
        }

        private WeakReference<Track> testDisposeTrackWithoutReference()
        {
            var weakRef = new WeakReference<Track>(track);

            track.Dispose();
            track = null;

            if (type == AudioTestComponents.Type.BASS)
                trackBass = null;
            else if (type == AudioTestComponents.Type.SDL3)
                trackSDL3 = null;

            return weakRef;
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestSampleChannelReferenceLostWhenSampleChannelIsDisposed(AudioTestComponents.Type id)
        {
            setupBackend(id);

            var channelReference = runTest(sample);

            // The first update disposes the track, the second one removes the track from the TrackStore.
            audio.Update();
            audio.Update();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.That(!channelReference.TryGetTarget(out _));

            static WeakReference<SampleChannel> runTest(Sample sample)
            {
                var channel = sample.GetChannel();

                channel.Play(); // Creates the handle/adds to mixer.
                channel.Stop();
                channel.Dispose();

                return new WeakReference<SampleChannel>(channel);
            }
        }

        private void assertIfTrackIsPlaying()
        {
            if (type == AudioTestComponents.Type.BASS)
                Assert.That(mixerBass.ChannelIsActive(trackBass), Is.Not.EqualTo(PlaybackState.Playing));
            else
                Assert.That(track.IsRunning, Is.Not.True);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestChannelDoesNotPlayIfReachedEndAndSeekedBackwards(AudioTestComponents.Type id)
        {
            setupBackend(id);

            audio.RunOnAudioThread(() =>
            {
                track.Seek(track.Length - 1);
                track.Start();
            });

            Thread.Sleep(50);
            audio.Update();

            assertIfTrackIsPlaying();

            audio.RunOnAudioThread(() => track.SeekAsync(0).WaitSafely());
            audio.Update();

            assertIfTrackIsPlaying();
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestChannelDoesNotPlayIfReachedEndAndMovedMixers(AudioTestComponents.Type id)
        {
            setupBackend(id);

            audio.RunOnAudioThread(() =>
            {
                track.Seek(track.Length - 1);
                track.Start();
            });

            Thread.Sleep(50);
            audio.Update();

            assertIfTrackIsPlaying();

            var secondMixer = audio.CreateMixer();
            secondMixer.Add(track);
            audio.Update();

            assertIfTrackIsPlaying();
        }
    }
}
