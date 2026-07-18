// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JetBrains.Annotations;
using mysoundlib_cs;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.SDL3;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Platform.Linux.Native;
using osu.Framework.Threading;
using SDL;

namespace osu.Framework.Audio
{
    public class SDL3AudioManager : AudioManager
    {
        #region Unsafe Library Handling

        private static volatile bool libraryPrepared;

        private static readonly object syncLock = new object();

        internal static unsafe void PrepareLibrary()
        {
            if (libraryPrepared)
                return;

            lock (syncLock)
            {
                if (libraryPrepared)
                    return;

                if (ManagedBass.Bass.CurrentDevice < 0)
                    ManagedBass.Bass.Init(ManagedBass.Bass.NoSoundDevice);

                if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
                {
                    // is this even needed?
                    Library.Load("libmysoundlib.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
                }

                MySoundLibrary.mslSetLogFunction(&logOutput);
                MySoundLibrary.mslSetLibraryLoadFunctions(&loadNativeLib, &loadExportedSym, &closeNativeLib);

                libraryPrepared = true;
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe void logOutput(byte* msg)
        {
            Logger.Log($"🔈 {SDL3.PtrToStringUTF8(msg)}");
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe nint loadNativeLib(byte* name)
        {
            string? strname = Marshal.PtrToStringAnsi((IntPtr)name);

            if (strname == null)
            {
                Logger.Log("Couldn't convert library name");
                return IntPtr.Zero;
            }

            if (RuntimeInfo.OS == RuntimeInfo.Platform.iOS)
                strname = $"@rpath/{strname}.framework/{strname}";

            if (NativeLibrary.TryLoad(strname, RuntimeInfo.EntryAssembly,
                    DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.SafeDirectories, out IntPtr lib))
            {
                return lib;
            }
            else
            {
                // those symbols may exist if libraries are statically compiled
                Logger.Log("Couldn't load a library: " + strname);
                return NativeLibrary.GetMainProgramHandle();
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static unsafe nint loadExportedSym(nint handle, byte* name)
        {
            string? strname = Marshal.PtrToStringAnsi((IntPtr)name);

            if (strname == null)
            {
                Logger.Log("Couldn't convert symbol name");
                return IntPtr.Zero;
            }

            if (NativeLibrary.TryGetExport(handle, strname, out nint exportHandle))
            {
                return exportHandle;
            }
            else
            {
                Logger.Log("Couldn't load a symbol: " + strname);
                return IntPtr.Zero;
            }
        }

        [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
        private static void closeNativeLib(nint handle)
        {
            if (handle != NativeLibrary.GetMainProgramHandle())
                NativeLibrary.Free(handle);
        }

        #endregion

        public static readonly SDL_AudioSpec AUDIO_SPEC;

        private Scheduler eventScheduler => EventScheduler ?? CurrentAudioThread.Scheduler;

        static SDL3AudioManager()
        {
            PrepareLibrary();
            MySoundLibrary.mslGetAudioFormat(out int freq, out int channels, out var format);

            AUDIO_SPEC = new SDL_AudioSpec
            {
                freq = freq,
                channels = channels,
                format = (SDL_AudioFormat)format
            };
        }

        private volatile IntPtr handle;

        /// <summary>
        /// Creates a new <see cref="SDL3AudioManager"/>.
        /// </summary>
        /// <param name="audioThread">The host's audio thread.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        /// <param name="config"></param>
        public SDL3AudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore, [CanBeNull] FrameworkConfigManager config)
            : base(audioThread, trackStore, sampleStore, config)
        {
            // handle gets initialized through Prepare in base(). Make sure that it is created.
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("Failed to create an audio manager");

            syncAudioDevices();
            AudioScheduler.AddOnce(InitCurrentDevice);
        }

        protected override void Prepare()
        {
            handle = MySoundLibrary.mslCreateAudioManager();
        }

        // Unlike BassAudioManager, it is only done once.
        private unsafe void syncAudioDevices()
        {
            List<string> deviceNames = new List<string>();
            byte** list = MySoundLibrary.mslGetAudioDeviceList(handle);
            if (list == null)
                return;

            try
            {
                for (byte** entry = list; *entry != null; entry++)
                    deviceNames.Add(SDL3.PtrToStringUTF8(*entry) ?? "");
            }
            finally
            {
                MySoundLibrary.mslFreeAudioDeviceList(list);
            }

            DeviceNames = deviceNames.ToImmutableList();

            if (deviceNames.Count > 0)
            {
                eventScheduler.Add(() =>
                {
                    foreach (string name in deviceNames)
                        InvokeOnNewDevice(name);
                });
            }
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

            if (IsDisposed)
                return;

            // check if handle is null to avoid dumb mistakes where it gets called earlier than prepare.
            if (item is SDL3AudioMixer mixer)
                MySoundLibrary.mslAddMixer(handle.ThrowIfNull(), mixer.Handle);
        }

        protected override void ItemRemoved(AudioComponent item)
        {
            base.ItemRemoved(item);

            if (IsDisposed)
                return;

            if (item is SDL3AudioMixer mixer)
                MySoundLibrary.mslRemoveMixer(handle.ThrowIfNull(), mixer.Handle);
        }

        private bool inited;

        private bool manualHint;

        protected override void InitCurrentDevice()
        {
            string deviceName = AudioDevice.Value;

            byte[]? name = deviceName == null ? null : Encoding.UTF8.GetBytes(deviceName + '\0');

            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
            {
                // Only manually set a value if the user has not set the hint
                if (manualHint || SDL3.SDL_GetHint(SDL3.SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES) == null)
                {
                    manualHint = true;

                    if (UseExperimentalWasapi.Value)
                    {
                        SDL3.SDL_SetHint(SDL3.SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES, "1"u8);
                        Logger.Log("Trying low latency WASAPI");
                    }
                    else
                    {
                        SDL3.SDL_ResetHint(SDL3.SDL_HINT_AUDIO_DEVICE_SAMPLE_FRAMES);
                    }
                }
            }

            unsafe
            {
                fixed (byte* ptr = name)
                {
                    if (!MySoundLibrary.mslOpenAudioDevice(handle, ptr).ToBool())
                    {
                        // It automatically falls back to the default device if provided one is not available.
                        // So there is no need to try again. It's doomed if it failed.
                        Logger.Log("Audio device cannot be used! Check your audio system.", level: LogLevel.Error);
                        return;
                    }
                }
            }

            inited = true;
            currentDeviceName = "loaded";
        }

        protected override bool IsCurrentDeviceValid() => inited;

        internal override Track.Track GetNewTrack(Stream data, string name) => new TrackSDL3(data, name);

        internal override SampleFactory GetSampleFactory(Stream data, string name, AudioMixer mixer, int playbackConcurrency)
            => new SampleSDL3Factory(data, name, (SDL3AudioMixer)mixer, playbackConcurrency);

        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            base.Dispose(disposing);

            MySoundLibrary.mslDestroyAudioManager(handle);
            handle = IntPtr.Zero;
        }
    }
}
