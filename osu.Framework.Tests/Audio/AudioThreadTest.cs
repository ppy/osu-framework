// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public abstract class AudioThreadTest
    {
        internal AudioManagerWithDeviceLoss Manager { get; private set; }
        private AudioThread thread;

        [SetUp]
        public virtual void SetUp()
        {
            thread = new AudioThread();

            var store = new NamespacedResourceStore<byte[]>(new DllResourceStore(@"osu.Framework.Tests.dll"), @"Resources");

            Manager = new AudioManagerWithDeviceLoss(thread, store, store);

            thread.Start();
            WaitAudioFrame();
        }

        [TearDown]
        public void TearDown()
        {
            Assert.IsFalse(thread.Exited);

            Manager?.Dispose();
            WaitAudioFrame();

            thread.Exit();
            WaitForOrAssert(() => thread.Exited, "Audio thread did not exit in time");
        }

        public void CheckTrackIsProgressing(Track track)
        {
            // playback should be continuing after device change
            for (int i = 0; i < 2; i++)
            {
                double checkAfter = track.CurrentTime;
                WaitForOrAssert(() => track.CurrentTime > checkAfter, "Track time did not increase", 1000);
                Assert.IsTrue(track.IsRunning);
            }
        }

        /// <summary>
        /// Waits for a specified condition to become true, or timeout reached.
        /// </summary>
        /// <param name="condition">The condition which should become true.</param>
        /// <param name="message">A message to display on timeout.</param>
        /// <param name="timeout">Timeout in milliseconds.</param>
        public static void WaitForOrAssert(Func<bool> condition, string message, int timeout = 60000) =>
            Assert.IsTrue(Task.Run(() =>
            {
                while (!condition()) Thread.Sleep(50);
            }).Wait(timeout), message);

        /// <summary>
        /// Block for a specified number of audio thread frames.
        /// </summary>
        /// <param name="count">The number of frames to wait for. Two frames is generally considered safest.</param>
        protected void WaitAudioFrame(int count = 2)
        {
            var cts = new TaskCompletionSource<bool>();

            void runScheduled() => thread.Scheduler.Add(() =>
            {
                if (count-- > 0)
                    runScheduled();
                else
                {
                    cts.SetResult(true);
                }
            });

            runScheduled();

            Task.WaitAll(cts.Task);
        }
    }
}
