// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using ManagedBass;
using ManagedBass.Asio;
using ManagedBass.Mix;
using ManagedBass.Wasapi;
using osu.Framework.Audio;
using osu.Framework.Audio.Asio;
using osu.Framework.Audio.EzLatency;
using osu.Framework.Audio.Host;
using osu.Framework.Audio.Wasapi;
using osu.Framework.Audio.Windows;
using osu.Framework.Bindables;
using osu.Framework.Development;
using osu.Framework.Logging;
using osu.Framework.Platform.Linux.Native;
using osu.Framework.Statistics;

namespace osu.Framework.Threading
{
    public class AudioThread : GameThread
    {
        internal const double MINIMUM_AMPLITUDE_PROCESSING_HZ = 120;

        private double amplitudeProcessingHz = MINIMUM_AMPLITUDE_PROCESSING_HZ;

        /// <summary>
        /// The target update rate for visual audio analysis (channel levels and FFT data).
        /// This is kept separate from <see cref="GameThread.ActiveHz"/> so low-latency audio
        /// control can run at a higher rate without repeatedly performing expensive analysis.
        /// </summary>
        internal double AmplitudeProcessingHz
        {
            get => Volatile.Read(ref amplitudeProcessingHz);
            set => Volatile.Write(ref amplitudeProcessingHz, Math.Max(MINIMUM_AMPLITUDE_PROCESSING_HZ, value));
        }

        internal enum AsioReconfigurePath
        {
            BufferRestart,
            FullReconfigure,
        }

        internal AsioReconfigurePath? LastAsioReconfigurePath { get; private set; }

        internal void ResetLastAsioReconfigurePath() => LastAsioReconfigurePath = null;

        private static int wasapiNativeUnavailableLogged;
        private static int asioResolverRegistered;

        internal readonly EzLatencyAnalyzer LatencyAnalyzer;

        public AudioThread()
            : base(name: "Audio")
        {
            // Ensure this AudioThread instance is always reachable from native WASAPI callbacks.
            wasapiUserHandle = GCHandle.Alloc(this, GCHandleType.Normal);
            wasapiUserPtr = GCHandle.ToIntPtr(wasapiUserHandle);

            OnNewFrame += onNewFrame;
            PreloadBass();

            // Initialise EzLatency on the audio thread.
            LatencyAnalyzer = new EzLatencyAnalyzer();

            // Forward any analyzer records from this audio thread into the global EzLatencyService
            LatencyAnalyzer.OnNewRecord += r =>
            {
                EzLatencyService.Instance.PushRecord(r);
            };
        }

        public override bool IsCurrent => ThreadSafety.IsAudioThread;

        internal sealed override void MakeCurrent()
        {
            base.MakeCurrent();

            ThreadSafety.IsAudioThread = true;
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.TasksRun,
            StatisticsCounterType.Tracks,
            StatisticsCounterType.Samples,
            StatisticsCounterType.SChannels,
            StatisticsCounterType.Components,
            StatisticsCounterType.MixChannels,
        };

        private readonly List<AudioManager> managers = new List<AudioManager>();

        private static readonly HashSet<int> initialised_devices = new HashSet<int>();

        private static readonly GlobalStatistic<double> cpu_usage = GlobalStatistics.Get<double>("Audio", "Bass CPU%");

        private long frameCount;

        private void onNewFrame()
        {
            if (frameCount++ % 1000 == 0)
                cpu_usage.Value = Bass.CPUUsage;

            lock (managers)
            {
                for (int i = 0; i < managers.Count; i++)
                {
                    var m = managers[i];
                    m.Update();
                }
            }
        }

        internal void RegisterManager(AudioManager manager)
        {
            lock (managers)
            {
                if (managers.Contains(manager))
                    throw new InvalidOperationException($"{manager} was already registered");

                managers.Add(manager);
            }

            // Set the manager reference for event triggering
            Manager ??= manager;

            manager.GlobalMixerHandle.BindTo(globalMixerHandle);
        }

        internal void UnregisterManager(AudioManager manager)
        {
            lock (managers)
                managers.Remove(manager);

            manager.GlobalMixerHandle.UnbindFrom(globalMixerHandle);
        }

        protected override void OnExit()
        {
            base.OnExit();

            lock (managers)
            {
                // AudioManagers are iterated over backwards since disposal will unregister and remove them from the list.
                for (int i = managers.Count - 1; i >= 0; i--)
                {
                    var m = managers[i];

                    m.Dispose();

                    // Audio component disposal (including the AudioManager itself) is scheduled and only runs when the AudioThread updates.
                    // But the AudioThread won't run another update since it's exiting, so an update must be performed manually in order to finish the disposal.
                    m.Update();
                }

                managers.Clear();
            }

            // Safety net to ensure we have freed all devices before exiting.
            // This is mainly required for device-lost scenarios.
            // See https://github.com/ppy/osu-framework/pull/3378 for further discussion.
            foreach (int d in initialised_devices.ToArray())
                FreeDevice(d);

            if (wasapiUserHandle.IsAllocated)
                wasapiUserHandle.Free();
        }

        #region BASS Initialisation

        // TODO: All this bass init stuff should probably not be in this class.

        // WASAPI callbacks must never be allowed to be GC'd while native code may still call into them.
        // Use static delegates with a stable user pointer back to this thread instance.
        private static readonly WasapiProcedure wasapi_procedure_static = (buffer, length, user) =>
        {
            var thread = getWasapiOwner(user);

            int? mixer = thread?.globalMixerHandle.Value;
            if (mixer == null)
                return 0;

            int read = Bass.ChannelGetData(mixer.Value, buffer, length);

            if (read > 0)
                EzLatencyManager.GLOBAL.ObserveOutputPath(buffer, read, thread!.wasapiPullSampleFormat);

            return read;
        };

        private static readonly WasapiNotifyProcedure wasapi_notify_procedure_static = (notify, device, user) =>
        {
            var thread = getWasapiOwner(user);
            if (thread == null)
                return;

            // see https://www.un4seen.com/doc/#basswasapi/WASAPINOTIFYPROC.html
            switch (notify)
            {
                case WasapiNotificationType.DefaultOutput:
                    if (!thread.shouldTrackDefaultWasapiChanges())
                        return;

                    Logger.Log("Attempting BassWasapi reinitialisation after default output change", name: "audio");
                    break;

                case WasapiNotificationType.Fail:
                    Logger.Log("Attempting BassWasapi reinitialisation after initialisation failure", name: "audio");
                    break;

                default:
                    return;
            }

            // to note, when a failure occurs the notify procedure callback only occurs once, so there's
            // no chance of this getting into a runaway reinitialisation loop.
            //
            // the schedule safety here is to avoid a double re-init as two threads are usually involved here.
            bool exclusive = thread.wasapiExclusiveActive;
            thread.wasapiPendingRecoveryInit?.Cancel();
            thread.wasapiPendingRecoveryInit = thread.Scheduler.Add(() =>
            {
                if (notify == WasapiNotificationType.DefaultOutput && !thread.shouldTrackDefaultWasapiChanges())
                    return;

                thread.freeWasapi();

                bool ok = exclusive
                    ? thread.tryInitWasapiExclusiveWithBufferLadder(device)
                    : thread.initWasapi(device, exclusive: false, bufferMs: 0);

                if (ok)
                    thread.updateAudioManagers();
            });
        };

        private static AudioThread? getWasapiOwner(IntPtr user)
        {
            if (user == IntPtr.Zero)
                return null;

            try
            {
                return (AudioThread?)GCHandle.FromIntPtr(user).Target;
            }
            catch
            {
                return null;
            }
        }

        private GCHandle wasapiUserHandle;
        private readonly IntPtr wasapiUserPtr;
        private ScheduledDelegate? wasapiPendingRecoveryInit;
        private bool wasapiExclusiveActive;
        private int wasapiExclusiveRequestedBufferMs;
        private int wasapiExclusiveAppliedBufferMs;
        private int wasapiExclusivePeriodMs;
        private Errors lastWasapiInitError;
        private WasapiExclusiveEndpointGain? exclusiveEndpointGain;
        private AcousticLevelMath.SampleFormat wasapiPullSampleFormat = AcousticLevelMath.SampleFormat.IeeeFloat32;
        private NAudioWasapiOutput? naudioDefaultOutput;
        private int? naudioBoundBassDeviceId;
        private string? naudioBoundEndpointId;
        private int initDeviceDepth;
        private bool wasapiOrAsioWasActive;

        private bool shouldTrackDefaultWasapiChanges()
            => !wasapiExclusiveActive && string.IsNullOrEmpty(Manager?.AudioDevice.Value);

        /// <summary>
        /// Reference to the AudioManager that owns this AudioThread.
        /// Used to trigger events when ASIO devices are initialized.
        /// </summary>
        internal AudioManager? Manager { get; set; }

        /// <summary>
        /// If a global mixer is being used, this will be the BASS handle for it.
        /// If non-null, all game mixers should be added to this mixer.
        /// </summary>
        private readonly Bindable<int?> globalMixerHandle = new Bindable<int?>();

        internal bool InitDevice(int deviceId, AudioOutputMode outputMode, double preferredSampleRate, int asioBitDepth = 24)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);
            Trace.Assert(deviceId != -1); // The real device ID should always be used, as the -1 device has special cases which are hard to work with.

            if (initDeviceDepth > 0)
            {
                Logger.Log(
                    $"InitDevice re-entrancy ignored (device={deviceId}, mode={outputMode}, depth={initDeviceDepth})",
                    name: "audio",
                    level: LogLevel.Important);
                return (OperatingSystem.IsWindows() && isValidBassHandle(naudioDefaultOutput?.MixerHandle))
                       || isValidBassHandle(globalMixerHandle.Value)
                       || (Bass.GetDeviceInfo(deviceId, out var reInfo) && reInfo.IsInitialized);
            }

            // Keep a healthy NAudio Default session instead of tearing it down on spurious re-inits
            // (device list sync / BASS Default index vs physical device index for the same endpoint).
            if (outputMode == AudioOutputMode.Default
                && OperatingSystem.IsWindows()
                && naudioDefaultOutput?.IsRunning == true
                && endpointsMatchForKeepAlive(deviceId))
            {
                naudioBoundBassDeviceId = deviceId;
                string? latencyDriver = Bass.GetDeviceInfo(deviceId, out var existing) ? existing.Driver : naudioBoundEndpointId;
                EzLatencyManager.GLOBAL.NotifyOutputDeviceChanged(latencyDriver, outputMode);
                publishWindowsOutputRuntime(outputMode);
                return true;
            }

            initDeviceDepth++;

            try
            {
                return initDeviceCore(deviceId, outputMode, preferredSampleRate, asioBitDepth);
            }
            finally
            {
                initDeviceDepth--;
            }
        }

        [SupportedOSPlatform("windows")]
        private bool endpointsMatchForKeepAlive(int bassDeviceId)
        {
            if (string.IsNullOrEmpty(naudioBoundEndpointId))
                return naudioBoundBassDeviceId == bassDeviceId;

            if (!Bass.GetDeviceInfo(bassDeviceId, out var info))
                return false;

            string? incoming = WindowsAudioFormatQuery.TryResolvePlaybackEndpointId(
                string.IsNullOrEmpty(info.Driver) ? null : info.Driver);

            return !string.IsNullOrEmpty(incoming)
                   && string.Equals(incoming, naudioBoundEndpointId, StringComparison.OrdinalIgnoreCase);
        }

        private bool initDeviceCore(int deviceId, AudioOutputMode outputMode, double preferredSampleRate, int asioBitDepth)
        {
            // Important: stop any existing output first.
            // In particular, WASAPI exclusive can hold the device such that a subsequent Bass.Init() returns Busy.
            // If we can't initialise BASS, we also won't get a chance to clean up the previous output mode.
            freeNAudioDefault();
            freeAsio();
            freeWasapi();
            releaseAllOutputsForSwitch(deviceId, outputMode);

            // ASIO / BassWasapi / prior NAudio may still hold a virtual endpoint briefly (Oculus VAIO etc.).
            bool needsEndpointSettle = wasapiOrAsioWasActive || outputMode == AudioOutputMode.Default;
            wasapiOrAsioWasActive = false;

            if (outputMode == AudioOutputMode.Asio && !DebugUtils.IsNUnitRunning)
            {
                Thread.Sleep(100);
            }
            else if (needsEndpointSettle
                     && (outputMode == AudioOutputMode.Default || outputMode == AudioOutputMode.WasapiExclusive)
                     && OperatingSystem.IsWindows()
                     && !DebugUtils.IsNUnitRunning)
            {
                Thread.Sleep(150);
            }

            bool useNAudioDefault = outputMode == AudioOutputMode.Default
                                    && RuntimeInfo.OS == RuntimeInfo.Platform.Windows
                                    && OperatingSystem.IsWindows()
                                    && !DebugUtils.IsNUnitRunning;

            // NAudio opens the real WASAPI endpoint itself. Host the BASS decode mixer on NoSound so
            // Bass.Init does not fight NAudio for the same device (rapid VoiceMeeter switches → BASS_ERROR_UNKNOWN).
            int bassHostDeviceId = useNAudioDefault ? Bass.NoSoundDevice : deviceId;

            // Queried before the device is initialised on purpose. Bass.GetDeviceInfo() is an ordinary query,
            // but on Linux a *successful* call still leaves Errors.Parameter in the process-wide Bass.LastError;
            // running it after a successful Bass.Init() would dirty that code again for later readers. The
            // selected device is queried directly because its info is available even when the NAudio path hosts
            // BASS on NoSound.
            //
            // This only keeps the code clean on paths where an Init() actually runs below. The
            // already-initialised early return in tryInitBassDevice() never resets Bass.LastError, so it must
            // not be read as a success signal (see AudioManager.InitBass).
            string? latencyDriver = Bass.GetDeviceInfo(deviceId, out var latencyDeviceInfo) ? latencyDeviceInfo.Driver : null;

            if (!tryInitBassDevice(bassHostDeviceId))
            {
                if (!useNAudioDefault)
                    return false;

                // Last resort host context before attempting NAudio.
                if (!tryInitBassDevice(deviceId))
                    return false;

                bassHostDeviceId = deviceId;
            }

            switch (outputMode)
            {
                case AudioOutputMode.Default:
                    // Windows default: NAudio shared WASAPI pulls from a BASS decode mixer (replaces BASS device playback).
                    // Non-Windows keeps classic BASS device output. Experimental mode uses BassWasapi instead.
                    if (useNAudioDefault)
                    {
                        if (!initNAudioDefault(deviceId))
                        {
                            Logger.Log(
                                $"NAudio default output unavailable for BASS device {deviceId}; falling back to classic BASS device output.",
                                name: "audio",
                                level: LogLevel.Important);

                            // Classic playback needs the real device initialised.
                            if (!tryInitBassDevice(deviceId))
                                return false;

                            bassHostDeviceId = deviceId;
                        }
                    }

                    break;

                case AudioOutputMode.WasapiShared:
                    if (!attemptWasapiInitialisation(deviceId, exclusive: false))
                    {
                        Logger.Log($"BassWasapi initialisation failed (shared mode). BASS error: {Bass.LastError}", name: "audio", level: LogLevel.Error);
                        return false;
                    }

                    break;

                case AudioOutputMode.WasapiExclusive:
                    if (!attemptWasapiInitialisation(deviceId, exclusive: true))
                    {
                        Logger.Log($"BassWasapi initialisation failed (exclusive mode). BASS error: {Bass.LastError}", name: "audio", level: LogLevel.Error);
                        return false;
                    }

                    break;

                case AudioOutputMode.Asio:
                    // ASIO uses a BassAsio device index, not the BASS playback device id.
                    string selectedDeviceName = Manager?.AudioDevice.Value ?? string.Empty;

                    if (!string.IsNullOrEmpty(selectedDeviceName))
                    {
                        if (EzAsioDeviceManager.TryParseDeviceSelection(selectedDeviceName, out string deviceName))
                        {
                            int? asioDeviceIndex = EzAsioDeviceManager.FindAsioDeviceIndex(deviceName);

                            if (asioDeviceIndex.HasValue)
                            {
                                if (!initAsio(asioDeviceIndex.Value, preferredSampleRate, asioBitDepth, deviceName))
                                    return false;
                            }
                            else
                            {
                                Logger.Log($"Could not find ASIO device: {deviceName}", name: "audio", level: LogLevel.Error);
                                return false;
                            }
                        }
                        else
                        {
                            Logger.Log("Could not parse ASIO device name from selection", name: "audio", level: LogLevel.Error);
                            return false;
                        }
                    }
                    else
                    {
                        Logger.Log("No ASIO device name in selection; falling back to first available device", name: "audio");
                        var availableDevices = EzAsioDeviceManager.EnumerateAsioDevices().ToList();

                        if (availableDevices.Count != 0)
                        {
                            if (!initAsio(availableDevices.First().Index, preferredSampleRate, asioBitDepth, availableDevices.First().Name))
                                return false;
                        }
                        else
                        {
                            Logger.Log("No ASIO devices are available", name: "audio", level: LogLevel.Error);
                            return false;
                        }
                    }

                    break;
            }

            initialised_devices.Add(bassHostDeviceId);
            if (bassHostDeviceId != deviceId)
                initialised_devices.Add(deviceId);

            // Bind latency probe context to the active output endpoint/backend.
            EzLatencyManager.GLOBAL.NotifyOutputDeviceChanged(latencyDriver, outputMode);

            publishWindowsOutputRuntime(outputMode);

            return true;
        }

        /// <summary>
        /// Initialise a BASS playback device with short retries for transient Unknown/Busy after Free.
        /// </summary>
        private bool tryInitBassDevice(int deviceId)
        {
            if (isBassDeviceInitialized(deviceId))
            {
                Bass.CurrentDevice = deviceId;
                return true;
            }

            int[] delaysMs = [0, 80, 200];

            for (int attempt = 0; attempt < delaysMs.Length; attempt++)
            {
                if (delaysMs[attempt] > 0)
                    Thread.Sleep(delaysMs[attempt]);

                var initFlags = initialised_devices.Contains(deviceId) ? (DeviceInitFlags)16 : 0;

                if (Bass.Init(deviceId, Flags: initFlags))
                {
                    initialised_devices.Add(deviceId);
                    return true;
                }

                if (Bass.LastError == Errors.Already)
                {
                    initialised_devices.Add(deviceId);
                    Bass.CurrentDevice = deviceId;
                    return true;
                }

                var error = Bass.LastError;

                // Unknown (-1) / Busy are common right after Free on virtual cables.
                bool retryable = error == Errors.Unknown || error == Errors.Busy || error == Errors.Driver;

                Logger.Log(
                    $"BASS.Init({deviceId}) failed: {error} (attempt {attempt + 1}/{delaysMs.Length})",
                    name: "audio",
                    level: retryable && attempt < delaysMs.Length - 1 ? LogLevel.Debug : LogLevel.Error);

                if (!retryable)
                    return false;
            }

            return false;
        }

        private static bool isBassDeviceInitialized(int deviceId) =>
            Bass.GetDeviceInfo(deviceId, out var info) && info.IsInitialized;

        private void publishWindowsOutputRuntime(AudioOutputMode outputMode)
        {
            if (Manager == null)
                return;

            switch (outputMode)
            {
                case AudioOutputMode.Default:
                    if (OperatingSystem.IsWindows() && tryPublishNAudioDefaultRuntime())
                        break;

                    Manager.SetWindowsOutputRuntime(new AudioManager.WindowsOutputRuntimeInfo(
                        Mode: AudioOutputMode.Default,
                        Active: true,
                        NAudioFallbackToClassicBass: RuntimeInfo.OS == RuntimeInfo.Platform.Windows));
                    break;

                case AudioOutputMode.WasapiShared:
                    Manager.SetWindowsOutputRuntime(new AudioManager.WindowsOutputRuntimeInfo(
                        Mode: AudioOutputMode.WasapiShared,
                        Active: isValidBassHandle(globalMixerHandle.Value)));
                    break;

                case AudioOutputMode.WasapiExclusive:
                    Manager.SetWindowsOutputRuntime(new AudioManager.WindowsOutputRuntimeInfo(
                        Mode: AudioOutputMode.WasapiExclusive,
                        Active: wasapiExclusiveActive && isValidBassHandle(globalMixerHandle.Value),
                        WasapiBufferMs: wasapiExclusiveAppliedBufferMs > 0 ? wasapiExclusiveAppliedBufferMs : null,
                        WasapiPeriodMs: wasapiExclusivePeriodMs > 0 ? wasapiExclusivePeriodMs : null,
                        WasapiRequestedBufferMs: wasapiExclusiveRequestedBufferMs));
                    break;

                case AudioOutputMode.Asio:
                    Manager.SetWindowsOutputRuntime(new AudioManager.WindowsOutputRuntimeInfo(
                        Mode: AudioOutputMode.Asio,
                        Active: EzAsioDeviceManager.IsDeviceRunning(),
                        SampleRateHz: (int)Math.Round(EzAsioDeviceManager.GetCurrentSampleRate())));
                    break;
            }
        }

        [SupportedOSPlatform("windows")]
        private bool tryPublishNAudioDefaultRuntime()
        {
            if (Manager == null || naudioDefaultOutput?.IsRunning != true)
                return false;

            Manager.SetWindowsOutputRuntime(new AudioManager.WindowsOutputRuntimeInfo(
                Mode: AudioOutputMode.Default,
                Active: true,
                NAudioFallbackToClassicBass: false,
                SampleRateHz: naudioDefaultOutput.SampleRateHz,
                ActualLatencyMs: naudioDefaultOutput.ActualLatencyMs,
                RequestedLatencyMs: naudioDefaultOutput.RequestedLatencyMs,
                LowLatencyActive: naudioDefaultOutput.LowLatencyActive));
            return true;
        }

        private static void releaseAllOutputsForSwitch(int targetDeviceId, AudioOutputMode outputMode)
        {
            for (int deviceId = 0; deviceId < Bass.DeviceCount; deviceId++)
            {
                try
                {
                    if (!Bass.GetDeviceInfo(deviceId, out var deviceInfo) || !deviceInfo.IsInitialized)
                        continue;

                    Bass.CurrentDevice = deviceId;
                    Logger.Log($"Releasing BASS output device {deviceId} before switching to {outputMode} (target device {targetDeviceId}).", name: "audio", level: LogLevel.Debug);
                    Bass.Free();
                    initialised_devices.Remove(deviceId);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to release BASS output device {deviceId} before switching to {outputMode}: {ex}", name: "audio", level: LogLevel.Error);
                }
            }

            // Do not restore CurrentDevice to a just-freed index — that leaves BASS pointing at an invalid device
            // and the next Init can return Unknown (-1).
            try
            {
                if (Bass.GetDeviceInfo(Bass.NoSoundDevice, out var noSound) && noSound.IsInitialized)
                    Bass.CurrentDevice = Bass.NoSoundDevice;
            }
            catch
            {
            }

            Thread.Sleep(80);
        }

        internal void FreeDevice(int deviceId)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);

            int selectedDevice = Bass.CurrentDevice;

            // Tear down ASIO / NAudio before BASS so driver handles are released in order.
            freeNAudioDefault();
            freeAsio();

            if (canSelectDevice(deviceId))
            {
                Bass.CurrentDevice = deviceId;
                Bass.Free();
            }

            freeWasapi();

            if (selectedDevice != deviceId && canSelectDevice(selectedDevice))
                Bass.CurrentDevice = selectedDevice;

            initialised_devices.Remove(deviceId);

            static bool canSelectDevice(int deviceId) => Bass.GetDeviceInfo(deviceId, out var deviceInfo) && deviceInfo.IsInitialized;
        }

        /// <summary>
        /// Makes BASS available to be consumed.
        /// </summary>
        internal static void PreloadBass()
        {
            if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
            {
                if (Interlocked.Exchange(ref asioResolverRegistered, 1) == 1)
                    return;

                NativeLibrary.SetDllImportResolver(typeof(BassAsio).Assembly, resolveBassAsio);
            }

            if (RuntimeInfo.OS == RuntimeInfo.Platform.Linux)
            {
                // required for the time being to address libbass_fx.so load failures (see https://github.com/ppy/osu/issues/2852)
                Library.Load("libbass.so", Library.LoadFlags.RTLD_LAZY | Library.LoadFlags.RTLD_GLOBAL);
            }
        }

        private static IntPtr resolveBassAsio(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (!libraryName.Equals("bassasio", StringComparison.OrdinalIgnoreCase)
                && !libraryName.Equals("bassasio.dll", StringComparison.OrdinalIgnoreCase))
                return IntPtr.Zero;

            // Try the default resolver first without throwing — a failed ASIO probe must not break non-ASIO paths.
            if (NativeLibrary.TryLoad(libraryName, assembly, searchPath ?? DllImportSearchPath.UseDllDirectoryForDependencies | DllImportSearchPath.SafeDirectories, out IntPtr result))
                return result;

            // On 64-bit builds, fall back to the x64 native folder.
            if (Environment.Is64BitProcess)
            {
                string dllPath = Path.Combine(AppContext.BaseDirectory, "x64", "bassasio.dll");
                if (File.Exists(dllPath))
                    return NativeLibrary.TryLoad(dllPath, out result) ? result : IntPtr.Zero;
            }
            else
            {
                string dllPath = Path.Combine(AppContext.BaseDirectory, "x86", "bassasio.dll");
                if (File.Exists(dllPath))
                    return NativeLibrary.TryLoad(dllPath, out result) ? result : IntPtr.Zero;
            }

            // Let the runtime use its default resolver if we could not load bassasio.
            return IntPtr.Zero;
        }

        private bool attemptWasapiInitialisation() => attemptWasapiInitialisation(Bass.CurrentDevice, exclusive: false);

        private bool attemptWasapiInitialisation(int bassDeviceId, bool exclusive)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return false;

            Logger.Log("Attempting local BassWasapi initialisation");

            try
            {
                return attemptWasapiInitialisationInternal(bassDeviceId, exclusive);
            }
            catch (DllNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI output is unavailable because basswasapi.dll could not be loaded ({e.Message}).");
                return false;
            }
            catch (EntryPointNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI output is unavailable because basswasapi.dll is incompatible/mismatched ({e.Message}).");
                return false;
            }
            catch (Exception e)
            {
                Logger.Log($"WASAPI initialisation failed with exception: {e}", name: "audio", level: LogLevel.Error);
                return false;
            }
        }

        private bool attemptWasapiInitialisationInternal(int bassDeviceId, bool exclusive)
        {
            Logger.Log($"Attempting local BassWasapi initialisation (exclusive: {exclusive})", name: "audio", level: LogLevel.Verbose);

            int wasapiDevice = -1;

            // WASAPI device indices don't match normal BASS devices.
            // Each device is listed multiple times with each supported channel/frequency pair.
            //
            // Working backwards to find the correct device is how bass does things internally (see BassWasapi.GetBassDevice).
            if (bassDeviceId > 0)
            {
                string driver = Bass.GetDeviceInfo(bassDeviceId).Driver;

                if (!string.IsNullOrEmpty(driver))
                {
                    var candidates = new List<(int index, int freq, int chans)>();

                    // WASAPI device indices don't match normal BASS devices.
                    // Each device is listed multiple times with each supported channel/frequency pair.
                    //
                    // Working backwards to find the correct device is how bass does things internally (see BassWasapi.GetBassDevice).
                    // We replicate this by scanning the full list and remembering the last match.
                    for (int i = 0; i < 16384; i++)
                    {
                        if (!BassWasapi.GetDeviceInfo(i, out WasapiDeviceInfo info))
                            break;

                        // Only consider output devices (not input/loopback), since we're initialising audio output.
                        if (info.ID == driver && info.IsEnabled && !info.IsInput && !info.IsLoopback)
                            candidates.Add((i, info.MixFrequency, info.MixChannels));
                    }

                    if (candidates.Count > 0)
                    {
                        // Prefer common stereo formats.
                        var best = orderWasapiCandidates(candidates).First();

                        wasapiDevice = best.index;
                        Logger.Log($"Mapped BASS device {bassDeviceId} (driver '{driver}') to WASAPI device {wasapiDevice} (mix: {best.freq}Hz/{best.chans}ch).", name: "audio",
                            level: LogLevel.Verbose);

                        // In exclusive mode, the chosen (freq/chans) pair matters. If the preferred candidate fails,
                        // we will retry other candidates below. Buffer ladder stays inside each format attempt
                        // so device reopen cost stays format-count × ladder-count, not the reverse nesting.
                        if (exclusive)
                        {
                            foreach (var candidate in orderWasapiCandidates(candidates))
                            {
                                if (tryInitWasapiExclusiveWithBufferLadder(candidate.index))
                                    return true;
                            }

                            Logger.Log($"All WASAPI exclusive format candidates failed for BASS device {bassDeviceId} (driver '{driver}').", name: "audio", level: LogLevel.Verbose);
                            return false;
                        }
                    }
                    else
                    {
                        // If the user selected a specific non-default device, do not fall back to system default.
                        // Fallback would likely be busy (e.g. browser playing on default), and would mask the real issue.
                        Logger.Log($"Could not map BASS device {bassDeviceId} (driver '{driver}') to a WASAPI output device; refusing to fall back to default (-1).", name: "audio",
                            level: LogLevel.Verbose);
                        return false;
                    }
                }
                else
                {
                    Logger.Log($"BASS device {bassDeviceId} did not provide a driver identifier; falling back to default WASAPI device (-1).", name: "audio", level: LogLevel.Verbose);
                }
            }

            if (wasapiDevice == -1)
                Logger.Log("Using default WASAPI device (-1).", name: "audio", level: LogLevel.Verbose);

            // To keep things in a sane state let's only keep one device initialised via wasapi.
            if (exclusive)
                return tryInitWasapiExclusiveWithBufferLadder(wasapiDevice);

            freeWasapi();
            return initWasapi(wasapiDevice, exclusive: false, bufferMs: 0);
        }

        private static IEnumerable<int> wasapiExclusiveBufferLadder(int requestedMs)
        {
            if (requestedMs > 0)
            {
                yield return requestedMs;

                for (int v = requestedMs * 2; v <= AudioOutputDefaults.WASAPI_EXCLUSIVE_MAX_BUFFER_MS; v *= 2)
                    yield return v;
            }
            else
            {
                for (int v = AudioOutputDefaults.WASAPI_EXCLUSIVE_MIN_BUFFER_MS; v <= AudioOutputDefaults.WASAPI_EXCLUSIVE_MAX_BUFFER_MS; v += 10)
                    yield return v;
            }
        }

        private bool tryInitWasapiExclusiveWithBufferLadder(int wasapiDevice)
        {
            int requestedMs = Manager?.WasapiExclusiveBufferMs.Value ?? AudioOutputDefaults.WASAPI_EXCLUSIVE_AUTO;
            int? firstAttemptMs = null;

            foreach (int bufferMs in wasapiExclusiveBufferLadder(requestedMs))
            {
                firstAttemptMs ??= bufferMs;
                bool onlyTransientFailures = true;

                for (int attempt = 0; attempt < WasapiExclusiveBufferLadder.TransientRetryDelaysMs.Length; attempt++)
                {
                    int delayMs = WasapiExclusiveBufferLadder.TransientRetryDelaysMs[attempt];

                    if (delayMs > 0)
                        Thread.Sleep(delayMs);

                    freeWasapi();

                    if (initWasapi(wasapiDevice, exclusive: true, bufferMs))
                    {
                        if (bufferMs != firstAttemptMs.Value)
                        {
                            Logger.Log(
                                $"WASAPI Exclusive buffer ladder stepped up from {firstAttemptMs}ms to {bufferMs}ms (requested={(requestedMs <= 0 ? "auto" : $"{requestedMs}ms")}).",
                                name: "audio", level: LogLevel.Important);
                        }

                        return true;
                    }

                    Errors error = lastWasapiInitError;

                    if (!WasapiExclusiveBufferLadder.IsTransientInitError(error))
                    {
                        onlyTransientFailures = false;
                        break;
                    }

                    Logger.Log(
                        $"WASAPI Exclusive init transient {error} at {bufferMs}ms (attempt {attempt + 1}/{WasapiExclusiveBufferLadder.TransientRetryDelaysMs.Length}).",
                        name: "audio",
                        level: LogLevel.Important);
                }

                if (onlyTransientFailures)
                {
                    Logger.Log(
                        $"WASAPI Exclusive init remained busy at {bufferMs}ms; not stepping the buffer ladder.",
                        name: "audio",
                        level: LogLevel.Important);
                    publishExclusiveWasapiInactive();
                    return false;
                }
            }

            publishExclusiveWasapiInactive();
            return false;
        }

        private void publishExclusiveWasapiInactive()
        {
            Manager?.SetWindowsOutputRuntime(new AudioManager.WindowsOutputRuntimeInfo(
                Mode: AudioOutputMode.WasapiExclusive,
                Active: false));
        }

        private bool initWasapi(int wasapiDevice, bool exclusive, int bufferMs)
        {
            try
            {
                wasapiExclusiveActive = exclusive;

                // BASSWASAPI flags:
                // - 0x1  = EXCLUSIVE
                // - 0x10 = EVENT (event-driven)
                // ManagedBass bindings used here do not currently expose Exclusive, so we use the documented value.
                const WasapiInitFlags exclusive_flag = (WasapiInitFlags)0x1;

                int requestedFrequency = 0;
                int requestedChannels = 0;

                // Shared mode can use event-driven callbacks and auto-format.
                // Exclusive mode should use an explicit supported format (freq/chans) from the chosen WASAPI entry.
                var flags = (WasapiInitFlags)0;

                if (exclusive)
                {
                    flags |= exclusive_flag;

                    if (wasapiDevice >= 0 && BassWasapi.GetDeviceInfo(wasapiDevice, out WasapiDeviceInfo selectedInfo))
                    {
                        requestedFrequency = selectedInfo.MixFrequency;
                        requestedChannels = selectedInfo.MixChannels;

                        int currentBassDevice = Bass.CurrentDevice;

                        if ((requestedFrequency <= 0 || requestedChannels <= 0) && currentBassDevice > 0)
                        {
                            string driver = Bass.GetDeviceInfo(currentBassDevice).Driver;
                            string deviceName = Bass.GetDeviceInfo(currentBassDevice).Name;
                            var mixFormat = HostAudioFormatQuery.TryGetMixFormat(driver, deviceName);

                            if (mixFormat != null)
                            {
                                requestedFrequency = mixFormat.Value.sampleRate;
                                requestedChannels = mixFormat.Value.channels;
                            }
                        }
                    }
                }
                else
                {
                    flags |= WasapiInitFlags.EventDriven | WasapiInitFlags.AutoFormat;
                }

                // Important: in exclusive mode, the underlying implementation may not support event-driven callbacks
                // and can fall back to polling. Using a near-zero period (float.Epsilon) can then cause a busy-loop,
                // leading to time running far too fast (and eventual instability elsewhere).
                // Shared mode keeps buffer=0 (OS minimum) and period=float.Epsilon.
                int periodMs = exclusive ? AudioOutputDefaults.WasapiExclusivePeriodMs(bufferMs) : 0;
                float bufferSeconds = exclusive ? bufferMs / 1000f : 0f;
                float periodSeconds = exclusive ? periodMs / 1000f : float.Epsilon;

                bool initialised = BassWasapi.Init(wasapiDevice, Frequency: requestedFrequency, Channels: requestedChannels, Procedure: wasapi_procedure_static, Flags: flags, Buffer: bufferSeconds,
                    Period: periodSeconds, User: wasapiUserPtr);

                if (!initialised)
                {
                    lastWasapiInitError = Bass.LastError;
                    wasapiExclusiveActive = false;
                    Logger.Log(
                        $"Initialising BassWasapi for device {wasapiDevice} (exclusive: {exclusive}, buffer: {bufferSeconds:0.###}s, period: {periodSeconds:0.###}s)...FAILED ({lastWasapiInitError})",
                        name: "audio", level: LogLevel.Verbose);
                    return false;
                }

                lastWasapiInitError = Errors.OK;
                Logger.Log(
                    $"Initialising BassWasapi for device {wasapiDevice} (exclusive: {exclusive}, buffer: {bufferSeconds:0.###}s, period: {periodSeconds:0.###}s)...success!",
                    name: "audio", level: LogLevel.Verbose);

                BassWasapi.GetInfo(out var wasapiInfo);
                wasapiPullSampleFormat = mapWasapiPullSampleFormat(wasapiInfo.Format);

                int bytesPerSample = wasapiInfo.Format switch
                {
                    WasapiFormat.Float or WasapiFormat.Bit32 => 4,
                    WasapiFormat.Bit24 => 3,
                    WasapiFormat.Bit16 => 2,
                    _ => 0
                };

                int actualBufferMs = bytesPerSample > 0 && wasapiInfo.Frequency > 0 && wasapiInfo.Channels > 0 && wasapiInfo.BufferLength > 0
                    ? (int)Math.Round(wasapiInfo.BufferLength * 1000d / (wasapiInfo.Frequency * wasapiInfo.Channels * bytesPerSample))
                    : bufferMs;

                if (exclusive)
                {
                    wasapiExclusiveRequestedBufferMs = bufferMs;
                    wasapiExclusiveAppliedBufferMs = actualBufferMs;
                    wasapiExclusivePeriodMs = periodMs;

                    var level = actualBufferMs != bufferMs ? LogLevel.Important : LogLevel.Verbose;
                    Logger.Log(
                        $"WASAPI Exclusive info: Freq={wasapiInfo.Frequency}, Chans={wasapiInfo.Channels}, Format={wasapiInfo.Format}, requested={bufferMs}ms applied={actualBufferMs}ms period={periodMs}ms",
                        name: "audio", level: level);
                }
                else
                {
                    Logger.Log($"WASAPI info: Freq={wasapiInfo.Frequency}, Chans={wasapiInfo.Channels}, Format={wasapiInfo.Format}", name: "audio", level: LogLevel.Verbose);
                }

                globalMixerHandle.Value = BassMix.CreateMixerStream(wasapiInfo.Frequency, wasapiInfo.Channels, BassFlags.MixerNonStop | BassFlags.Decode | BassFlags.Float);

                if (globalMixerHandle.Value == 0)
                {
                    lastWasapiInitError = Bass.LastError;
                    Logger.Log($"Failed to create WASAPI mixer stream for device {wasapiDevice}: {lastWasapiInitError}", name: "audio", level: LogLevel.Error);
                    freeWasapi();
                    return false;
                }

                BassWasapi.SetNotify(wasapi_notify_procedure_static, wasapiUserPtr);

                if (!BassWasapi.Start())
                {
                    lastWasapiInitError = Bass.LastError;
                    Logger.Log($"BassWasapi.Start() failed for device {wasapiDevice}: {lastWasapiInitError}", name: "audio", level: LogLevel.Error);
                    freeWasapi();
                    return false;
                }

                if (exclusive)
                {
                    if (OperatingSystem.IsWindows())
                        attachExclusiveEndpointGain(wasapiDevice, globalMixerHandle.Value);

                    Manager?.SetWindowsOutputRuntime(new AudioManager.WindowsOutputRuntimeInfo(
                        Mode: AudioOutputMode.WasapiExclusive,
                        Active: true,
                        WasapiBufferMs: wasapiExclusiveAppliedBufferMs > 0 ? wasapiExclusiveAppliedBufferMs : null,
                        WasapiPeriodMs: wasapiExclusivePeriodMs > 0 ? wasapiExclusivePeriodMs : null,
                        WasapiRequestedBufferMs: wasapiExclusiveRequestedBufferMs));
                }

                return true;
            }
            catch (DllNotFoundException e)
            {
                lastWasapiInitError = Bass.LastError;
                logWasapiNativeUnavailableOnce($"WASAPI output is unavailable because basswasapi.dll could not be loaded ({e.Message}).");
                freeWasapi();
                return false;
            }
            catch (EntryPointNotFoundException e)
            {
                lastWasapiInitError = Bass.LastError;
                logWasapiNativeUnavailableOnce($"WASAPI output is unavailable because basswasapi.dll is incompatible/mismatched ({e.Message}).");
                freeWasapi();
                return false;
            }
            catch (Exception e)
            {
                lastWasapiInitError = Bass.LastError;
                Logger.Log($"WASAPI init failed with exception: {e}", name: "audio", level: LogLevel.Error);
                freeWasapi();
                return false;
            }
        }

        private static AcousticLevelMath.SampleFormat mapWasapiPullSampleFormat(WasapiFormat format) =>
            format switch
            {
                WasapiFormat.Float => AcousticLevelMath.SampleFormat.IeeeFloat32,
                WasapiFormat.Bit16 => AcousticLevelMath.SampleFormat.Pcm16,
                WasapiFormat.Bit24 => AcousticLevelMath.SampleFormat.Pcm24,
                WasapiFormat.Bit32 => AcousticLevelMath.SampleFormat.Pcm32,
                _ => AcousticLevelMath.SampleFormat.Unsupported
            };

        private void freeWasapi()
        {
            // basswasapi ships only for Windows. This runs on every device init, so on
            // Linux/macOS the runtime searches for libbasswasapi.so and fails the startup.
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return;

            wasapiPendingRecoveryInit?.Cancel();
            wasapiPendingRecoveryInit = null;

            int? mixerToFree = globalMixerHandle.Value;
            bool hadWasapi = wasapiExclusiveActive || mixerToFree != null;

            try
            {
                // The mixer probably doesn't need to be recycled. Just keeping things sane for now.
                // Stop WASAPI first to prevent callbacks from accessing disposed resources.
                BassWasapi.SetNotify(null, IntPtr.Zero);
                BassWasapi.Stop();
                BassWasapi.Free();

                if (mixerToFree != null)
                    Bass.StreamFree(mixerToFree.Value);
            }
            catch (DllNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI cleanup failed because basswasapi.dll could not be loaded ({e.Message}).");
            }
            catch (EntryPointNotFoundException e)
            {
                logWasapiNativeUnavailableOnce($"WASAPI cleanup failed because basswasapi.dll is incompatible/mismatched ({e.Message}).");
            }
            catch (Exception e)
            {
                Logger.Log($"WASAPI cleanup failed with exception: {e}", name: "audio", level: LogLevel.Error);
            }
            finally
            {
                exclusiveEndpointGain?.Dispose();
                exclusiveEndpointGain = null;

                if (hadWasapi)
                    wasapiOrAsioWasActive = true;

                wasapiExclusiveActive = false;
                wasapiExclusiveRequestedBufferMs = 0;
                wasapiExclusiveAppliedBufferMs = 0;
                wasapiExclusivePeriodMs = 0;
                globalMixerHandle.Value = null;
                Thread.Sleep(50);
            }
        }

        [SupportedOSPlatform("windows")]
        private void attachExclusiveEndpointGain(int wasapiDevice, int? mixerHandle)
        {
            exclusiveEndpointGain?.Dispose();
            exclusiveEndpointGain = null;

            if (!isValidBassHandle(mixerHandle))
                return;

            string? endpointId = null;

            if (wasapiDevice >= 0 && BassWasapi.GetDeviceInfo(wasapiDevice, out WasapiDeviceInfo info))
                endpointId = info.ID;

            exclusiveEndpointGain = WasapiExclusiveEndpointGain.TryAttach(endpointId, mixerHandle!.Value, Scheduler);
        }

        private bool initNAudioDefault(int bassDeviceId)
        {
            if (!OperatingSystem.IsWindows())
                return false;

            freeNAudioDefault();

            try
            {
                naudioDefaultOutput = new NAudioWasapiOutput();

                // Virtual endpoints often reject an immediate reopen after BassWasapi/NAudio Stop.
                // BASS HSTREAM is a DWORD; ManagedBass exposes it as signed int — negative values are valid.
                int? mixer = null;
                int[] delaysMs = [0, 100, 250];

                for (int attempt = 0; attempt < delaysMs.Length; attempt++)
                {
                    if (delaysMs[attempt] > 0)
                        Thread.Sleep(delaysMs[attempt]);

                    mixer = naudioDefaultOutput.Start(bassDeviceId);

                    if (isValidBassHandle(mixer))
                        break;

                    Logger.Log(
                        $"NAudio default output start attempt {attempt + 1}/{delaysMs.Length} failed for BASS device {bassDeviceId} (returned={mixer?.ToString() ?? "null"}).",
                        name: "audio",
                        level: LogLevel.Important);
                }

                if (!isValidBassHandle(mixer))
                {
                    freeNAudioDefault();
                    return false;
                }

                globalMixerHandle.Value = mixer;
                naudioBoundBassDeviceId = bassDeviceId;
                naudioBoundEndpointId = naudioDefaultOutput.BoundEndpointId;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"NAudio default output init failed: {ex}", name: "audio", level: LogLevel.Error);
                freeNAudioDefault();
                return false;
            }
        }

        /// <summary>
        /// BASS channel/mixer handles are DWORD-sized; ManagedBass stores them in signed <see cref="int"/>.
        /// Zero is invalid; any non-zero value (including negative) is a live handle.
        /// </summary>
        private static bool isValidBassHandle(int? handle) => handle is int h && h != 0;

        private void freeNAudioDefault()
        {
            if (naudioDefaultOutput != null)
                wasapiOrAsioWasActive = true;

            naudioBoundBassDeviceId = null;
            naudioBoundEndpointId = null;

            if (naudioDefaultOutput == null)
                return;

            if (!OperatingSystem.IsWindows())
            {
                naudioDefaultOutput = null;
                return;
            }

            int? mixerOwnedByNAudio = naudioDefaultOutput.MixerHandle;

            try
            {
                naudioDefaultOutput.Stop();
            }
            catch (Exception ex)
            {
                Logger.Log($"NAudio default output cleanup failed: {ex.Message}", name: "audio", level: LogLevel.Important);
            }
            finally
            {
                naudioDefaultOutput.Dispose();
                naudioDefaultOutput = null;

                // Only clear the global handle if it still points at the NAudio-owned mixer.
                if (mixerOwnedByNAudio != null && globalMixerHandle.Value == mixerOwnedByNAudio)
                    globalMixerHandle.Value = null;
            }
        }

        private static void logWasapiNativeUnavailableOnce(string message)
        {
            if (Interlocked.Exchange(ref wasapiNativeUnavailableLogged, 1) == 1)
                return;

            Logger.Log(message, name: "audio", level: LogLevel.Error);
        }

        /// <summary>
        /// Reconfigures the active ASIO device without releasing all BASS output devices.
        /// </summary>
        internal bool TryReconfigureAsio(int asioDeviceIndex, double preferredSampleRate, int bitDepth, int bufferSize)
        {
            Debug.Assert(ThreadSafety.IsAudioThread);

            if (Manager == null)
                return false;

            if (EzAsioDeviceManager.ActiveDeviceIndex != asioDeviceIndex)
                return false;

            preferredSampleRate = EzAsioDeviceManager.GetTargetSampleRate(Manager.SampleRate.Value == 0 ? preferredSampleRate : Manager.SampleRate.Value);
            bitDepth = EzAsioDeviceManager.GetTargetBitDepth(bitDepth);
            bool useExternalPCM = Manager.AsioUseExternalPCM.Value;
            int? resolvedBufferSize = useExternalPCM ? null : EzAsioDeviceManager.GetTargetBufferSize(bufferSize);

            releaseAsioMixerOnly();

            // External PCM must re-probe the driver (rate/bit depth/buffer caps); the buffer-only short path cannot refresh driver metadata.
            bool formatChangeRequired = useExternalPCM || requiresAsioFormatChange(preferredSampleRate, bitDepth);

            if (!formatChangeRequired)
            {
                LastAsioReconfigurePath = AsioReconfigurePath.BufferRestart;

                if (!EzAsioDeviceManager.TryRestartWithBuffer(resolvedBufferSize))
                    return false;

                return completeAsioMixerAndStart(resolvedBufferSize);
            }

            LastAsioReconfigurePath = AsioReconfigurePath.FullReconfigure;

            if (!EzAsioDeviceManager.ReconfigureDevice(asioDeviceIndex, preferredSampleRate, bitDepth, resolvedBufferSize, useExternalPCM))
                return false;

            return completeAsioMixerAndStart(resolvedBufferSize);
        }

        private static bool requiresAsioFormatChange(double preferredSampleRate, int bitDepth)
        {
            if (EzAsioDeviceManager.GetCurrentDeviceInfo() == null)
                return true;

            double currentRate = EzAsioDeviceManager.GetCurrentSampleRate();

            if (currentRate <= 0 || Math.Abs(currentRate - preferredSampleRate) >= 1.0)
                return true;

            return EzAsioDeviceManager.TargetBitDepth != bitDepth;
        }

        private bool initAsio(int asioDeviceIndex, double preferredSampleRate, int bitDepth, string asioDeviceName)
        {
            freeAsio();

            if (Manager == null)
                return false;

            preferredSampleRate = EzAsioDeviceManager.GetTargetSampleRate(Manager.SampleRate.Value == 0 ? preferredSampleRate : Manager.SampleRate.Value);
            bitDepth = EzAsioDeviceManager.GetTargetBitDepth(bitDepth);
            bool useExternalPCM = Manager.AsioUseExternalPCM.Value;
            int? bufferSize = useExternalPCM ? null : EzAsioDeviceManager.GetTargetBufferSize(Manager.AsioBufferSize.Value);

            if (EzAsioDeviceManager.IsDeviceRunning())
            {
                Logger.Log("ASIO device already running, stopping before initialization", name: "audio", level: LogLevel.Debug);
                EzAsioDeviceManager.StopDevice();
                Thread.Sleep(100);
            }

            if (EzAsioDeviceManager.RequiresVirtualHostWarmUp(asioDeviceName))
                warmUpHostAudioForVirtualAsio(asioDeviceName);

            if (!ensureBassOutputDeviceReady(Bass.CurrentDevice))
            {
                Logger.Log($"BASS output device {Bass.CurrentDevice} is not initialised after ASIO warm-up; cannot create mixer.", name: "audio", level: LogLevel.Error);
                return false;
            }

            if (!EzAsioDeviceManager.InitializeDevice(asioDeviceIndex, preferredSampleRate, bufferSize, bitDepth, useExternalPCM, waitForDevice: true, waitTimeoutMs: 10_000))
            {
                Logger.Log($"EzAsioDeviceManager.InitializeDevice({asioDeviceIndex}, {preferredSampleRate}, {bufferSize}, {bitDepth}, externalPCM={useExternalPCM}) failed", name: "audio", level: LogLevel.Error);
                return false;
            }

            return completeAsioMixerAndStart(bufferSize);
        }

        private bool completeAsioMixerAndStart(int? bufferSize)
        {
            if (Manager == null)
                return false;

            var deviceInfo = EzAsioDeviceManager.GetCurrentDeviceInfo();

            if (deviceInfo == null)
            {
                Logger.Log("Unable to get ASIO device info after initialisation", name: "audio", level: LogLevel.Error);
                freeAsio();
                return false;
            }

            int outputChannels = Math.Max(1, deviceInfo.Value.Outputs);

            if (outputChannels < 2)
            {
                Logger.Log($"ASIO device has insufficient output channels ({outputChannels}); stereo requires at least 2", name: "audio", level: LogLevel.Important);
                freeAsio();
                FreeDevice(Bass.CurrentDevice);
                return false;
            }

            double sampleRate = BassAsio.Rate;

            if (sampleRate <= 0 || sampleRate > 1000000 || double.IsNaN(sampleRate) || double.IsInfinity(sampleRate))
            {
                Logger.Log($"Invalid ASIO sample rate ({sampleRate}Hz); cannot start output.", name: "audio", level: LogLevel.Error);
                freeAsio();
                return false;
            }

            const int mixer_channels = 2;
            globalMixerHandle.Value = BassMix.CreateMixerStream((int)sampleRate, mixer_channels, BassFlags.MixerNonStop | BassFlags.Decode | BassFlags.Float);

            if (globalMixerHandle.Value == 0)
            {
                var mixerError = Bass.LastError;
                Logger.Log($"Failed to create ASIO mixer stream: {(int)mixerError} ({mixerError}), sampleRate={sampleRate}", name: "audio", level: LogLevel.Error);
                freeAsio();
                return false;
            }

            // Decode mixers cannot be started with ChannelPlay (BASS returns Decode). ASIO output pulls from the mixer via BassAsio, same as WASAPI.
            EzAsioDeviceManager.SetGlobalMixerHandle(globalMixerHandle.Value.Value);

            if (!EzAsioDeviceManager.StartDevice(bufferSize))
            {
                Logger.Log("EzAsioDeviceManager.StartDevice() failed", name: "audio", level: LogLevel.Error);
                freeAsio();
                return false;
            }

            int activeBufferSize = EzAsioDeviceManager.ActiveBufferSize > 0 ? EzAsioDeviceManager.ActiveBufferSize : bufferSize ?? 0;
            int activeBitDepth = EzAsioDeviceManager.TargetBitDepth;

            Manager.OnAsioDeviceConfigurationChanged?.Invoke(sampleRate, activeBufferSize, activeBitDepth);
            Manager.OnAsioDeviceInitialized?.Invoke(sampleRate);
            return true;
        }

        private void releaseAsioMixerOnly()
        {
            if (globalMixerHandle.Value != null)
            {
                try
                {
                    Bass.StreamFree(globalMixerHandle.Value.Value);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception freeing ASIO mixer: {ex.Message}", name: "audio", level: LogLevel.Error);
                }

                globalMixerHandle.Value = null;
            }

            EzAsioDeviceManager.StopDevice();
        }

        private void warmUpHostAudioForVirtualAsio(string asioDeviceName)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return;

            int targetDevice = findMatchingBassDeviceId(asioDeviceName) ?? Bass.CurrentDevice;

            if (targetDevice < 0)
                return;

            int previousDevice = Bass.CurrentDevice;

            try
            {
                // Waking the matching host endpoint can help virtual ASIO drivers (ASIO4ALL, Voicemeeter, etc.).
                // Never Bass.Free() the device we are already using for mixer creation — that leaves BASS uninitialised and produces silent output.
                if (targetDevice == previousDevice)
                {
                    Logger.Log($"Warming up active BASS device {targetDevice} before ASIO initialisation", name: "audio", level: LogLevel.Debug);

                    if (attemptWasapiInitialisation(targetDevice, exclusive: false))
                        Thread.Sleep(50);

                    freeWasapi();
                    return;
                }

                Logger.Log($"Warming up host BASS device {targetDevice} (active device {previousDevice}) before ASIO initialisation", name: "audio", level: LogLevel.Debug);

                Bass.CurrentDevice = targetDevice;

                if (!Bass.Init(targetDevice) && Bass.LastError != Errors.Already)
                    return;

                if (attemptWasapiInitialisation(targetDevice, exclusive: false))
                    Thread.Sleep(50);

                freeWasapi();
                Bass.Free();
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                Logger.Log($"Host audio warm-up before ASIO failed: {ex.Message}", name: "audio", level: LogLevel.Debug);
            }
            finally
            {
                ensureBassOutputDeviceReady(previousDevice);
            }
        }

        private static int? findMatchingBassDeviceId(string asioDeviceName)
        {
            string normalisedAsio = normaliseDeviceNameForMatch(asioDeviceName);

            for (int deviceId = 2; deviceId < Bass.DeviceCount; deviceId++)
            {
                if (!Bass.GetDeviceInfo(deviceId, out DeviceInfo info) || !info.IsEnabled)
                    continue;

                if (deviceNamesMatch(normalisedAsio, normaliseDeviceNameForMatch(info.Name)))
                    return deviceId;
            }

            return null;
        }

        private static bool ensureBassOutputDeviceReady(int deviceId)
        {
            if (deviceId < 0)
                return false;

            try
            {
                Bass.CurrentDevice = deviceId;

                if (Bass.GetDeviceInfo(deviceId, out DeviceInfo info) && info.IsInitialized)
                    return true;

                if (Bass.Init(deviceId))
                    return true;

                if (Bass.LastError == Errors.Already && Bass.GetDeviceInfo(deviceId, out info))
                    return info.IsInitialized;

                Logger.Log($"Failed to ensure BASS device {deviceId} is initialised: {Bass.LastError}", name: "audio", level: LogLevel.Error);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception ensuring BASS device {deviceId}: {ex}", name: "audio", level: LogLevel.Error);
                return false;
            }
        }

        private static string normaliseDeviceNameForMatch(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            return name.Replace("(ASIO)", string.Empty, StringComparison.OrdinalIgnoreCase)
                       .Replace("(WASAPI Exclusive)", string.Empty, StringComparison.OrdinalIgnoreCase)
                       .Trim();
        }

        private static bool deviceNamesMatch(string asioName, string bassName)
        {
            if (string.IsNullOrEmpty(asioName) || string.IsNullOrEmpty(bassName))
                return false;

            if (asioName.Contains(bassName, StringComparison.OrdinalIgnoreCase) || bassName.Contains(asioName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Virtual ASIO drivers often share a prefix with the underlying device (Voicemeeter, ASIO4ALL, etc.).
            static string firstToken(string value) => value.Split(new[] { ' ', '(', ')' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? value;

            return string.Equals(firstToken(asioName), firstToken(bassName), StringComparison.OrdinalIgnoreCase);
        }

        private static IOrderedEnumerable<(int index, int freq, int chans)> orderWasapiCandidates(IEnumerable<(int index, int freq, int chans)> candidates)
            => candidates.OrderBy(c => c.chans == 2 ? 0 : 1)
                         .ThenBy(c => Math.Abs(c.freq - AudioOutputDefaults.DEFAULT_SAMPLE_RATE))
                         .ThenBy(c => Math.Abs(c.freq - AudioOutputDefaults.SECONDARY_SAMPLE_RATE));

        private void freeAsio()
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows)
                return;

            Logger.Log("Freeing ASIO device", name: "audio", level: LogLevel.Important);
            wasapiOrAsioWasActive = true;

            try
            {
                EzAsioDeviceManager.StopDevice();

                Thread.Sleep(50);

                EzAsioDeviceManager.FreeDevice();
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception while cleaning up ASIO device: {ex.Message}; attempting force reset", name: "audio", level: LogLevel.Error);

                try
                {
                    EzAsioDeviceManager.ForceReset();
                }
                catch (Exception resetEx)
                {
                    Logger.Log($"Force reset also failed: {resetEx.Message}", name: "audio", level: LogLevel.Error);
                }
            }

            if (globalMixerHandle.Value != null)
            {
                try
                {
                    Bass.StreamFree(globalMixerHandle.Value.Value);
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception while freeing ASIO mixer: {ex.Message}", name: "audio", level: LogLevel.Error);
                }

                globalMixerHandle.Value = null;
            }

            // Extra delay after ASIO teardown to avoid busy-driver errors when switching devices.
            if (!DebugUtils.IsNUnitRunning)
                Thread.Sleep(300);
        }

        private void updateAudioManagers()
        {
            AudioManager[] currentManagers;

            lock (managers)
                currentManagers = managers.ToArray();

            int currentDevice = Bass.CurrentDevice;

            foreach (var manager in currentManagers)
                manager.UpdateDevice(currentDevice);
        }

        #endregion
    }
}
