// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
        private SampleChannelVirtual channel;

        [SetUp]
        public void Setup()
        {
            channel = new SampleChannelVirtual();
            updateChannel();
        }

        [Test]
        public void TestStart()
        {
            Assert.IsFalse(channel.Played);
            Assert.IsFalse(channel.HasCompleted);

            channel.Play();
            updateChannel();

            Thread.Sleep(50);

            Assert.IsTrue(channel.Played);
            Assert.IsTrue(channel.HasCompleted);
        }

        private void updateChannel() => RunOnAudioThread(() => channel.Update());

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
