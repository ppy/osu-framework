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
using static SDL.SDL3;

namespace osu.Framework.Audio
{
    public class SDL3AudioManager : AudioManager
    {
        public static readonly int AUDIO_FREQ = 44100;
        public static readonly int AUDIO_CHANNELS = 2;
        public static readonly SDL_AudioFormat AUDIO_FORMAT = SDL_AUDIO_F32;

        private readonly List<SDL3AudioMixer> sdlMixerList = new List<SDL3AudioMixer>();

        private ImmutableArray<SDL_AudioDeviceID> deviceIdArray = ImmutableArray<SDL_AudioDeviceID>.Empty;

        private Scheduler eventScheduler => EventScheduler ?? CurrentAudioThread.Scheduler;

        protected override void InvokeOnNewDevice(string deviceName) => eventScheduler.Add(() => base.InvokeOnNewDevice(deviceName));

        protected override void InvokeOnLostDevice(string deviceName) => eventScheduler.Add(() => base.InvokeOnLostDevice(deviceName));

        private SDL3BaseAudioManager baseManager;

        /// <summary>
        /// Creates a new <see cref="SDL3AudioManager"/>.
        /// </summary>
        /// <param name="audioThread">The host's audio thread.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        public SDL3AudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore)
            : base(audioThread, trackStore, sampleStore)
        {
            AudioScheduler.Add(syncAudioDevices);
        }

        protected override void Prepare()
        {
            baseManager = new SDL3BaseAudioManager(() => sdlMixerList);
        }

        public override string ToString()
        {
            return $@"{GetType().ReadableName()} ({baseManager.DeviceName})";
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
                baseManager.RunWhileLockingAudioStream(() => sdlMixerList.Add(mixer));
        }

        protected override void ItemRemoved(AudioComponent item)
        {
            base.ItemRemoved(item);

            if (item is SDL3AudioMixer mixer)
                baseManager.RunWhileLockingAudioStream(() => sdlMixerList.Remove(mixer));
        }

        internal void OnNewDeviceEvent(SDL_AudioDeviceID addedDeviceIndex)
        {
            AudioScheduler.Add(() =>
            {
                // the index is only valid until next SDL_GetNumAudioDevices call, so get the name first.
                string name = SDL_GetAudioDeviceName(addedDeviceIndex);

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
                    InvokeOnLostDevice(baseManager.DeviceName);
                    SetAudioDevice();
                }
                else
                {
                    // we can probably guess the name by comparing the old list and the new one, but it won't be reliable
                    InvokeOnLostDevice(string.Empty);
                }
            });
        }

        private unsafe void syncAudioDevices()
        {
            int count = 0;
            SDL_AudioDeviceID* idArrayPtr = SDL_GetAudioPlaybackDevices(&count);

            var idArray = ImmutableArray.CreateBuilder<SDL_AudioDeviceID>(count);
            var nameArray = ImmutableArray.CreateBuilder<string>(count);

            for (int i = 0; i < count; i++)
            {
                SDL_AudioDeviceID id = *(idArrayPtr + i);
                string name = SDL_GetAudioDeviceName(id);

                if (string.IsNullOrEmpty(name))
                    continue;

                idArray.Add(id);
                nameArray.Add(name);
            }

            deviceIdArray = idArray.ToImmutable();
            DeviceNames = nameArray.ToImmutableList();
        }

        private bool setAudioDevice(SDL_AudioDeviceID targetId)
        {
            if (baseManager.SetAudioDevice(targetId))
            {
                Logger.Log($@"🔈 SDL Audio initialised
                            Driver:      {SDL_GetCurrentAudioDriver()}
                            Device Name: {baseManager.DeviceName}
                            Format:      {baseManager.AudioSpec.freq}hz {baseManager.AudioSpec.channels}ch
                            Sample size: {baseManager.BufferSize}");

                return true;
            }

            return false;
        }

        protected override bool SetAudioDevice(string deviceName = null)
        {
            deviceName ??= AudioDevice.Value;

            int deviceIndex = DeviceNames.FindIndex(d => d == deviceName);
            if (deviceIndex >= 0)
                return setAudioDevice(deviceIdArray[deviceIndex]);

            return setAudioDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK);
        }

        protected override bool SetAudioDevice(int deviceIndex)
        {
            if (deviceIndex < deviceIdArray.Length && deviceIndex >= 0)
                return setAudioDevice(deviceIdArray[deviceIndex]);

            return SetAudioDevice();
        }

        protected override bool IsCurrentDeviceValid() => baseManager.DeviceId > 0 && SDL_AudioDevicePaused(baseManager.DeviceId) == SDL_FALSE;

        internal override Track.Track GetNewTrack(Stream data, string name) => baseManager.GetNewTrack(data, name);

        internal override SampleFactory GetSampleFactory(Stream data, string name, AudioMixer mixer, int playbackConcurrency)
            => baseManager.GetSampleFactory(data, name, mixer, playbackConcurrency);

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            baseManager.Dispose();
        }

        /// <summary>
        /// To share basic playback logic with audio tests.
        /// </summary>
        internal unsafe class SDL3BaseAudioManager : IDisposable
        {
            internal SDL_AudioSpec AudioSpec { get; private set; }

            internal SDL_AudioDeviceID DeviceId { get; private set; }
            internal SDL_AudioStream* DeviceStream { get; private set; }

            internal int BufferSize { get; private set; } = (int)(AUDIO_FREQ * 0.01);

            internal string DeviceName { get; private set; } = "Not loaded";

            private readonly Func<IEnumerable<SDL3AudioMixer>> mixerIterator;

            private ObjectHandle<SDL3BaseAudioManager> objectHandle;

            private readonly SDL3AudioDecoderManager decoderManager = new SDL3AudioDecoderManager();

            internal SDL3BaseAudioManager(Func<IEnumerable<SDL3AudioMixer>> mixerIterator)
            {
                if (SDL_InitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO) == SDL_bool.SDL_FALSE)
                {
                    throw new InvalidOperationException($"Failed to initialise SDL Audio: {SDL_GetError()}");
                }

                this.mixerIterator = mixerIterator;

                objectHandle = new ObjectHandle<SDL3BaseAudioManager>(this, GCHandleType.Normal);
                AudioSpec = new SDL_AudioSpec
                {
                    freq = AUDIO_FREQ,
                    channels = AUDIO_CHANNELS,
                    format = AUDIO_FORMAT
                };
            }

            internal void RunWhileLockingAudioStream(Action action)
            {
                SDL_AudioStream* stream = DeviceStream;

                if (stream != null)
                    SDL_LockAudioStream(stream);

                try
                {
                    action();
                }
                finally
                {
                    if (stream != null)
                        SDL_UnlockAudioStream(stream);
                }
            }

            internal bool SetAudioDevice(SDL_AudioDeviceID targetId)
            {
                if (DeviceStream != null)
                {
                    SDL_DestroyAudioStream(DeviceStream);
                    DeviceStream = null;
                }

                SDL_AudioSpec spec = AudioSpec;

                SDL_AudioStream* deviceStream = SDL_OpenAudioDeviceStream(targetId, &spec, &audioCallback, objectHandle.Handle);

                if (deviceStream != null)
                {
                    SDL_DestroyAudioStream(DeviceStream);
                    DeviceStream = deviceStream;
                    AudioSpec = spec;

                    DeviceId = SDL_GetAudioStreamDevice(deviceStream);

                    int sampleFrameSize = 0;
                    SDL_AudioSpec temp; // this has 'real' device info which is useless since SDL converts audio according to the spec we provided
                    if (SDL_GetAudioDeviceFormat(DeviceId, &temp, &sampleFrameSize) == 0)
                        BufferSize = sampleFrameSize * (int)Math.Ceiling((double)spec.freq / temp.freq);
                }

                if (deviceStream == null)
                {
                    if (targetId == SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK)
                        return false;

                    return SetAudioDevice(SDL_AUDIO_DEVICE_DEFAULT_PLAYBACK);
                }

                SDL_ResumeAudioDevice(DeviceId);

                DeviceName = SDL_GetAudioDeviceName(targetId);

                return true;
            }

            [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
            private static void audioCallback(IntPtr userdata, SDL_AudioStream* stream, int additionalAmount, int totalAmount)
            {
                var handle = new ObjectHandle<SDL3BaseAudioManager>(userdata);
                if (handle.GetTarget(out SDL3BaseAudioManager audioManager))
                    audioManager.internalAudioCallback(stream, additionalAmount);
            }

            private float[] audioBuffer;

            private void internalAudioCallback(SDL_AudioStream* stream, int additionalAmount)
            {
                additionalAmount /= 4;

                if (audioBuffer == null || audioBuffer.Length < additionalAmount)
                    audioBuffer = new float[additionalAmount];

                Array.Fill(audioBuffer, 0);

                try
                {
                    foreach (var mixer in mixerIterator())
                    {
                        if (mixer.IsAlive)
                            mixer.MixChannelsInto(audioBuffer, additionalAmount);
                    }

                    fixed (float* ptr = audioBuffer)
                        SDL_PutAudioStreamData(stream, (IntPtr)ptr, additionalAmount * 4);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error while pushing audio to SDL");
                }
            }

            /// <summary>
            /// With how decoders work, we need this to get test passed
            /// I don't want this either... otherwise we have to dispose decoder in tests
            /// </summary>
            private class ReceiverGCWrapper : SDL3AudioDecoderManager.ISDL3AudioDataReceiver
            {
                private readonly WeakReference<SDL3AudioDecoderManager.ISDL3AudioDataReceiver> channelWeakReference;

                internal ReceiverGCWrapper(WeakReference<SDL3AudioDecoderManager.ISDL3AudioDataReceiver> channel)
                {
                    channelWeakReference = channel;
                }

                void SDL3AudioDecoderManager.ISDL3AudioDataReceiver.GetData(byte[] data, int length, bool done)
                {
                    if (channelWeakReference.TryGetTarget(out SDL3AudioDecoderManager.ISDL3AudioDataReceiver r))
                        r.GetData(data, length, done);
                    else
                        throw new ObjectDisposedException("channel is already disposed");
                }

                void SDL3AudioDecoderManager.ISDL3AudioDataReceiver.GetMetaData(int bitrate, double length, long byteLength)
                {
                    if (channelWeakReference.TryGetTarget(out SDL3AudioDecoderManager.ISDL3AudioDataReceiver r))
                        r.GetMetaData(bitrate, length, byteLength);
                    else
                        throw new ObjectDisposedException("channel is already disposed");
                }
            }

            internal Track.Track GetNewTrack(Stream data, string name)
            {
                TrackSDL3 track = new TrackSDL3(name, AudioSpec, BufferSize);
                ReceiverGCWrapper receiverGC = new ReceiverGCWrapper(new WeakReference<SDL3AudioDecoderManager.ISDL3AudioDataReceiver>(track));
                decoderManager.StartDecodingAsync(data, AudioSpec, true, receiverGC);
                return track;
            }

            internal SampleFactory GetSampleFactory(Stream data, string name, AudioMixer mixer, int playbackConcurrency)
            {
                SampleSDL3Factory sampleFactory = new SampleSDL3Factory(name, (SDL3AudioMixer)mixer, playbackConcurrency, AudioSpec);
                ReceiverGCWrapper receiverGC = new ReceiverGCWrapper(new WeakReference<SDL3AudioDecoderManager.ISDL3AudioDataReceiver>(sampleFactory));
                decoderManager.StartDecodingAsync(data, AudioSpec, false, receiverGC);
                return sampleFactory;
            }

            public void Dispose()
            {
                if (DeviceStream != null)
                {
                    SDL_DestroyAudioStream(DeviceStream);
                    DeviceStream = null;
                    DeviceId = 0;
                    // Destroying audio stream will close audio device because we use SDL3 OpenAudioDeviceStream
                    // won't use multiple AudioStream for now since it's barely useful
                }

                objectHandle.Dispose();
                decoderManager.Dispose();

                SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_AUDIO);
            }
        }
    }
}
