// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using ManagedBass;
using ManagedBass.Fx;
using ManagedBass.Mix;
using osu.Framework.Audio.Asio;
using osu.Framework.Audio.Host;
using osu.Framework.Audio.EzLatency;
using osu.Framework.Audio.Mixing;
using osu.Framework.Audio.Mixing.Bass;
using osu.Framework.Audio.Mixing.Wasapi;
using osu.Framework.Audio.Sample;
using osu.Framework.Audio.Track;
using osu.Framework.Audio.Wasapi;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Development;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.IO.Stores;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Audio
{
    public class AudioManager : AudioCollectionManager<AudioComponent>
    {
        /// <summary>
        /// The number of BASS audio devices preceding the first real audio device.
        /// Consisting of <see cref="Bass.NoSoundDevice"/> and <see cref="bass_default_device"/>.
        /// </summary>
        protected const int BASS_INTERNAL_DEVICE_COUNT = 2;

        /// <summary>
        /// The index of the BASS audio device denoting the OS default.
        /// </summary>
        /// <remarks>
        /// See http://www.un4seen.com/doc/#bass/BASS_CONFIG_DEV_DEFAULT.html for more information on the included device.
        /// </remarks>
        private const int bass_default_device = 1;

        /// <summary>
        /// The manager component responsible for audio tracks (e.g. songs).
        /// </summary>
        public ITrackStore Tracks => globalTrackStore.Value;

        /// <summary>
        /// The manager component responsible for audio samples (e.g. sound effects).
        /// </summary>
        public ISampleStore Samples => globalSampleStore.Value;

        /// <summary>
        /// The thread audio operations (mainly Bass calls) are ran on.
        /// </summary>
        private readonly AudioThread thread;

        // Optional audio backend (non-null when using the new Windows WASAPI backend prototype).
        [CanBeNull]
        private readonly IAudioBackend audioBackend;

        /// <summary>
        /// The global mixer which all tracks are routed into by default.
        /// </summary>
        public readonly AudioMixer TrackMixer;

        /// <summary>
        /// The global mixer which all samples are routed into by default.
        /// </summary>
        public readonly AudioMixer SampleMixer;

        /// <summary>
        /// Sample rate used for ASIO device initialisation and runtime changes.
        /// </summary>
        public readonly Bindable<int> SampleRate = new Bindable<int>(AudioOutputDefaults.DEFAULT_SAMPLE_RATE);

        /// <summary>
        /// ASIO buffer size (default 128) used during device initialisation.
        /// </summary>
        public readonly Bindable<int> AsioBufferSize = new Bindable<int>(AudioOutputDefaults.DEFAULT_ASIO_BUFFER_SIZE);

        /// <summary>
        /// ASIO output bit depth (16 or 24). Default is 24.
        /// </summary>
        public readonly Bindable<int> AsioBitDepth = new Bindable<int>(24);

        /// <summary>
        /// When enabled, ASIO pass-through bypasses manual format settings and uses the driver's native format.
        /// </summary>
        public readonly BindableBool AsioPassThrough = new BindableBool(false);

        /// <summary>
        /// The names of all available audio devices.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This property does not contain the names of disabled audio devices.
        /// </para>
        /// <para>
        /// This property may also not necessarily contain the name of the default audio device provided by the OS.
        /// Consumers should provide a "Default" audio device entry which sets <see cref="AudioDevice"/> to an empty string.
        /// </para>
        /// </remarks>
        public IEnumerable<string> AudioDeviceNames => getAudioDeviceEntries();

        /// <summary>
        /// Is fired whenever a new audio device is discovered and provides its name.
        /// </summary>
        public event Action<string> OnNewDevice;

        /// <summary>
        /// Is fired whenever an audio device is lost and provides its name.
        /// </summary>
        public event Action<string> OnLostDevice;

        /// <summary>
        /// Invoked after an ASIO device is initialised successfully, with the effective sample rate.
        /// </summary>
        [CanBeNull]
        public Action<double> OnAsioDeviceInitialized;

        /// <summary>
        /// Invoked after an ASIO device is initialised successfully, with sample rate, buffer size, and bit depth.
        /// </summary>
        [CanBeNull]
        public Action<double, int, int> OnAsioDeviceConfigurationChanged;

        /// <summary>
        /// The preferred audio device we should use. A value of
        /// <see cref="string.Empty"/> denotes the OS default.
        /// </summary>
        public readonly Bindable<string> AudioDevice = new Bindable<string>();

        /// <summary>
        /// Whether to use experimental WASAPI initialisation on windows.
        /// This generally results in lower audio latency, but also changes the audio synchronisation from
        /// historical expectations, meaning users / application will have to account for different offsets.
        /// </summary>
        public readonly BindableBool UseExperimentalWasapi = new BindableBool();

        /// <summary>
        /// Volume of all samples played game-wide.
        /// </summary>
        public readonly BindableDouble VolumeSample = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        /// <summary>
        /// Volume of all tracks played game-wide.
        /// </summary>
        public readonly BindableDouble VolumeTrack = new BindableDouble(1)
        {
            MinValue = 0,
            MaxValue = 1
        };

        /// <summary>
        /// Whether a global mixer is being used for audio routing.
        /// For now, this is only the case on Windows when using shared mode WASAPI initialisation.
        /// </summary>
        public IBindable<bool> UsingGlobalMixer => usingGlobalMixer;

        private readonly Bindable<bool> usingGlobalMixer = new BindableBool();

        /// <summary>
        /// If a global mixer is being used, this will be the BASS handle for it.
        /// If non-null, all game mixers should be added to this mixer.
        /// </summary>
        /// <remarks>
        /// When this is non-null, all mixers created via <see cref="CreateAudioMixer"/>
        /// will themselves be added to the global mixer, which will handle playback itself.
        ///
        /// In this mode of operation, nested mixers will be created with the <see cref="BassFlags.Decode"/>
        /// flag, meaning they no longer handle playback directly.
        ///
        /// An eventual goal would be to use a global mixer across all platforms as it can result
        /// in more control and better playback performance.
        /// </remarks>
        internal readonly IBindable<int?> GlobalMixerHandle = new Bindable<int?>();

        public override bool IsLoaded => base.IsLoaded &&
                                         // bass default device is a null device (-1), not the actual system default.
                                         Bass.CurrentDevice != Bass.DefaultDevice;

        // Mutated by multiple threads, must be thread safe.
        private ImmutableArray<DeviceInfo> audioDevices = ImmutableArray<DeviceInfo>.Empty;
        private ImmutableList<string> audioDeviceNames = ImmutableList<string>.Empty;
        private ImmutableList<string> previousAsioDeviceNames = ImmutableList<string>.Empty;

        private static int asioNativeUnavailableLogged;

        private const string legacy_type_bass = "BASS";
        private const string legacy_type_wasapi_shared = "WASAPI Shared";
        private const string type_wasapi_exclusive = "WASAPI Exclusive";
        private const string type_asio = "ASIO";

        private bool syncingSelection;

        private void setUserBindableValueLeaseSafe<T>(Bindable<T> bindable, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(bindable.Value, newValue))
                return;

            // These bindables are bound into UI (osu!) and can trigger transforms/animations.
            // Ensure mutations happen on the update thread (Game.Scheduler) to avoid cross-thread Drawable mutations.
            if (ThreadSafety.IsUpdateThread || EventScheduler == null)
            {
                setBindableValueLeaseSafe(bindable, newValue);
                return;
            }

            eventScheduler.Add(() => setBindableValueLeaseSafe(bindable, newValue));
        }

        private static void setBindableValueLeaseSafe<T>(Bindable<T> bindable, T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(bindable.Value, newValue))
                return;

            // Bindables may be in a leased state (Disabled=true), in which case Value setter throws.
            // We still want internal state/config to reflect the effective output fallback.
            if (bindable.Disabled)
                bindable.SetValue(bindable.Value, newValue, true);
            else
                bindable.Value = newValue;
        }

        private Scheduler scheduler => thread.Scheduler;

        private Scheduler eventScheduler => EventScheduler ?? scheduler;

        private readonly CancellationTokenSource cancelSource = new CancellationTokenSource();

        /// <summary>
        /// The scheduler used for invoking publicly exposed delegate events.
        /// </summary>
        public Scheduler EventScheduler;

        internal IBindableList<AudioMixer> ActiveMixers => activeMixers;
        private readonly BindableList<AudioMixer> activeMixers = new BindableList<AudioMixer>();

        private readonly Lazy<TrackStore> globalTrackStore;
        private readonly Lazy<SampleStore> globalSampleStore;

        /// <summary>
        /// Sets the preferred ASIO sample rate.
        /// </summary>
        /// <param name="sampleRate">The preferred sample rate in Hz.</param>
        public void SetPreferredAsioSampleRate(int sampleRate)
        {
            SampleRate.Value = sampleRate > 0 ? sampleRate : AudioOutputDefaults.DEFAULT_SAMPLE_RATE;
        }

        /// <summary>
        /// Sets the preferred ASIO buffer size.
        /// </summary>
        /// <param name="bufferSize">The preferred buffer size for ASIO device.</param>
        public void SetAsioBufferSize(int bufferSize)
        {
            AsioBufferSize.Value = bufferSize > 0 ? bufferSize : AudioOutputDefaults.DEFAULT_ASIO_BUFFER_SIZE;
        }

        /// <summary>
        /// Sets the preferred ASIO sample rate and bit depth.
        /// </summary>
        public void SetAsioFormat(int sampleRate, int bitDepth)
        {
            SampleRate.Value = sampleRate > 0 ? sampleRate : AudioOutputDefaults.DEFAULT_SAMPLE_RATE;
            AsioBitDepth.Value = bitDepth is 16 or 24 ? bitDepth : 24;
        }

        /// <summary>
        /// Sets whether ASIO pass-through mode is enabled.
        /// </summary>
        public void SetAsioPassThrough(bool enabled) => AsioPassThrough.Value = enabled;

        /// <summary>
        /// Returns supported sample-rate/bit-depth combinations for the given ASIO device.
        /// </summary>
        public IReadOnlyList<EzAsioFormatOption> GetAsioSupportedFormats(string asioDeviceName)
        {
            int? index = EzAsioDeviceManager.FindAsioDeviceIndex(asioDeviceName);

            if (!index.HasValue)
                return Array.Empty<EzAsioFormatOption>();

            var bassNames = audioDeviceNames;

            return EzAsioDeviceManager.GetSupportedFormats(index.Value, asioDeviceName, bassNames);
        }

        /// <summary>
        /// Returns supported buffer sizes for the given ASIO device (from driver min/max/granularity).
        /// </summary>
        public IReadOnlyList<int> GetAsioSupportedBufferSizes(string asioDeviceName)
        {
            int? index = EzAsioDeviceManager.FindAsioDeviceIndex(asioDeviceName);

            if (!index.HasValue)
                return Array.Empty<int>();

            return EzAsioDeviceManager.GetSupportedBufferSizes(index.Value);
        }

        /// <summary>
        /// Configures ASIO device name substrings that trigger host audio warm-up before ASIO initialisation (virtual bridge drivers).
        /// </summary>
        public void ConfigureAsioVirtualHostWarmUpNamePatterns(IEnumerable<string> patterns)
            => EzAsioDeviceManager.SetVirtualHostWarmUpNamePatterns(patterns);

        /// <summary>
        /// Refreshes cached ASIO format/buffer lists on the audio thread (safe driver metadata read).
        /// </summary>
        public void RequestAsioCapabilitiesRefresh(string asioDeviceName, Action onComplete = null)
        {
            int? index = EzAsioDeviceManager.FindAsioDeviceIndex(asioDeviceName);

            if (!index.HasValue)
            {
                onComplete?.Invoke();
                return;
            }

            int deviceIndex = index.Value;

            scheduler.Add(() =>
            {
                EzAsioDeviceManager.TryPopulateCapabilitiesCache(deviceIndex);

                if (onComplete != null)
                    eventScheduler.Add(onComplete);
            });
        }

        /// <summary>
        /// Whether the current selection is ASIO and the driver is started.
        /// </summary>
        public bool IsAsioOutputActive()
        {
            if (parseSelection(AudioDevice.Value).mode != AudioOutputMode.Asio)
                return false;

            try
            {
                return EzAsioDeviceManager.IsDeviceRunning();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// User-facing ASIO status for settings UI: summary (output state) and capabilities (driver probe).
        /// </summary>
        public readonly record struct AsioStatusNote(string SummaryLine, string CapabilitiesLine)
        {
            /// <summary>
            /// Formats the note for display (one or two lines).
            /// </summary>
            public string ToDisplayText() => string.IsNullOrEmpty(CapabilitiesLine)
                ? SummaryLine
                : $"{SummaryLine}\n{CapabilitiesLine}";
        }

        /// <summary>
        /// Returns two-line ASIO status for settings UI.
        /// </summary>
        public AsioStatusNote GetAsioStatusNote(string asioDeviceName)
        {
            int? index = EzAsioDeviceManager.FindAsioDeviceIndex(asioDeviceName);

            if (!index.HasValue)
                return new AsioStatusNote($"ASIO device \"{asioDeviceName}\" was not found.", string.Empty);

            var formats = EzAsioDeviceManager.GetSupportedFormats(index.Value, asioDeviceName, audioDeviceNames);
            var buffers = EzAsioDeviceManager.GetSupportedBufferSizes(index.Value);

            bool isCurrentSelectedAsio = parseSelection(AudioDevice.Value).mode == AudioOutputMode.Asio
                                         && string.Equals(parseSelection(AudioDevice.Value).deviceName, asioDeviceName, StringComparison.Ordinal);
            bool running = isCurrentSelectedAsio && EzAsioDeviceManager.IsDeviceRunning();
            bool passThrough = AsioPassThrough.Value;

            string summaryLine = buildAsioSummaryLine(running, passThrough);
            string capabilitiesLine = buildAsioCapabilitiesLine(index.Value, formats, buffers);

            return new AsioStatusNote(summaryLine, capabilitiesLine);
        }

        // 设置页状态 Note：第一行描述当前输出状态
        private string buildAsioSummaryLine(bool running, bool passThrough)
        {
            string passThroughText = passThrough ? "pass-through on" : "pass-through off";

            if (!running)
            {
                if (passThrough)
                    return $"Output: not running · {passThroughText} · will use driver native format";

                return $"Output: not running · {passThroughText} · configured {SampleRate.Value} Hz / {AsioBitDepth.Value} bit / buffer {AsioBufferSize.Value}";
            }

            double currentRate = EzAsioDeviceManager.GetCurrentSampleRate();
            int bitDepth = EzAsioDeviceManager.TargetBitDepth;
            int buffer = EzAsioDeviceManager.ActiveBufferSize;
            var info = EzAsioDeviceManager.GetCurrentDeviceInfo();
            int outputs = info?.Outputs ?? 0;
            bool routingActive = EzAsioDeviceManager.IsOutputRoutingActive();

            string rateText = currentRate > 0 ? $"{(int)Math.Round(currentRate)} Hz" : "sample rate unknown";
            string bufferText = buffer > 0 ? $"buffer {buffer}" : "buffer unknown";
            string routingText = routingActive ? "routing active" : "routing inactive";

            return $"Output: running · {rateText} / {bitDepth} bit · {bufferText} · {outputs} ch · {routingText} · {passThroughText}";
        }

        // 设置页状态 Note：第二行描述驱动探测到的格式与缓冲能力
        private static string buildAsioCapabilitiesLine(int deviceIndex, IReadOnlyList<EzAsioFormatOption> formats, IReadOnlyList<int> buffers)
        {
            const int max_formats_shown = 6;

            string formatSegment;

            if (formats.Count == 0)
            {
                formatSegment = "Formats: no probe data yet";
            }
            else
            {
                var shown = formats.Take(max_formats_shown).Select(f => $"{f.SampleRate}/{f.BitDepth}");
                string formatList = string.Join(", ", shown);

                formatSegment = formats.Count > max_formats_shown ? $"Formats: {formatList}… ({formats.Count} total)" : $"Formats: {formatList} ({formats.Count} total)";
            }

            string bufferSegment = buildBufferCapabilitiesSegment(deviceIndex, buffers);

            if (string.IsNullOrEmpty(bufferSegment))
                return $"Driver probe · {formatSegment}";

            return $"Driver probe · {formatSegment} · {bufferSegment}";
        }

        private static string buildBufferCapabilitiesSegment(int deviceIndex, IReadOnlyList<int> buffers)
        {
            if (EzAsioDeviceManager.TryGetCachedBufferParameters(deviceIndex, out int min, out int preferred, out int max, out int granularity))
            {
                string rangeText = min > 0 && max > 0 ? $"{min}–{max}" : buffers.Count > 0 ? $"{buffers.First()}–{buffers.Last()}" : "unknown range";
                string preferredText = preferred > 0 ? $"preferred {preferred}" : string.Empty;
                string granularityText = describeBufferGranularity(granularity);
                string countText = buffers.Count > 0 ? $"{buffers.Count} values" : string.Empty;

                var parts = new List<string> { $"Buffer {rangeText}" };

                if (!string.IsNullOrEmpty(preferredText))
                    parts.Add(preferredText);

                if (!string.IsNullOrEmpty(granularityText))
                    parts.Add(granularityText);

                if (!string.IsNullOrEmpty(countText))
                    parts.Add(countText);

                return string.Join(", ", parts);
            }

            if (buffers.Count == 0)
                return "Buffer: no probe data yet";

            if (buffers.Count == 1)
                return $"Buffer {buffers[0]} (1 value)";

            return $"Buffer {buffers.First()}–{buffers.Last()} ({buffers.Count} values)";
        }

        private static string describeBufferGranularity(int granularity) => granularity switch
        {
            0 => "granularity: min/pref/max only",
            < 0 => "granularity: powers of 2",
            _ => $"granularity: +{granularity}",
        };

        /// <summary>
        /// Constructs an AudioStore given a track resource store, and a sample resource store.
        /// </summary>
        /// <param name="audioThread">The host's audio thread.</param>
        /// <param name="trackStore">The resource store containing all audio tracks to be used in the future.</param>
        /// <param name="sampleStore">The sample store containing all audio samples to be used in the future.</param>
        /// <param name="config"></param>
        public AudioManager(AudioThread audioThread, ResourceStore<byte[]> trackStore, ResourceStore<byte[]> sampleStore, [CanBeNull] FrameworkConfigManager config)
        {
            thread = audioThread;

            // Initialise optional WASAPI backend on Windows (prototype).
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
            {
                try
                {
                    // Pass a provider to allow the backend to read the current global mixer handle when available.
                    audioBackend = new WasapiAudioBackend(() => GlobalMixerHandle.Value);
                }
                catch
                {
                    audioBackend = null;
                }
            }

            thread.RegisterManager(this);

            if (config != null)
            {
                // attach config bindables
                config.BindWith(FrameworkSetting.AudioDevice, AudioDevice);
                config.BindWith(FrameworkSetting.AudioUseExperimentalWasapi, UseExperimentalWasapi);
                config.BindWith(FrameworkSetting.VolumeUniversal, Volume);
                config.BindWith(FrameworkSetting.VolumeEffect, VolumeSample);
                config.BindWith(FrameworkSetting.VolumeMusic, VolumeTrack);
            }

            AudioDevice.ValueChanged += _ =>
            {
                if (syncingSelection)
                    return;

                scheduler.AddOnce(initCurrentDevice);
            };
            UseExperimentalWasapi.ValueChanged += e =>
            {
                if (syncingSelection)
                    return;

                // When enabling experimental(shared) mode, ASIO/Exclusive selections are incompatible — coerce to a safe value.
                if (e.NewValue && RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
                {
                    string selection = AudioDevice.Value;

                    if (tryParseSuffixed(selection, type_wasapi_exclusive, out string baseName))
                    {
                        syncingSelection = true;

                        try
                        {
                            setBindableValueLeaseSafe(AudioDevice, baseName);
                        }
                        finally
                        {
                            syncingSelection = false;
                        }
                    }
                    else if (tryParseSuffixed(selection, type_asio, out _))
                    {
                        syncingSelection = true;

                        try
                        {
                            setBindableValueLeaseSafe(AudioDevice, string.Empty);
                        }
                        finally
                        {
                            syncingSelection = false;
                        }
                    }
                }

                // Shared-mode WASAPI is still controlled by this checkbox.
                // Keep dropdown values as the raw BASS device name to preserve historical UX.
                scheduler.AddOnce(initCurrentDevice);
            };
            // initCurrentDevice not required for changes to `GlobalMixerHandle` as it is only changed when experimental wasapi is toggled (handled above).
            GlobalMixerHandle.ValueChanged += handle => usingGlobalMixer.Value = handle.NewValue.HasValue;

            // Listen for unified sample rate changes and reinitialize ASIO immediately.
            SampleRate.ValueChanged += e =>
            {
                requestAsioReinitialisation($"Sample rate changed to {e.NewValue}Hz");
            };

            // Listen for ASIO buffer size changes and reinitialize ASIO immediately.
            AsioBufferSize.ValueChanged += e =>
            {
                requestAsioReinitialisation($"ASIO buffer size changed to {e.NewValue}");
            };

            AsioBitDepth.ValueChanged += e =>
            {
                requestAsioReinitialisation($"ASIO bit depth changed to {e.NewValue}");
            };

            AsioPassThrough.ValueChanged += e =>
            {
                requestAsioReinitialisation($"ASIO pass-through changed to {e.NewValue}");
            };

            AddItem(TrackMixer = createAudioMixer(null, nameof(TrackMixer)));
            AddItem(SampleMixer = createAudioMixer(null, nameof(SampleMixer)));

            globalTrackStore = new Lazy<TrackStore>(() =>
            {
                var store = new TrackStore(trackStore, TrackMixer);
                AddItem(store);
                store.AddAdjustment(AdjustableProperty.Volume, VolumeTrack);
                return store;
            });

            globalSampleStore = new Lazy<SampleStore>(() =>
            {
                var store = new SampleStore(sampleStore, SampleMixer);
                AddItem(store);
                store.AddAdjustment(AdjustableProperty.Volume, VolumeSample);
                return store;
            });

            syncAudioDevices();

            // check for changes in any audio devices every 1000ms (slightly expensive operation)
            CancellationToken token = cancelSource.Token;
            scheduler.AddDelayed(() =>
            {
                new Thread(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            if (CheckForDeviceChanges(audioDevices))
                                syncAudioDevices();
                            Thread.Sleep(1000);
                        }
                        catch
                        {
                        }
                    }
                })
                {
                    IsBackground = true
                }.Start();
            }, 1000);
        }

        protected override void Dispose(bool disposing)
        {
            cancelSource.Cancel();

            thread.UnregisterManager(this);

            OnNewDevice = null;
            OnLostDevice = null;

            base.Dispose(disposing);
        }

        private static int userMixerID;

        /// <summary>
        /// Creates a new <see cref="AudioMixer"/>.
        /// </summary>
        /// <remarks>
        /// Channels removed from this <see cref="AudioMixer"/> fall back to the global <see cref="SampleMixer"/>.
        /// </remarks>
        /// <param name="identifier">An identifier displayed on the audio mixer visualiser.</param>
        public AudioMixer CreateAudioMixer(string identifier = null) =>
            createAudioMixer(SampleMixer, !string.IsNullOrEmpty(identifier) ? identifier : $"user #{Interlocked.Increment(ref userMixerID)}");

        private AudioMixer createAudioMixer(AudioMixer fallbackMixer, string identifier)
        {
            // Only use the experimental WASAPI mixer when the prototype backend exists
            // and the user has explicitly enabled experimental WASAPI.
            if (audioBackend != null && UseExperimentalWasapi.Value)
            {
                var wasapiMixer = new WasapiAudioMixer(audioBackend, fallbackMixer, identifier);
                AddItem(wasapiMixer);
                return wasapiMixer;
            }

            var bassMixer = new BassAudioMixer(this, fallbackMixer, identifier);
            AddItem(bassMixer);
            return bassMixer;
        }

        protected override void ItemAdded(AudioComponent item)
        {
            base.ItemAdded(item);
            if (item is AudioMixer mixer)
                activeMixers.Add(mixer);
        }

        protected override void ItemRemoved(AudioComponent item)
        {
            base.ItemRemoved(item);
            if (item is AudioMixer mixer)
                activeMixers.Remove(mixer);
        }

        /// <summary>
        /// Obtains the <see cref="TrackStore"/> corresponding to a given resource store.
        /// Returns the global <see cref="TrackStore"/> if no resource store is passed.
        /// </summary>
        /// <param name="store">The <see cref="IResourceStore{T}"/> of which to retrieve the <see cref="TrackStore"/>.</param>
        /// <param name="mixer">The <see cref="AudioMixer"/> to use for tracks created by this store. Defaults to the global <see cref="TrackMixer"/>.</param>
        public ITrackStore GetTrackStore(IResourceStore<byte[]> store = null, AudioMixer mixer = null)
        {
            if (store == null) return globalTrackStore.Value;

            TrackStore tm = new TrackStore(store, mixer ?? TrackMixer);
            globalTrackStore.Value.AddItem(tm);
            return tm;
        }

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
        /// <param name="mixer">The <see cref="AudioMixer"/> to use for samples created by this store. Defaults to the global <see cref="SampleMixer"/>.</param>
        public ISampleStore GetSampleStore(IResourceStore<byte[]> store = null, AudioMixer mixer = null)
        {
            if (store == null) return globalSampleStore.Value;

            SampleStore sm = new SampleStore(store, mixer ?? SampleMixer);
            globalSampleStore.Value.AddItem(sm);
            return sm;
        }

        /// <summary>
        /// (Re-)Initialises BASS for the current <see cref="AudioDevice"/>.
        /// This will automatically fall back to the system default device on failure.
        /// </summary>
        private void initCurrentDevice()
        {
            // Note: normalisation may write back to bindables; ensure those writes are update-thread-safe.
            normaliseLegacySelection();

            var (mode, deviceName) = parseSelection(AudioDevice.Value);

            bool isExplicitSelection = !string.IsNullOrEmpty(AudioDevice.Value);
            // bool isTypedSelection = hasTypeSuffix(AudioDevice.Value);

            // keep legacy setting and dropdown selection in sync.
            if (!syncingSelection)
            {
                syncingSelection = true;

                try
                {
                    // Option (1): experimental(shared) mode hides Exclusive/ASIO entries.
                    // If we still see these modes (eg. from config), force experimental off to keep behaviour consistent.
                    if (UseExperimentalWasapi.Value && (mode == AudioOutputMode.WasapiExclusive || mode == AudioOutputMode.Asio))
                        setUserBindableValueLeaseSafe(UseExperimentalWasapi, false);
                }
                finally
                {
                    syncingSelection = false;
                }
            }

            // try using the specified device
            if (mode == AudioOutputMode.Asio)
            {
                // Resolve the display name to a BassAsio device index.
                int? asioDeviceIndex = EzAsioDeviceManager.FindAsioDeviceIndex(deviceName);

                if (asioDeviceIndex.HasValue)
                {
                    // ASIO output still requires BASS to be initialised (mixer source), but playback is via BassAsio.
                    // Prefer the host playback device that matches this ASIO driver name.
                    int bassDeviceId = bass_default_device;

                    int? matchedListIndex = HostDeviceMatcher.FindBassPlaybackDeviceListIndex(deviceName, audioDeviceNames);

                    if (matchedListIndex.HasValue)
                        bassDeviceId = BASS_INTERNAL_DEVICE_COUNT + matchedListIndex.Value;

                    if (trySetDevice(bassDeviceId, mode))
                    {
                        RequestAsioCapabilitiesRefresh(deviceName);
                        return;
                    }

                    Logger.Log($"ASIO device '{deviceName}' initialization failed.", name: "audio", level: LogLevel.Important);
                }
                else
                {
                    Logger.Log($"ASIO device '{deviceName}' not found.", name: "audio", level: LogLevel.Error);
                }

                goto explicit_selection_failed;
            }
            else
            {
                // BASS internal devices (eg. "No sound") are not listed in audioDeviceNames.
                for (int i = 0; i < BASS_INTERNAL_DEVICE_COUNT; i++)
                {
                    if (!string.Equals(audioDevices[i].Name, deviceName, StringComparison.Ordinal))
                        continue;

                    var internalMode = i == Bass.NoSoundDevice ? AudioOutputMode.Default : mode;
                    if (trySetDevice(i, internalMode))
                        return;

                    break;
                }

                // try using the specified device
                int deviceIndex = audioDeviceNames.FindIndex(d => d == deviceName);
                if (deviceIndex >= 0 && trySetDevice(BASS_INTERNAL_DEVICE_COUNT + deviceIndex, mode)) return;

                if (isExplicitSelection)
                {
                    Logger.Log($"Explicitly selected audio device '{AudioDevice.Value}' failed to initialise in mode {mode}.", name: "audio", level: LogLevel.Important);
                    goto explicit_selection_failed;
                }
            }

            // try using the system default if there is any device present.
            // mobiles are an exception as the built-in speakers may not be provided as an audio device name,
            // but they are still provided by BASS under the internal device name "Default".
            if ((audioDeviceNames.Count > 0 || RuntimeInfo.IsMobile) && trySetDevice(bass_default_device, mode)) return;

            // no audio devices can be used, so try using Bass-provided "No sound" device as last resort.
            trySetDevice(Bass.NoSoundDevice, AudioOutputMode.Default);

            // we're boned. even "No sound" device won't initialise.
            return;

        explicit_selection_failed:
            // Headless tests explicitly select "No sound", which is a BASS internal device.
            if (DebugUtils.IsNUnitRunning && trySetDevice(Bass.NoSoundDevice, AudioOutputMode.Default))
                return;

            Logger.Log($"Keeping explicit audio selection '{AudioDevice.Value}' after initialisation failure; skipping silent fallback to default device.", name: "audio", level: LogLevel.Important);
            Logger.Log($"Audio output remains uninitialised after explicit device selection failure: '{AudioDevice.Value}'.", name: "audio", level: LogLevel.Important);
            return;

            bool trySetDevice(int deviceId, AudioOutputMode outputMode)
            {
                var device = audioDevices.ElementAtOrDefault(deviceId);

                // device is invalid
                if (!device.IsEnabled)
                    return false;

                // we don't want bass initializing with real audio device on headless test runs.
                if (deviceId != Bass.NoSoundDevice && DebugUtils.IsNUnitRunning)
                    return false;

                // initialize new device
                if (!InitBass(deviceId, outputMode))
                    return false;

                //we have successfully initialised a new device.
                // Initialise optional audio backend (WASAPI prototype) with the selected device.
                if (audioBackend != null)
                {
                    try
                    {
                        audioBackend.Initialize(deviceId);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Audio backend failed to initialize: {ex}", name: "audio", level: LogLevel.Important);
                    }
                }

                // Notify backend of device change so it can reconfigure if required.
                try
                {
                    audioBackend?.UpdateDevice(deviceId);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Audio backend UpdateDevice failed: {ex}", name: "audio", level: LogLevel.Important);
                }

                UpdateDevice(deviceId);

                return true;
            }
        }

        private void normaliseLegacySelection()
        {
            if (syncingSelection)
                return;

            string selection = AudioDevice.Value;
            if (string.IsNullOrEmpty(selection))
                return;

            // Earlier iterations stored typed entries for BASS/Shared WASAPI directly in AudioDevice.
            // The dropdown now shows raw device names (plus appended Exclusive/ASIO), so rewrite old values.
            if (tryParseSuffixed(selection, legacy_type_bass, out string baseName))
            {
                syncingSelection = true;

                try
                {
                    setUserBindableValueLeaseSafe(AudioDevice, baseName);
                }
                finally
                {
                    syncingSelection = false;
                }

                return;
            }

            if (tryParseSuffixed(selection, legacy_type_wasapi_shared, out baseName))
            {
                syncingSelection = true;

                try
                {
                    setUserBindableValueLeaseSafe(AudioDevice, baseName);
                    setUserBindableValueLeaseSafe(UseExperimentalWasapi, true);
                }
                finally
                {
                    syncingSelection = false;
                }
            }
        }

        private static bool hasTypeSuffix(string value) => !string.IsNullOrEmpty(value) && (value.EndsWith($" ({type_wasapi_exclusive})", StringComparison.Ordinal)
                                                                                            || value.EndsWith($" ({type_asio})", StringComparison.Ordinal));

        /// <summary>
        /// This method calls <see cref="Bass.Init(int, int, DeviceInitFlags, IntPtr, IntPtr)"/>.
        /// It can be overridden for unit testing.
        /// </summary>
        /// <param name="device">The device to initialise.</param>
        /// <param name="outputMode">The output mode to use for playback.</param>
        protected virtual bool InitBass(int device, AudioOutputMode outputMode)
        {
            if (int.TryParse(Environment.GetEnvironmentVariable("OSU_TEMP_TESTING_BASS_CONFIG_DEV_PERIOD"), out int devicePeriod))
            {
                Logger.Log(
                    $"Device period is set to \"{devicePeriod}\" via environment variable for testing purposes.\n\nThis is made available for testing so we can gather feedback on how to incorporate as a permanent game setting. Incorrect settings may lead to serious issues.",
                    level: LogLevel.Important);

                // Device period normally is in milliseconds, but it might be set to a negative
                // value too for an exact sample size, e.g. -256 for 256 samples.
                // https://www.un4seen.com/doc/#bass/BASS_CONFIG_DEV_PERIOD.html
                Bass.Configure(ManagedBass.Configuration.DevicePeriod, devicePeriod);

                // 1ms is definitely too low, but we're setting such low number on purpose,
                // in order for BASS to automatically set it to twice the length of BASS_CONFIG_DEV_PERIOD.
                //
                // See https://www.un4seen.com/doc/#bass/BASS_CONFIG_DEV_BUFFER.html
                Bass.DeviceBufferLength = 1;
            }
            else
            {
                // reduce latency to a known sane minimum.
                Bass.DeviceBufferLength = 10;
            }

            // These two likely don't have any effect because we set StreamSystem.NoBuffer on audio streams.
            Bass.UpdatePeriod = 1;
            Bass.PlaybackBufferLength = 20;

            // ensure there are no brief delays on audio operations (causing stream stalls etc.) after periods of silence.
            Bass.DeviceNonStop = true;

            // without this, if bass falls back to directsound legacy mode the audio playback offset will be way off.
            Bass.Configure(ManagedBass.Configuration.TruePlayPosition, 0);

            // Set BASS_IOS_SESSION_DISABLE here to leave session configuration in our hands (see iOS project).
            Bass.Configure(ManagedBass.Configuration.IOSSession, 16);

            // Always provide a default device. This should be a no-op, but we have asserts for this behaviour.
            Bass.Configure(ManagedBass.Configuration.IncludeDefaultDevice, true);

            // Enable custom BASS_CONFIG_MP3_OLDGAPS flag for backwards compatibility.
            // - This disables support for ItunSMPB tag parsing to match previous expectations.
            // - This also disables a change which assumes a 529 sample (2116 byte in stereo 16-bit) delay if the MP3 file doesn't specify one.
            //   (That was added in Bass for more consistent results across platforms and standard/mp3-free BASS versions, because OSX/iOS's MP3 decoder always removes 529 samples)
            // Bass.Configure((ManagedBass.Configuration)68, 1);

            // Disable BASS_CONFIG_DEV_TIMEOUT flag to keep BASS audio output from pausing on device processing timeout.
            // See https://www.un4seen.com/forum/?topic=19601 for more information.
            Bass.Configure((ManagedBass.Configuration)70, false);

            bool attemptInit()
            {
                bool innerSuccess;

                try
                {
                    innerSuccess = thread.InitDevice(device, outputMode, SampleRate.Value, AsioBitDepth.Value);
                }
                catch (Exception e)
                {
                    Logger.Log($"Audio device initialisation threw an exception (mode: {outputMode}, device: {device}): {e}", name: "audio", level: LogLevel.Error);
                    return false;
                }

                // For ASIO mode, initialization failure should be treated as a critical failure
                // since ASIO devices require specific initialization that may not be recoverable
                if (outputMode == AudioOutputMode.Asio && !innerSuccess)
                {
                    Logger.Log("ASIO device initialization failed - this is treated as a critical failure", name: "audio", level: LogLevel.Error);
                    return false;
                }

                bool alreadyInitialised = Bass.LastError == Errors.Already;

                if (alreadyInitialised)
                {
                    // For ASIO, a failed device init must fail even if BASS was already initialised.
                    if (outputMode == AudioOutputMode.Asio && !innerSuccess)
                    {
                        Logger.Log("ASIO device initialization failed even though BASS was already initialized", name: "audio", level: LogLevel.Error);
                        return false;
                    }

                    return true;
                }

                if (BassUtils.CheckFaulted(false))
                    return false;

                if (!innerSuccess)
                {
                    Logger.Log("BASS failed to initialize but did not provide an error code", name: "audio", level: LogLevel.Error);
                    return false;
                }

                var deviceInfo = audioDevices.ElementAtOrDefault(device);

                Logger.Log($@"🔈 BASS initialised
                          BASS version:           {Bass.Version}
                          BASS FX version:        {BassFx.Version}
                          BASS MIX version:       {BassMix.Version}
                          Device:                 {deviceInfo.Name}
                          Driver:                 {deviceInfo.Driver}
                          Device period length:   {devicePeriod}
                          Device buffer length:   {Bass.DeviceBufferLength} ms
                          Update period:          {Bass.UpdatePeriod} ms
                          Playback buffer length: {Bass.PlaybackBufferLength} ms");

                return true;
            }

            return attemptInit();
        }

        private void syncAudioDevices()
        {
            audioDevices = GetAllDevices();

            // Bass should always be providing "No sound" and "Default" device.
            Trace.Assert(audioDevices.Length >= BASS_INTERNAL_DEVICE_COUNT, "Bass did not provide any audio devices.");

            var oldDeviceNames = audioDeviceNames;
            var newDeviceNames = audioDeviceNames = audioDevices.Skip(BASS_INTERNAL_DEVICE_COUNT).Where(d => d.IsEnabled).Select(d => d.Name).ToImmutableList();

            scheduler.Add(() =>
            {
                if (cancelSource.IsCancellationRequested)
                    return;

                if (!IsCurrentDeviceValid())
                    initCurrentDevice();
            }, false);

            var newDevices = newDeviceNames.Except(oldDeviceNames).ToList();
            var lostDevices = oldDeviceNames.Except(newDeviceNames).ToList();

            if (newDevices.Count > 0 || lostDevices.Count > 0)
            {
                eventScheduler.Add(delegate
                {
                    foreach (string d in newDevices)
                        OnNewDevice?.Invoke(d);
                    foreach (string d in lostDevices)
                        OnLostDevice?.Invoke(d);
                });
            }

            syncAsioDeviceChanges();
        }

        private static bool shouldPollAsioDeviceListChanges()
        {
            // BassAsio enumeration from the device-monitor background thread can block indefinitely on some drivers.
            // Automated test/benchmark hosts never need hot-plug ASIO notifications.
            return RuntimeInfo.OS == RuntimeInfo.Platform.Windows
                   && !DebugUtils.IsNUnitRunning
                   && EzAsioDeviceManager.IsAvailable;
        }

        private void syncAsioDeviceChanges()
        {
            if (!shouldPollAsioDeviceListChanges())
            {
                previousAsioDeviceNames = ImmutableList<string>.Empty;
                return;
            }

            ImmutableList<string> current;

            try
            {
                current = EzAsioDeviceManager.EnumerateAsioDevices().Select(d => d.Name).ToImmutableList();
            }
            catch (Exception ex)
            {
                logAsioNativeUnavailableOnce(ex);
                return;
            }

            var newAsioDevices = current.Except(previousAsioDeviceNames).ToList();
            var lostAsioDevices = previousAsioDeviceNames.Except(current).ToList();
            previousAsioDeviceNames = current;

            if (newAsioDevices.Count == 0 && lostAsioDevices.Count == 0)
                return;

            eventScheduler.Add(delegate
            {
                foreach (string d in newAsioDevices)
                    OnNewDevice?.Invoke(formatEntry(d, type_asio));
                foreach (string d in lostAsioDevices)
                    OnLostDevice?.Invoke(formatEntry(d, type_asio));
            });
        }

        private IEnumerable<string> getAudioDeviceEntries()
        {
            var entries = new List<string>();

            // Base BASS devices (historical UX: raw device names).
            entries.AddRange(audioDeviceNames);

            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
            {
                // Always expose Exclusive/ASIO entries; experimental WASAPI only affects unsuffixed BASS device names.
                entries.AddRange(audioDeviceNames.Select(d => formatEntry(d, type_wasapi_exclusive)));

                // ASIO drivers.
                int asioCount = 0;

                foreach (var device in EzAsioDeviceManager.EnumerateAsioDevices())
                {
                    entries.Add(formatEntry(device.Name, type_asio));
                    asioCount++;
                }

                Logger.Log($"Found {asioCount} ASIO devices", name: "audio", level: LogLevel.Verbose);
            }

            return entries;
        }

        private static string formatEntry(string name, string type) => $"{name} ({type})";

        private (AudioOutputMode mode, string deviceName) parseSelection(string selection)
        {
            // Default device.
            if (string.IsNullOrEmpty(selection))
            {
                return (UseExperimentalWasapi.Value && RuntimeInfo.OS == RuntimeInfo.Platform.Windows
                    ? AudioOutputMode.WasapiShared
                    : AudioOutputMode.Default, string.Empty);
            }

            if (tryParseSuffixed(selection, type_wasapi_exclusive, out string name))
                return (AudioOutputMode.WasapiExclusive, name);

            if (tryParseSuffixed(selection, type_asio, out name))
            {
                return (AudioOutputMode.Asio, name);
            }

            // Legacy value (raw BASS device name). Keep old behaviour: the experimental flag decides shared WASAPI.
            if (UseExperimentalWasapi.Value && RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
                return (AudioOutputMode.WasapiShared, selection);

            return (AudioOutputMode.Default, selection);
        }

        private static bool tryParseSuffixed(string value, string type, out string baseName)
        {
            string suffix = $" ({type})";

            if (value.EndsWith(suffix, StringComparison.Ordinal))
            {
                baseName = value[..^suffix.Length];
                return true;
            }

            baseName = string.Empty;
            return false;
        }

        private void requestAsioReinitialisation(string reason)
        {
            if (syncingSelection)
                return;

            if (parseSelection(AudioDevice.Value).mode != AudioOutputMode.Asio)
            {
                Logger.Log($"{reason}, but current device ({AudioDevice.Value}) is not ASIO; skipping reinitialisation.", name: "audio", level: LogLevel.Debug);
                return;
            }

            Logger.Log($"{reason}. Reinitialising ASIO output.", name: "audio", level: LogLevel.Important);

            var (_, deviceName) = parseSelection(AudioDevice.Value);
            int? asioIndex = EzAsioDeviceManager.FindAsioDeviceIndex(deviceName);

            scheduler.AddOnce(() =>
            {
                bool reconfigured = false;

                if (asioIndex.HasValue)
                {
                    try
                    {
                        reconfigured = thread.TryReconfigureAsio(asioIndex.Value, SampleRate.Value, AsioBitDepth.Value, AsioBufferSize.Value);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"ASIO short-path reconfiguration failed: {ex}", name: "audio", level: LogLevel.Error);
                    }
                }

                if (!reconfigured)
                    initCurrentDevice();

                if (!reconfigured && !IsCurrentDeviceValid())
                    Logger.Log($"ASIO reinitialisation failed after setting change. Device selection: {AudioDevice.Value}", name: "audio", level: LogLevel.Error);
            });
        }

        private static void logAsioNativeUnavailableOnce(Exception e)
        {
            if (Interlocked.Exchange(ref asioNativeUnavailableLogged, 1) == 1)
                return;

            // Keep message actionable but non-intrusive (no UI popups).
            Logger.Log(
                $"ASIO output is unavailable because the native bassasio library could not be loaded ({e.GetType().Name}: {e.Message}). Ensure bassasio.dll is present alongside other BASS native libraries (typically in the x64/x86 subdirectories; the BASS.ASIO NuGet package will copy it automatically when referenced).",
                name: "audio", level: LogLevel.Error);
        }

        /// <summary>
        /// Check whether any audio device changes have occurred.
        ///
        /// Changes supported are:
        /// - A new device is added
        /// - An existing device is Enabled/Disabled or set as Default
        /// </summary>
        /// <remarks>
        /// This method is optimised to incur the lowest overhead possible.
        /// </remarks>
        /// <param name="previousDevices">The previous audio devices array.</param>
        /// <returns>Whether a change was detected.</returns>
        protected virtual bool CheckForDeviceChanges(ImmutableArray<DeviceInfo> previousDevices)
        {
            int deviceCount = Bass.DeviceCount;

            if (previousDevices.Length != deviceCount)
                return true;

            for (int i = 0; i < deviceCount; i++)
            {
                var prevInfo = previousDevices[i];

                Bass.GetDeviceInfo(i, out var info);

                if (info.IsEnabled != prevInfo.IsEnabled)
                    return true;

                if (info.IsDefault != prevInfo.IsDefault)
                    return true;
            }

            if (shouldPollAsioDeviceListChanges() && checkAsioDeviceListChanged())
                return true;

            return false;
        }

        private bool checkAsioDeviceListChanged()
        {
            try
            {
                var current = EzAsioDeviceManager.EnumerateAsioDevices().Select(d => d.Name).ToImmutableList();
                return !current.SequenceEqual(previousAsioDeviceNames);
            }
            catch (Exception ex)
            {
                logAsioNativeUnavailableOnce(ex);
                return false;
            }
        }

        protected virtual ImmutableArray<DeviceInfo> GetAllDevices()
        {
            int deviceCount = Bass.DeviceCount;

            var devices = ImmutableArray.CreateBuilder<DeviceInfo>(deviceCount);
            for (int i = 0; i < deviceCount; i++)
                devices.Add(Bass.GetDeviceInfo(i));

            return devices.MoveToImmutable();
        }

        // The current device is considered valid if it is enabled, initialized, and not a fallback device.
        protected virtual bool IsCurrentDeviceValid()
        {
            var device = audioDevices.ElementAtOrDefault(Bass.CurrentDevice);
            var (mode, selectedName) = parseSelection(AudioDevice.Value);

            // ASIO output selection does not map to a BASS device name; ensure we're initialised and ASIO is working.
            if (mode == AudioOutputMode.Asio)
            {
                try
                {
                    if (!EzAsioDeviceManager.IsDeviceRunning())
                        return false;

                    var asioInfo = EzAsioDeviceManager.GetCurrentDeviceInfo();
                    double currentRate = EzAsioDeviceManager.GetCurrentSampleRate();

                    return asioInfo != null && currentRate > 0 && !double.IsNaN(currentRate) && !double.IsInfinity(currentRate);
                }
                catch
                {
                    return false;
                }
            }

            bool isFallback = string.IsNullOrEmpty(selectedName) ? !device.IsDefault : device.Name != selectedName;
            return device.IsEnabled && device.IsInitialized && !isFallback;
        }

        public override string ToString()
        {
            string deviceName = audioDevices.ElementAtOrDefault(Bass.CurrentDevice).Name;
            return $@"{GetType().ReadableName()} ({deviceName ?? "Unknown"})";
        }
    }
}
