// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Audio;
using System.IO;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.Audio
{
    public abstract class AudioTestComponents : IDisposable
    {
        public enum Type
        {
            BASS,
            SDL3
        }

        internal readonly AudioMixer Mixer;
        public readonly DllResourceStore Resources;
        internal readonly TrackStore TrackStore;
        internal readonly SampleStore SampleStore;

        protected readonly AudioCollectionManager<AudioComponent> AllComponents = new AudioCollectionManager<AudioComponent>();
        protected readonly AudioCollectionManager<AudioComponent> MixerComponents = new AudioCollectionManager<AudioComponent>();

        protected AudioTestComponents(bool init)
        {
            Prepare();

            if (init)
                Init();

            AllComponents.AddItem(MixerComponents);

            Mixer = CreateMixer();
            Resources = new DllResourceStore(typeof(TrackBassTest).Assembly);
            TrackStore = new TrackStore(Resources, Mixer, CreateTrack);
            SampleStore = new SampleStore(Resources, Mixer, CreateSampleFactory);

            Add(TrackStore, SampleStore);
        }

        protected virtual void Prepare()
        {
        }

        internal abstract Track CreateTrack(Stream data, string name);

        internal abstract SampleFactory CreateSampleFactory(Stream stream, string name, AudioMixer mixer, int playbackConcurrency);

        public abstract void Init();

        public virtual void Add(params AudioComponent[] component)
        {
            foreach (var c in component)
                AllComponents.AddItem(c);
        }

        public abstract AudioMixer CreateMixer();

        public virtual void Update()
        {
            RunOnAudioThread(AllComponents.Update);
        }

        /// <summary>
        /// Runs an <paramref name="action"/> on a newly created audio thread, and blocks until it has been run to completion.
        /// </summary>
        /// <param name="action">The action to run on the audio thread.</param>
        public virtual void RunOnAudioThread(Action action) => AudioTestHelper.RunOnAudioThread(action);

        internal Track GetTrack() => TrackStore.Get("Resources.Tracks.sample-track.mp3");
        internal Sample GetSample() => SampleStore.Get("Resources.Tracks.sample-track.mp3");

        public void Dispose() => RunOnAudioThread(() =>
        {
            AllComponents.Dispose();
            AllComponents.Update(); // Actually runs the disposal.

            DisposeInternal();
        });

        public virtual void DisposeInternal()
        {
        }
    }
}
