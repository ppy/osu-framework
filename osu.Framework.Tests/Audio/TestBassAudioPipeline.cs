// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Threading;
using ManagedBass;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Development;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    /// <summary>
    /// Provides a BASS audio pipeline to be used for testing audio components.
    /// </summary>
    public class TestBassAudioPipeline
    {
        public readonly BassAudioMixer Mixer;
        public readonly DllResourceStore Resources;
        internal readonly TrackStore TrackStore;
        internal readonly SampleStore SampleStore;

        private readonly Scheduler scheduler;
        private readonly List<AudioComponent> components = new List<AudioComponent>();

        public TestBassAudioPipeline(bool init = true)
        {
            if (init)
                Init();

            scheduler = new Scheduler();
            Mixer = new BassAudioMixer(scheduler);

            Resources = new DllResourceStore(typeof(TrackBassTest).Assembly);
            TrackStore = new TrackStore(Resources, Mixer);
            SampleStore = new SampleStore(Resources, Mixer);

            Add(TrackStore, SampleStore);
        }

        public void Init()
        {
            AudioThread.PreloadBass();

            // Initialize bass with no audio to make sure the test remains consistent even if there is no audio device.
            Bass.Configure(ManagedBass.Configuration.UpdatePeriod, 5);
            Bass.Init(0);
        }

        public void Add(params AudioComponent[] component) => components.AddRange(component);

        public void Update()
        {
            RunOnAudioThread(() =>
            {
                scheduler.Update();
                Mixer.Update();

                foreach (var c in components)
                    c.Update();
            });
        }

        public void RunOnAudioThread(Action action)
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
