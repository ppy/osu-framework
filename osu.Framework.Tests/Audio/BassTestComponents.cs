// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using ManagedBass;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
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
            var mixer = new BassAudioMixer(null, Mixer, "Test mixer");
            mixerComponents.AddItem(mixer);
            return mixer;
        }

        public void Update()
        {
            RunOnAudioThread(() => allComponents.Update());
        }

        /// <summary>
        /// Runs an <paramref name="action"/> on a newly created audio thread, and blocks until it has been run to completion.
        /// </summary>
        /// <param name="action">The action to run on the audio thread.</param>
        public void RunOnAudioThread(Action action) => AudioTestHelper.RunOnAudioThread(action);

        internal TrackBass GetTrack() => (TrackBass)TrackStore.Get("Resources.Tracks.sample-track.mp3");
        internal SampleBass GetSample() => (SampleBass)SampleStore.Get("Resources.Tracks.sample-track.mp3");

        public void Dispose() => RunOnAudioThread(() =>
        {
            allComponents.Dispose();
            allComponents.Update(); // Actually runs the disposal.
            Bass.Free();
        });
    }
}
