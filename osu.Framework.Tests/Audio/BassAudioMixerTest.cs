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
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class BassAudioMixerTest
    {
        private AudioTestComponents.Type type;
        private AudioTestComponents audio;
        private AudioMixer mixer;
        private Track track;
        private Sample sample;

        [TearDown]
        public void Teardown()
        {
            audio?.Dispose();
        }

        private void setupBackend(AudioTestComponents.Type id, bool loadTrack = false)
        {
            type = id;

            if (id == AudioTestComponents.Type.BASS)
            {
                audio = new BassTestComponents();
                track = audio.GetTrack();
                sample = audio.GetSample();
            }
            else if (id == AudioTestComponents.Type.SDL3)
            {
                audio = new SDL3AudioTestComponents();
                track = audio.GetTrack();
                sample = audio.GetSample();

                if (loadTrack)
                    ((SDL3AudioTestComponents)audio).WaitUntilTrackIsLoaded((TrackSDL3)track);
            }
            else
            {
                throw new InvalidOperationException("not a supported id");
            }

            audio.Update();
            mixer = audio.Mixer;
        }

        private void assertThatMixerContainsChannel(AudioMixer mixer, IAudioChannel channel)
        {
            if (type == AudioTestComponents.Type.BASS)
                Assert.That(BassMix.ChannelGetMixer(((IBassAudioChannel)channel).Handle), Is.EqualTo(((BassAudioMixer)mixer).Handle));
            else
                Assert.That(channel.Mixer == mixer, Is.True);
        }

        [Test]
        public void TestMixerInitialised()
        {
            setupBackend(AudioTestComponents.Type.BASS);

            Assert.That(((BassAudioMixer)mixer).Handle, Is.Not.Zero);
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
                Assert.That(BassMix.ChannelGetMixer(((IBassAudioChannel)track).Handle), Is.Zero);
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
            setupBackend(id, true);

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

        // [TestCase(AudioTestComponents.Type.SDL3)]
        [TestCase(AudioTestComponents.Type.BASS)]
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

        private void assertThatTrackIsPlaying()
        {
            if (type == AudioTestComponents.Type.BASS)
                Assert.That(((BassAudioMixer)mixer).ChannelIsActive((TrackBass)track), Is.Not.EqualTo(PlaybackState.Playing));
            else
                Assert.That(track.IsRunning, Is.Not.True);
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestChannelDoesNotPlayIfReachedEndAndSeekedBackwards(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            audio.RunOnAudioThread(() =>
            {
                track.Seek(track.Length - 1);
                track.Start();
            });

            Thread.Sleep(50);
            audio.Update();

            assertThatTrackIsPlaying();

            audio.RunOnAudioThread(() => track.SeekAsync(0).WaitSafely());
            audio.Update();

            assertThatTrackIsPlaying();
        }

        [TestCase(AudioTestComponents.Type.BASS)]
        [TestCase(AudioTestComponents.Type.SDL3)]
        public void TestChannelDoesNotPlayIfReachedEndAndMovedMixers(AudioTestComponents.Type id)
        {
            setupBackend(id, true);

            audio.RunOnAudioThread(() =>
            {
                track.Seek(track.Length - 1);
                track.Start();
            });

            Thread.Sleep(50);
            audio.Update();

            assertThatTrackIsPlaying();

            var secondMixer = audio.CreateMixer();
            secondMixer.Add(track);
            audio.Update();

            assertThatTrackIsPlaying();
        }
    }
}
