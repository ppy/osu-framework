// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.SDL2;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Threading;
using SDL2;

namespace osu.Framework.Audio
{
    public class SDL2AudioManager : AudioManager
    {
        public const int AUDIO_FREQ = 44100;
        public const byte AUDIO_CHANNELS = 2;
        public const ushort AUDIO_FORMAT = SDL.AUDIO_F32;

        private volatile uint deviceId;

        private SDL.SDL_AudioSpec spec;

        private static readonly AudioDecoderManager decoder = new AudioDecoderManager();

        private readonly List<SDL2AudioMixer> sdlMixerList = new List<SDL2AudioMixer>();

        /// <summary>
        /// Creates a new <see cref="SDL2AudioManager"/>.
        /// </summary>
        /// <param name="audioThread">The host's audio thread.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        public SDL2AudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
            : base(audioThread, trackStore, sampleStore)
        {
            // Must not edit this except for samples, as components (especially mixer) expects this to match.
            spec = new SDL.SDL_AudioSpec
            {
                freq = AUDIO_FREQ,
                channels = AUDIO_CHANNELS,
                format = AUDIO_FORMAT,
                callback = audioCallback,
                samples = 256 // determines latency, this value can be changed but is already reasonably low
            };

            // comment below lines if you want to use FFmpeg to decode audio, AudioDecoder will use FFmpeg if no BASS device is available
            EnqueueAction(() =>
            {
                ManagedBass.Bass.Configure((ManagedBass.Configuration)68, 1);
                AudioThread.InitDevice(0);
            });
        }

        private string currentDeviceName = "Not loaded";

        public override string ToString()
        {
            return $@"{GetType().ReadableName()} ({currentDeviceName})";
        }

        protected override AudioMixer AudioCreateAudioMixer(AudioMixer globalMixer, string identifier)
        {
            var mixer = new SDL2AudioMixer(globalMixer, identifier);
            AddItem(mixer);
            return mixer;
        }

        protected override void ItemAdded(AudioComponent item)
        {
            base.ItemAdded(item);

            if (item is SDL2AudioMixer mixer)
            {
                try
                {
                    if (deviceId != 0)
                        SDL.SDL_LockAudioDevice(deviceId);

                    sdlMixerList.Add(mixer);
                }
                finally
                {
                    if (deviceId != 0)
                        SDL.SDL_UnlockAudioDevice(deviceId);
                }
            }
        }

        protected override void ItemRemoved(AudioComponent item)
        {
            base.ItemRemoved(item);

            if (item is SDL2AudioMixer mixer)
            {
                try
                {
                    if (deviceId != 0)
                        SDL.SDL_LockAudioDevice(deviceId);

                    sdlMixerList.Remove(mixer);
                }
                finally
                {
                    if (deviceId != 0)
                        SDL.SDL_UnlockAudioDevice(deviceId);
                }
            }
        }

        private void audioCallback(IntPtr userdata, IntPtr stream, int bufsize)
        {
            try
            {
                float[] main = new float[bufsize / 4];

                foreach (var mixer in sdlMixerList)
                {
                    if (mixer.IsAlive)
                        mixer.MixChannelsInto(main);
                }

                unsafe
                {
                    fixed (float* mainPtr = main)
                        Buffer.MemoryCopy(mainPtr, stream.ToPointer(), bufsize, bufsize);
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while pushing audio to SDL");
            }
        }

        protected override bool IsDevicesUpdated(out ImmutableList<string> newDevices, out ImmutableList<string> lostDevices)
        {
            var updatedAudioDevices = EnumerateAllDevices().ToImmutableList();

            if (DeviceNames.SequenceEqual(updatedAudioDevices))
            {
                newDevices = lostDevices = ImmutableList<string>.Empty;
                return false;
            }

            newDevices = updatedAudioDevices.Except(DeviceNames).ToImmutableList();
            lostDevices = DeviceNames.Except(updatedAudioDevices).ToImmutableList();

            DeviceNames = updatedAudioDevices;
            return true;
        }

        protected virtual IEnumerable<string> EnumerateAllDevices()
        {
            int deviceCount = SDL.SDL_GetNumAudioDevices(0); // it may return -1 if only default device is available (sound server)
            for (int i = 0; i < deviceCount; i++)
                yield return SDL.SDL_GetAudioDeviceName(i, 0);
        }

        protected override bool SetAudioDevice(string deviceName = null)
        {
            if (!AudioDeviceNames.Contains(deviceName))
                deviceName = null;

            if (deviceId > 0)
                SDL.SDL_CloseAudioDevice(deviceId);

            // Let audio driver adjust latency, this may set to a high value on Windows, but let's just be safe
            const uint flag = SDL.SDL_AUDIO_ALLOW_SAMPLES_CHANGE;
            deviceId = SDL.SDL_OpenAudioDevice(deviceName, 0, ref spec, out var outspec, (int)flag);

            if (deviceId == 0)
            {
                if (deviceName == null)
                {
                    Logger.Log("SDL Audio init failed!", level: LogLevel.Error);
                    return false;
                }

                Logger.Log("SDL Audio init failed, try using default device...", level: LogLevel.Important);
                return SetAudioDevice();
            }

            spec = outspec;

            // Start playback
            SDL.SDL_PauseAudioDevice(deviceId, 0);

            currentDeviceName = deviceName ?? "Default";

            Logger.Log($@"🔈 SDL Audio initialised
                            Driver:      {SDL.SDL_GetCurrentAudioDriver()}
                            Device Name: {currentDeviceName}
                            Frequency:   {spec.freq} hz
                            Channels:    {spec.channels}
                            Format:      {(SDL.SDL_AUDIO_ISSIGNED(spec.format) ? "" : "un")}signed {SDL.SDL_AUDIO_BITSIZE(spec.format)} bits{(SDL.SDL_AUDIO_ISFLOAT(spec.format) ? " (float)" : "")}
                            Samples:     {spec.samples} samples
                            Buffer size: {spec.size} bytes");

            return true;
        }

        protected override bool SetAudioDevice(int deviceIndex)
        {
            if (deviceIndex < DeviceNames.Count && deviceIndex >= 0)
                return SetAudioDevice(DeviceNames[deviceIndex]);

            return SetAudioDevice();
        }

        protected override bool IsCurrentDeviceValid() => SDL.SDL_GetAudioDeviceStatus(deviceId) != SDL.SDL_AudioStatus.SDL_AUDIO_STOPPED;

        internal override Track.Track GetNewTrack(Stream data, string name)
        {
            TrackSDL2 track = new TrackSDL2(name, spec.freq, spec.channels, spec.samples);
            EnqueueAction(() => decoder.StartDecodingAsync(AUDIO_FREQ, AUDIO_CHANNELS, AUDIO_FORMAT, data, track.AddToQueue));
            return track;
        }

        internal override SampleFactory GetSampleFactory(Stream data, string name, AudioMixer mixer, int playbackConcurrency)
            => new SampleSDL2Factory(data, name, (SDL2AudioMixer)mixer, playbackConcurrency, spec);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            decoder?.Dispose();

            if (deviceId > 0)
            {
                SDL.SDL_CloseAudioDevice(deviceId);
                deviceId = 0;
            }
        }
    }
}
