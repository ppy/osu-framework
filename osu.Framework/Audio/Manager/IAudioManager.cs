// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Audio.Manager
{
    /// <summary>
    /// An audio manager responsible for audio output and routing to mixers.
    /// </summary>
    public interface IAudioManager
    {
        /// <summary>
        /// Is fired whenever a new audio device is discovered and provides its name.
        /// </summary>
        event Action<KeyValuePair<string, string>>? OnNewDevice;

        /// <summary>
        /// Is fired whenever an audio device is lost and provides its name.
        /// </summary>
        event Action<KeyValuePair<string, string>>? OnLostDevice;

        /// <summary>
        /// The identifier of preferred audio device we should use.
        /// </summary>
        Bindable<string> AudioDevice { get; }

        /// <summary>
        /// The pair of identifier and name of all available audio devices.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property does not contain the names of disabled audio devices.
        /// </para>
        /// <para>
        /// This property may also not necessarily contain the name of the default audio device provided by the OS.
        /// </para>
        /// </remarks>
        ImmutableDictionary<string, string> AudioDevices { get; }

        /// <summary>
        /// The identifier of the default audio device.
        /// </summary>
        /// <remarks>
        /// This may not necessarily be present in <see cref="AudioDevices"/>.
        /// </remarks>
        string DefaultDevice { get; }

        /// <summary>
        /// Volume of all samples played game-wide.
        /// </summary>
        BindableDouble VolumeSample { get; }

        /// <summary>
        /// Volume of all tracks played game-wide.
        /// </summary>
        BindableDouble VolumeTrack { get; }

        /// <summary>
        /// The scheduler used for invoking publicly exposed delegate events.
        /// </summary>
        Scheduler? EventScheduler { get; }

        /// <summary>
        /// Creates a new <see cref="AudioMixer"/>.
        /// </summary>
        /// <remarks>
        /// Channels removed from this <see cref="AudioMixer"/> fall back to the global <see cref="AudioManager.SampleMixer"/>.
        /// </remarks>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        AudioMixer CreateAudioMixer(string? identifier = default);

        /// <summary>
        /// Sets a track resource store, and a sample resource store.
        /// </summary>
        /// <param name="trackStore">The resource store containing all audio tracks to be used.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used.</param>
        void SetStore(ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore);

        /// <summary>
        /// Obtains the <see cref="TrackStore"/> corresponding to a given resource store.
        /// Returns the global <see cref="TrackStore"/> if no resource store is passed.
        /// </summary>
        /// <param name="store">The <see cref="IResourceStore{T}"/> of which to retrieve the <see cref="TrackStore"/>.</param>
        /// <param name="mixer">The <see cref="AudioMixer"/> to use for tracks created by this store. Defaults to the global <see cref="AudioManager.TrackMixer"/>.</param>
        ITrackStore GetTrackStore(IResourceStore<byte[]>? store = null, AudioMixer? mixer = null);

        /// <summary>
        /// Obtains the <see cref="SampleStore"/> corresponding to a given resource store.
        /// Returns the global <see cref="SampleStore"/> if no resource store is passed.
        /// </summary>
        /// <remarks>
        /// By default, <c>.wav</c> and <c>.ogg</c> extensions will be automatically appended to lookups on the returned store
        /// if the lookup does not correspond directly to an existing filename.
        /// Additional extensions can be added via <see cref="ISampleStore.AddExtension"/>.
        /// </remarks>
        /// <param name="store">The <see cref="IResourceStore{T}"/> of which to retrieve the <see cref="SampleStore"/>.</param>
        /// <param name="mixer">The <see cref="AudioMixer"/> to use for samples created by this store. Defaults to the global <see cref="AudioManager.SampleMixer"/>.</param>
        ISampleStore GetSampleStore(IResourceStore<byte[]>? store = null, AudioMixer? mixer = null);
    }
}
