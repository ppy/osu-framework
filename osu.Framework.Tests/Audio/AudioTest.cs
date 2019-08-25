// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Audio;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public abstract class AudioTest
    {
        private AudioThread thread;

        protected NamespacedResourceStore<byte[]> Store;
        protected AudioManager Manager;

        protected AudioComponent AudioComponent;

        [SetUp]
        public virtual void Setup()
        {
            Architecture.SetIncludePath();

            Store = new NamespacedResourceStore<byte[]>(new DllResourceStore("osu.Framework.Tests.dll"), "Resources");
            Manager = new AudioManager(thread = new AudioThread(), Store, Store);

            thread.Start();
        }

        [TearDown]
        public virtual void TearDown()
        {
            Assert.IsFalse(thread.Exited);

            thread.Exit();

            Thread.Sleep(500);

            Assert.IsTrue(thread.Exited);
        }

        protected void UpdateComponent() => RunOnAudioThread(() => AudioComponent?.Update());

        /// <summary>
        /// Certain actions are invoked on the audio thread.
        /// Here we simulate this process on a correctly named thread to avoid endless blocking.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        protected void RunOnAudioThread(Action action)
        {
            var resetEvent = new ManualResetEvent(false);

            new Thread(() =>
            {
                action();

                resetEvent.Set();
            })
            {
                Name = GameThread.PrefixedThreadNameFor("Audio")
            }.Start();

            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException();
        }

        /// <summary>
        /// Block for a specified number of audio thread frames.
        /// </summary>
        /// <param name="count">The number of frames to wait for. Two frames is generally considered safest.</param>
        protected void WaitAudioFrame(int count = 2)
        {
            var cts = new TaskCompletionSource<bool>();

            void runScheduled()
            {
                thread.Scheduler.Add(() =>
                {
                    if (count-- > 0)
                        runScheduled();
                    else
                    {
                        cts.SetResult(true);
                    }
                });
            }

            runScheduled();

            Task.WaitAll(cts.Task);
        }
    }
}
