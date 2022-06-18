// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Audio.Sample;
using osu.Framework.Development;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleChannelVirtualTest
    {
        private Sample sample;

        [SetUp]
        public void Setup()
        {
            sample = new SampleVirtual();
            updateSample();
        }

        [Test]
        public void TestStart()
        {
            var channel = sample.Play();
            Assert.IsTrue(channel.Playing);
            Assert.IsFalse(channel.HasCompleted);

            updateSample();

            Assert.IsFalse(channel.Playing);
            Assert.IsTrue(channel.HasCompleted);
        }

        [Test]
        public void TestLooping()
        {
            var channel = sample.Play();
            channel.Looping = true;
            Assert.IsTrue(channel.Playing);
            Assert.IsFalse(channel.HasCompleted);

            updateSample();

            Assert.IsTrue(channel.Playing);
            Assert.False(channel.HasCompleted);

            channel.Stop();

            Assert.False(channel.Playing);
            Assert.IsTrue(channel.HasCompleted);
        }

        private void updateSample() => RunOnAudioThread(() => sample.Update());

        /// <summary>
        /// Certain actions are invoked on the audio thread.
        /// Here we simulate this process on a correctly named thread to avoid endless blocking.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public static void RunOnAudioThread(Action action)
        {
            var resetEvent = new ManualResetEvent(false);

            new Thread(() =>
            {
                ThreadSafety.IsAudioThread = true;

                action();

                resetEvent.Set();
            })
            {
                Name = GameThread.PrefixedThreadNameFor("Audio")
            }.Start();

            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException();
        }
    }
}
