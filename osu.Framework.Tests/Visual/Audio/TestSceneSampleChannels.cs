// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneSampleChannels : FrameworkTestScene
    {
        [Resolved]
        private ISampleStore sampleStore { get; set; }

        private Sample sample;

        [BackgroundDependencyLoader]
        private void load()
        {
            sample = sampleStore.Get("long.mp3");
        }

        [Test]
        public void TestPlay()
        {
            AddStep("play sample", () => sample.Play());
        }

        [Test]
        public void TestDoesNotRestart()
        {
            SampleChannel channel = null;
            AddStep("play sample", () => channel = sample.Play());
            AddUntilStep("wait for end", () => !channel.Playing);
            AddStep("play channel", () => channel.Play());
            AddAssert("not playing", () => !channel.Playing);
        }

        [Test]
        public void TestPlayLoopingSample()
        {
            SampleChannel channel = null;
            AddStep("play sample", () =>
            {
                channel = sample.GetChannel();
                channel.Looping = true;
                channel.Play();
            });

            AddWaitStep("wait for loop", 20);
            AddAssert("still playing", () => channel.Playing);
            AddStep("stop playing", () => channel.Stop());
        }

        [Test]
        public void TestTogglePlaying()
        {
            SampleChannel channel = null;
            AddStep("play sample", () => channel = sample.Play());
            AddStep("stop channel", () => channel.Stop());
            AddStep("start channel", () => channel.Play());
            AddAssert("still playing", () => channel.Playing);
        }

        [Test]
        public void TestChannelWithNoReferenceDisposedAfterPlaybackFinished()
        {
            var channelRef = new WeakReference<SampleChannel>(null);
            AddStep("play sample", () => channelRef.SetTarget(sample.Play()));
            AddUntilStep("reference lost", () =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return !channelRef.TryGetTarget(out _);
            });
        }

        [Test]
        public void TestSampleWithNoReferenceDisposedAfterPlaybackFinished()
        {
            var sampleRef = new WeakReference<Sample>(null);

            AddStep("play sample", () =>
            {
                var sample2 = sampleStore.Get("long.mp3");

                sampleRef.SetTarget(sample2);
                sample2.Play();
            });

            AddUntilStep("reference lost", () =>
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return !sampleRef.TryGetTarget(out _);
            });
        }
    }
}
