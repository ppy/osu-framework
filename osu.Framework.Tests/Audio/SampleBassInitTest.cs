// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio.Sample;
using osu.Framework.Development;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleBassInitTest
    {
        private DllResourceStore resources;
        private SampleBassFactory sampleFactory;
        private Sample sample;

        [SetUp]
        public void Setup()
        {
            try
            {
                // Make sure that the audio device is not initialised.
                if (RuntimeInfo.OS != RuntimeInfo.Platform.Linux)
                {
                    Bass.CurrentDevice = 0;
                    Bass.Free();
                }
            }
            catch
            {
            }

            resources = new DllResourceStore(typeof(TrackBassTest).Assembly);
            sampleFactory = new SampleBassFactory(resources.Get("Resources.Tracks.sample-track.mp3"));
            sample = sampleFactory.CreateSample();

            updateSample();

            Bass.Init(0);
        }

        [TearDown]
        public void Teardown()
        {
            // See AudioThread.FreeDevice().
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Linux)
                Bass.Free();
        }

        [Test]
        public void TestSampleInitialisesOnUpdateDevice()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
                Assert.Ignore("Test may be intermittent on linux (see AudioThread.FreeDevice()).");

            Assert.That(sample.IsLoaded, Is.False);
            runOnAudioThread(() => sampleFactory.UpdateDevice(0));
            Assert.That(sample.IsLoaded, Is.True);
        }

        private void updateSample() => runOnAudioThread(() => sampleFactory.Update());

        /// <summary>
        /// Certain actions are invoked on the audio thread.
        /// Here we simulate this process on a correctly named thread to avoid endless blocking.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        private void runOnAudioThread(Action action)
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
