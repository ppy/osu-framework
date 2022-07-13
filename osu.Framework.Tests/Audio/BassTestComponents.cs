// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Threading;
using ManagedBass;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing.Bass;
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
    public class BassTestComponents : IDisposable
    {
        internal readonly BassAudioMixer Mixer;
        public readonly DllResourceStore Resources;
        internal readonly TrackStore TrackStore;
        internal readonly SampleStore SampleStore;

        private readonly AudioCollectionManager<AudioComponent> allComponents = new AudioCollectionManager<AudioComponent>();
        private readonly AudioCollectionManager<AudioComponent> mixerComponents = new AudioCollectionManager<AudioComponent>();

        public BassTestComponents(bool init = true)
        {
            if (init)
                Init();

            allComponents.AddItem(mixerComponents);

            Mixer = CreateMixer();
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

        public void Add(params AudioComponent[] component)
        {
            foreach (var c in component)
                allComponents.AddItem(c);
        }

        internal BassAudioMixer CreateMixer()
        {
            var mixer = new BassAudioMixer(Mixer, "Test mixer");
            mixerComponents.AddItem(mixer);
            return mixer;
        }

        public void Update()
        {
            RunOnAudioThread(() => allComponents.Update());
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

        internal TrackBass GetTrack() => (TrackBass)TrackStore.Get("Resources.Tracks.sample-track.mp3");
        internal SampleBass GetSample() => (SampleBass)SampleStore.Get("Resources.Tracks.sample-track.mp3");

        public void Dispose()
        {
            allComponents.Dispose();
            allComponents.Update(); // Actually runs the disposal.
            Bass.Free();
        }
    }
}
