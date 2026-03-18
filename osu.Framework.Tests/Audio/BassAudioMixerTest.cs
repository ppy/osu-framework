// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Reflection;
using System.Threading;
using ManagedBass;
using ManagedBass.Mix;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class BassAudioMixerTest
    {
        private BassTestComponents bass;
        private TrackBass track;
        private SampleBass sample;

        [SetUp]
        public void Setup()
        {
            bass = new BassTestComponents();
            track = bass.GetTrack();
            sample = bass.GetSample();

            bass.Update();
        }

        [TearDown]
        public void Teardown()
        {
            bass?.Dispose();
        }

        [Test]
        public void TestMixerInitialised()
        {
            Assert.That(bass.Mixer.Handle, Is.Not.Zero);
        }

        [Test]
        public void TestAddedToGlobalMixerByDefault()
        {
            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(bass.Mixer.Handle));
        }

        [Test]
        public void TestCannotBeRemovedFromGlobalMixer()
        {
            bass.Mixer.Remove(track);
            bass.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(bass.Mixer.Handle));
        }

        [Test]
        public void TestTrackIsMovedBetweenMixers()
        {
            var secondMixer = bass.CreateMixer();

            secondMixer.Add(track);
            bass.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(secondMixer.Handle));

            bass.Mixer.Add(track);
            bass.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(bass.Mixer.Handle));
        }

        [Test]
        public void TestMovedToGlobalMixerWhenRemovedFromMixer()
        {
            var secondMixer = bass.CreateMixer();

            secondMixer.Add(track);
            secondMixer.Remove(track);
            bass.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(bass.Mixer.Handle));
        }

        [Test]
        public void TestVirtualTrackCanBeAddedAndRemoved()
        {
            var secondMixer = bass.CreateMixer();
            var virtualTrack = bass.TrackStore.GetVirtual();

            secondMixer.Add(virtualTrack);
            bass.Update();

            secondMixer.Remove(virtualTrack);
            bass.Update();
        }

        [Test]
        public void TestFreedChannelRemovedFromDefault()
        {
            track.Dispose();
            bass.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.Zero);
        }

        [Test]
        public void TestChannelMovedToGlobalMixerAfterDispose()
        {
            var secondMixer = bass.CreateMixer();

            secondMixer.Add(track);
            bass.Update();

            secondMixer.Dispose();
            bass.Update();

            Assert.That(BassMix.ChannelGetMixer(getHandle()), Is.EqualTo(bass.Mixer.Handle));
        }

        [Test]
        public void TestPlayPauseStop()
        {
            Assert.That(!track.IsRunning);

            bass.RunOnAudioThread(() => track.Start());
            bass.Update();

            Assert.That(track.IsRunning);

            bass.RunOnAudioThread(() => track.Stop());
            bass.Update();

            Assert.That(!track.IsRunning);

            bass.RunOnAudioThread(() =>
            {
                track.Seek(track.Length - 1000);
                track.Start();
            });

            bass.Update();

            Assert.That(() =>
            {
                bass.Update();
                return !track.IsRunning;
            }, Is.True.After(3000));
        }

        [Test]
        public void TestChannelRetainsPlayingStateWhenMovedBetweenMixers()
        {
            var secondMixer = bass.CreateMixer();

            secondMixer.Add(track);
            bass.Update();

            Assert.That(!track.IsRunning);

            bass.RunOnAudioThread(() => track.Start());
            bass.Update();

            Assert.That(track.IsRunning);

            bass.Mixer.Add(track);
            bass.Update();

            Assert.That(track.IsRunning);
        }

        [Test]
        public void TestTrackReferenceLostWhenTrackIsDisposed()
        {
            var trackReference = testDisposeTrackWithoutReference();

            // The first update disposes the track, the second one removes the track from the TrackStore.
            bass.Update();
            bass.Update();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.That(!trackReference.TryGetTarget(out _));
        }

        private WeakReference<TrackBass> testDisposeTrackWithoutReference()
        {
            var weakRef = new WeakReference<TrackBass>(track);

            track.Dispose();
            track = null;

            return weakRef;
        }

        [Test]
        public void TestSampleChannelReferenceLostWhenSampleChannelIsDisposed()
        {
            var channelReference = runTest(sample);

            // The first update disposes the track, the second one removes the track from the TrackStore.
            bass.Update();
            bass.Update();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.That(!channelReference.TryGetTarget(out _));

            static WeakReference<SampleChannel> runTest(SampleBass sample)
            {
                var channel = sample.GetChannel();

                channel.Play(); // Creates the handle/adds to mixer.
                channel.Stop();
                channel.Dispose();

                return new WeakReference<SampleChannel>(channel);
            }
        }

        [Test]
        public void TestChannelDoesNotPlayIfReachedEndAndSeekedBackwards()
        {
            bass.RunOnAudioThread(() =>
            {
                track.Seek(track.Length - 1);
                track.Start();
            });

            Thread.Sleep(50);
            bass.Update();

            Assert.That(bass.Mixer.ChannelIsActive(track), Is.Not.EqualTo(PlaybackState.Playing));

            bass.RunOnAudioThread(() => track.SeekAsync(0).WaitSafely());
            bass.Update();

            Assert.That(bass.Mixer.ChannelIsActive(track), Is.Not.EqualTo(PlaybackState.Playing));
        }

        [Test]
        public void TestChannelDoesNotPlayIfReachedEndAndMovedMixers()
        {
            bass.RunOnAudioThread(() =>
            {
                track.Seek(track.Length - 1);
                track.Start();
            });

            Thread.Sleep(50);
            bass.Update();

            Assert.That(bass.Mixer.ChannelIsActive(track), Is.Not.EqualTo(PlaybackState.Playing));

            var secondMixer = bass.CreateMixer();
            secondMixer.Add(track);
            bass.Update();

            Assert.That(secondMixer.ChannelIsActive(track), Is.Not.EqualTo(PlaybackState.Playing));
        }

        /// Tests a race condition where mixer.Add(track) is called before createMixer.
        /// Artificially forces the wrong order by manually modifying AudioComponent.pendingActions.
        /// This actually happens quite frequently when initializing TestSceneAudioMixer.cs.
        [Test]
        public void TestRaceConditionAddBeforeCreateMixer()
        {
            int trackHandle = getHandle();
            Assert.That(trackHandle, Is.Not.Zero, "Track should have a BASS handle");

            // Create a mixer but don't register it with the component manager yet
            var newMixer = new BassAudioMixer(null, bass.Mixer, "Race test mixer");
            bass.Add(newMixer);

            var enqueueActionMethod = typeof(AudioComponent).GetMethod("EnqueueAction",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var pendingActionsField = typeof(AudioComponent).GetField("PendingActions",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var createMixerMethod = typeof(BassAudioMixer).GetMethod("createMixer",
                BindingFlags.NonPublic | BindingFlags.Instance);

            Assert.That(enqueueActionMethod, Is.Not.Null, "Should be able to find EnqueueAction method");
            Assert.That(pendingActionsField, Is.Not.Null, "Should be able to find PendingActions field");
            Assert.That(createMixerMethod, Is.Not.Null, "Should be able to find createMixer method");

            // Clear the pending action queue to remove the auto-queued createMixer from constructor
            object pendingActions = pendingActionsField!.GetValue(newMixer);
            Assert.That(pendingActions, Is.Not.Null, "PendingActions should not be null");
            object newQueue = Activator.CreateInstance(pendingActions.GetType());
            pendingActionsField.SetValue(newMixer, newQueue);

            // Reconstruct the pending action queue to simulate what happens when mixer.Add(track) is called before the mixer is initialized
            // 1. track channel added to the BassAudioMixer.pendingChannels, since mixer handle is still null
            enqueueActionMethod.Invoke(newMixer,
            [
                new Action(() =>
                {
                    newMixer.Add(track);
                })
            ]);
            // 2. create mixer is called, after which mixer handle will be valid
            enqueueActionMethod.Invoke(newMixer,
            [
                new Action(() => createMixerMethod!.Invoke(newMixer, null))
            ]);
            // 3. track channel salvaged from pending channels and added to the mixer by BassAudioMixer.UpdateState
            bass.Update();

            Assert.That(newMixer.Handle, Is.Not.Zero, "Mixer should be initialized");

            // Now the track should be in the new mixer
            Assert.That(BassMix.ChannelGetMixer(trackHandle), Is.EqualTo(newMixer.Handle),
                "Track should be successfully added to the mixer even when Add was called before createMixer");
        }

        private int getHandle() => ((IBassAudioChannel)track).Handle;
    }
}
