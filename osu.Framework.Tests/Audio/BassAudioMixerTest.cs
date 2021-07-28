// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using ManagedBass.Fx;
using ManagedBass.Mix;
using NUnit.Framework;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class BassAudioMixerTest
    {
        private TestBassAudioPipeline pipeline;
        private TrackBass track;
        private SampleBass sample;

        [SetUp]
        public void Setup()
        {
            pipeline = new TestBassAudioPipeline();
            track = pipeline.GetTrack();
            sample = pipeline.GetSample();

            pipeline.Update();
            pipeline.Update();
        }

        [TearDown]
        public void Teardown()
        {
            pipeline?.Dispose();
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

        [Test]
        public void TestTrackReferenceLostWhenTrackIsDisposed()
        {
            track.Dispose();

            // The first update disposes the track, the second one removes the track from the TrackStore.
            pipeline.Update();
            pipeline.Update();

            var trackReference = new WeakReference<TrackBass>(track);
            track = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Assert.That(!trackReference.TryGetTarget(out _));
        }

        [Test]
        public void TestSampleChannelReferenceLostWhenSampleChannelIsDisposed()
        {
            var channelReference = runTest(sample);

            // The first update disposes the track, the second one removes the track from the TrackStore.
            pipeline.Update();
            pipeline.Update();

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
            pipeline.Mixer.Effects.Add(new BQFParameters());
            assertEffectParameters();

            pipeline.Mixer.Effects.AddRange(new[]
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
            pipeline.Mixer.Effects.Add(new BQFParameters());
            assertEffectParameters();

            pipeline.Mixer.Effects.RemoveAt(0);
            assertEffectParameters();

            pipeline.Mixer.Effects.AddRange(new[]
            {
                new BQFParameters(),
                new BQFParameters(),
                new BQFParameters()
            });
            assertEffectParameters();

            pipeline.Mixer.Effects.RemoveAt(1);
            assertEffectParameters();

            pipeline.Mixer.Effects.RemoveAt(1);
            assertEffectParameters();
        }

        [Test]
        public void TestMoveEffect()
        {
            pipeline.Mixer.Effects.AddRange(new[]
            {
                new BQFParameters(),
                new BQFParameters(),
                new BQFParameters()
            });
            assertEffectParameters();

            pipeline.Mixer.Effects.Move(0, 1);
            assertEffectParameters();

            pipeline.Mixer.Effects.Move(2, 0);
            assertEffectParameters();
        }

        [Test]
        public void TestReplaceEffect()
        {
            pipeline.Mixer.Effects.AddRange(new[]
            {
                new BQFParameters(),
                new BQFParameters(),
                new BQFParameters()
            });
            assertEffectParameters();

            pipeline.Mixer.Effects[1] = new BQFParameters();
            assertEffectParameters();
        }

        [Test]
        public void TestInsertEffect()
        {
            pipeline.Mixer.Effects.AddRange(new[]
            {
                new BQFParameters(),
                new BQFParameters()
            });
            assertEffectParameters();

            pipeline.Mixer.Effects.Insert(1, new BQFParameters());
            assertEffectParameters();

            pipeline.Mixer.Effects.Insert(3, new BQFParameters());
            assertEffectParameters();
        }

        private void assertEffectParameters()
        {
            pipeline.Update();

            Assert.That(pipeline.Mixer.MixedEffects.Count, Is.EqualTo(pipeline.Mixer.Effects.Count));

            Assert.Multiple(() =>
            {
                for (int i = 0; i < pipeline.Mixer.MixedEffects.Count; i++)
                {
                    Assert.That(pipeline.Mixer.MixedEffects[i].Effect, Is.EqualTo(pipeline.Mixer.Effects[i]));
                    Assert.That(pipeline.Mixer.MixedEffects[i].Priority, Is.EqualTo(-i));
                }
            });
        }

        private int getHandle() => ((IBassAudioChannel)track).Handle;
    }
}
