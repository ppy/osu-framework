// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using osu.Framework.Allocation;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.SDL3;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Threading;
using SDL;

namespace osu.Framework.Audio
{
    public unsafe class SDL3AudioManager : AudioManager
    {
        public static readonly int AUDIO_FREQ = 44100;
        public static readonly int AUDIO_CHANNELS = 2;
        public static readonly SDL_AudioFormat AUDIO_FORMAT = SDL3.SDL_AUDIO_F32;

        private volatile SDL_AudioDeviceID deviceId;
        private volatile SDL_AudioStream* deviceStream;

        private SDL_AudioSpec spec;
        private int bufferSize = (int)(AUDIO_FREQ * 0.01); // 10ms, will be calculated later when opening audio device, it works as a base value until then.

        private static readonly AudioDecoderManager decoder = new AudioDecoderManager();

        private readonly List<SDL3AudioMixer> sdlMixerList = new List<SDL3AudioMixer>();

        private ImmutableArray<SDL_AudioDeviceID> deviceIdArray = ImmutableArray<SDL_AudioDeviceID>.Empty;

        protected ObjectHandle<SDL3AudioManager> ObjectHandle { get; private set; }

        private Scheduler eventScheduler => EventScheduler ?? CurrentAudioThread.Scheduler;

        protected override void InvokeOnNewDevice(string deviceName) => eventScheduler.Add(() => base.InvokeOnNewDevice(deviceName));

        protected override void InvokeOnLostDevice(string deviceName) => eventScheduler.Add(() => base.InvokeOnLostDevice(deviceName));

        /// <summary>
        /// Creates a new <see cref="SDL3AudioManager"/>.
        /// </summary>
        /// <param name="audioThread">The host's audio thread.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        public SDL3AudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
            : base(audioThread, trackStore, sampleStore)
        {
            ObjectHandle = new ObjectHandle<SDL3AudioManager>(this, GCHandleType.Normal);

            // Must not edit this, as components (especially mixer) expects this to match.
            spec = new SDL_AudioSpec
            {
                freq = AUDIO_FREQ,
                channels = AUDIO_CHANNELS,
                format = AUDIO_FORMAT
            };

            // determines latency, but some audio servers may not make use of this hint
            SDL3.SDL_SetHint(SDL3.SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES, "256"u8);

            AudioScheduler.Add(() =>
            {
                syncAudioDevices();

                // comment below lines if you want to use FFmpeg to decode audio, AudioDecoder will use FFmpeg if no BASS device is available
                ManagedBass.Bass.Configure((ManagedBass.Configuration)68, 1);
                audioThread.InitDevice(ManagedBass.Bass.NoSoundDevice);
            });
        }

        private string currentDeviceName = "Not loaded";

        public override string ToString()
        {
            return $@"{GetType().ReadableName()} ({currentDeviceName})";
        }

        protected override AudioMixer AudioCreateAudioMixer(AudioMixer fallbackMixer, string identifier)
        {
            var mixer = new SDL3AudioMixer(fallbackMixer, identifier);
            AddItem(mixer);
            return mixer;
        }

        protected override void ItemAdded(AudioComponent item)
        {
            base.ItemAdded(item);

            if (item is SDL3AudioMixer mixer)
            {
                try
                {
                    if (deviceId != 0)
                        SDL3.SDL_LockAudioStream(deviceStream);

                    sdlMixerList.Add(mixer);
                }
                finally
                {
                    if (deviceId != 0)
                        SDL3.SDL_UnlockAudioStream(deviceStream);
                }
            }
        }

        protected override void ItemRemoved(AudioComponent item)
        {
            base.ItemRemoved(item);

            if (item is SDL3AudioMixer mixer)
            {
                try
                {
                    if (deviceId != 0)
                        SDL3.SDL_LockAudioStream(deviceStream);

                    sdlMixerList.Remove(mixer);
                }
                finally
                {
                    if (deviceId != 0)
                        SDL3.SDL_UnlockAudioStream(deviceStream);
                }
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void audioCallback(IntPtr userdata, SDL_AudioStream* stream, int additionalAmount, int totalAmount)
        {
            var handle = new ObjectHandle<SDL3AudioManager>(userdata);
            if (handle.GetTarget(out SDL3AudioManager audioManager))
                audioManager.internalAudioCallback(stream, additionalAmount);
        }

        private float[] audioBuffer;

        private void internalAudioCallback(SDL_AudioStream* stream, int additionalAmount)
        {
            additionalAmount /= 4;

            if (audioBuffer == null || audioBuffer.Length < additionalAmount)
                audioBuffer = new float[additionalAmount];

            try
            {
                int filled = 0;

                foreach (var mixer in sdlMixerList)
                {
                    if (mixer.IsAlive)
                        mixer.MixChannelsInto(audioBuffer, additionalAmount, ref filled);
                }

                fixed (float* ptr = audioBuffer)
                    SDL3.SDL_PutAudioStreamData(stream, (IntPtr)ptr, filled * 4);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while pushing audio to SDL");
            }
        }

        internal void OnNewDeviceEvent(SDL_AudioDeviceID addedDeviceIndex)
        {
            AudioScheduler.Add(() =>
            {
                // the index is only vaild until next SDL_GetNumAudioDevices call, so get the name first.
                string name = SDL3.SDL_GetAudioDeviceName(addedDeviceIndex);

                syncAudioDevices();
                InvokeOnNewDevice(name);
            });
        }

        internal void OnLostDeviceEvent(SDL_AudioDeviceID removedDeviceId)
        {
            AudioScheduler.Add(() =>
            {
                // SDL doesn't retain information about removed device.
                syncAudioDevices();

                if (!IsCurrentDeviceValid()) // current device lost
                {
                    InvokeOnLostDevice(currentDeviceName);
                    SetAudioDevice();
                }
                else
                {
                    // we can probably guess the name by comparing the old list and the new one, but it won't be reliable
                    InvokeOnLostDevice(string.Empty);
                }
            });
        }

        private void syncAudioDevices()
        {
            int count = 0;
            SDL_AudioDeviceID* idArrayPtr = SDL3.SDL_GetAudioOutputDevices(&count);

            var idArray = ImmutableArray.CreateBuilder<SDL_AudioDeviceID>(count);
            var nameArray = ImmutableArray.CreateBuilder<string>(count);

            for (int i = 0; i < count; i++)
            {
                SDL_AudioDeviceID id = *(idArrayPtr + i);
                string name = SDL3.SDL_GetAudioDeviceName(id);

                if (string.IsNullOrEmpty(name))
                    continue;

                idArray.Add(id);
                nameArray.Add(name);
            }

            deviceIdArray = idArray.ToImmutable();
            DeviceNames = nameArray.ToImmutableList();

            Logger.Log($"count {count} , id {deviceIdArray.Length} , names {DeviceNames.Count}");
        }

        private bool setAudioDevice(SDL_AudioDeviceID targetId)
        {
            if (deviceStream != null)
            {
                SDL3.SDL_DestroyAudioStream(deviceStream);
                deviceStream = null;
            }

            fixed (SDL_AudioSpec* ptr = &spec)
            {
                deviceStream = SDL3.SDL_OpenAudioDeviceStream(targetId, ptr, &audioCallback, ObjectHandle.Handle);

                if (deviceStream != null)
                {
                    deviceId = SDL3.SDL_GetAudioStreamDevice(deviceStream);

                    int sampleFrameSize = 0;
                    SDL_AudioSpec temp; // this has 'real' device info which is useless since SDL converts audio according to the spec we provided
                    if (SDL3.SDL_GetAudioDeviceFormat(deviceId, &temp, &sampleFrameSize) == 0)
                        bufferSize = sampleFrameSize * (int)Math.Ceiling((double)spec.freq / temp.freq);
                }
            }

            if (deviceStream == null)
            {
                if (targetId == SDL3.SDL_AUDIO_DEVICE_DEFAULT_OUTPUT)
                    return false;

                return setAudioDevice(SDL3.SDL_AUDIO_DEVICE_DEFAULT_OUTPUT);
            }

            SDL3.SDL_ResumeAudioDevice(deviceId);

            currentDeviceName = SDL3.SDL_GetAudioDeviceName(targetId);

            Logger.Log($@"🔈 SDL Audio initialised
                            Driver:      {SDL3.SDL_GetCurrentAudioDriver()}
                            Device Name: {currentDeviceName}
                            Format:      {spec.freq}hz {spec.channels}ch
                            Sample size: {bufferSize}");

            return true;
        }

        protected override bool SetAudioDevice(string deviceName = null)
        {
            deviceName ??= AudioDevice.Value;

            int deviceIndex = DeviceNames.FindIndex(d => d == deviceName);
            if (deviceIndex >= 0)
                return setAudioDevice(deviceIdArray[deviceIndex]);

            return setAudioDevice(SDL3.SDL_AUDIO_DEVICE_DEFAULT_OUTPUT);
        }

        protected override bool SetAudioDevice(int deviceIndex)
        {
            if (deviceIndex < deviceIdArray.Length && deviceIndex >= 0)
                return setAudioDevice(deviceIdArray[deviceIndex]);

            return SetAudioDevice();
        }

        protected override bool IsCurrentDeviceValid() => deviceId > 0 && SDL3.SDL_AudioDevicePaused(deviceId) == SDL3.SDL_FALSE;

        internal override Track.Track GetNewTrack(Stream data, string name)
        {
            TrackSDL3 track = new TrackSDL3(name, spec.freq, spec.channels, bufferSize);
            EnqueueAction(() => decoder.StartDecodingAsync(spec.freq, spec.channels, spec.format, data, track.ReceiveAudioData));
            return track;
        }

        internal override SampleFactory GetSampleFactory(Stream data, string name, AudioMixer mixer, int playbackConcurrency)
            => new SampleSDL3Factory(data, name, (SDL3AudioMixer)mixer, playbackConcurrency, spec);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            decoder?.Dispose();

            if (deviceStream != null)
            {
                SDL3.SDL_DestroyAudioStream(deviceStream);
                deviceStream = null;
                deviceId = 0;
                // Destroying audio stream will close audio device because we use SDL3 OpenAudioDeviceStream
                // won't use multiple AudioStream for now since it's barely useful
            }

            ObjectHandle.Dispose();
        }
    }
}
