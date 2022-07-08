// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using NUnit.Framework;
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
        public void TestAddEffect()
        {
            bass.Mixer.Effects.Add(new BQFParameters());
            assertEffectParameters();

            bass.Mixer.Effects.AddRange(new[]
            {
                new BQFParameters(),
                new BQFParameters(),
                new BQFParameters()
            });
            assertEffectParameters();
        }

        [Test]
        public void TestRemoveEffect()
        {
            bass.Mixer.Effects.Add(new BQFParameters());
            assertEffectParameters();

            bass.Mixer.Effects.RemoveAt(0);
            assertEffectParameters();

            bass.Mixer.Effects.AddRange(new[]
            {
                new BQFParameters(),
                new BQFParameters(),
                new BQFParameters()
            });
            assertEffectParameters();

            bass.Mixer.Effects.RemoveAt(1);
            assertEffectParameters();

            bass.Mixer.Effects.RemoveAt(1);
            assertEffectParameters();
        }

        [Test]
        public void TestMoveEffect()
        {
            bass.Mixer.Effects.AddRange(new[]
            {
                new BQFParameters(),
                new BQFParameters(),
                new BQFParameters()
            });
            assertEffectParameters();

            bass.Mixer.Effects.Move(0, 1);
            assertEffectParameters();

            bass.Mixer.Effects.Move(2, 0);
            assertEffectParameters();
        }

        [Test]
        public void TestReplaceEffect()
        {
            bass.Mixer.Effects.AddRange(new[]
            {
                new BQFParameters(),
                new BQFParameters(),
                new BQFParameters()
            });
            assertEffectParameters();

            bass.Mixer.Effects[1] = new BQFParameters();
            assertEffectParameters();
        }

        [Test]
        public void TestInsertEffect()
        {
            bass.Mixer.Effects.AddRange(new[]
            {
                new BQFParameters(),
                new BQFParameters()
            });
            assertEffectParameters();

            bass.Mixer.Effects.Insert(1, new BQFParameters());
            assertEffectParameters();

            bass.Mixer.Effects.Insert(3, new BQFParameters());
            assertEffectParameters();
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

        private void assertEffectParameters()
        {
            bass.Update();

            Assert.That(bass.Mixer.ActiveEffects.Count, Is.EqualTo(bass.Mixer.Effects.Count));

            Assert.Multiple(() =>
            {
                for (int i = 0; i < bass.Mixer.ActiveEffects.Count; i++)
                {
                    Assert.That(bass.Mixer.ActiveEffects[i].Effect, Is.EqualTo(bass.Mixer.Effects[i]));
                    Assert.That(bass.Mixer.ActiveEffects[i].Priority, Is.EqualTo(-i));
                }
            });
        }

        private int getHandle() => ((IBassAudioChannel)track).Handle;
    }
}
