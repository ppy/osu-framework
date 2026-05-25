// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ManagedBass;
using ManagedBass.Asio;
using osu.Framework.Logging;

namespace osu.Framework.Audio.Asio
{
    /// <summary>
    /// 管理 ASIO 设备的初始化、启动与释放。
    /// </summary>
    public static class EzAsioDeviceManager
    {
        public const int DEFAULT_SAMPLE_RATE = AudioOutputDefaults.DEFAULT_SAMPLE_RATE;
        public const int DEFAULT_BUFFER_SIZE = AudioOutputDefaults.DEFAULT_ASIO_BUFFER_SIZE;
        private const string device_selection_postfix = " (ASIO)";

        private const int max_retry_count = 4;
        private const int retry_delay_ms = 100;
        private const int max_backoff_delay_ms = 1000;
        private const int device_free_delay_ms = 100;
        private const double sample_rate_tolerance = 1.0;
        private static readonly AsioProcedure asio_callback = asioProcedure;

        private static readonly object sync_root = new object();

        private static int globalMixerHandle;
        private static double? requestedSampleRate;
        private static int? requestedBufferSize;
        private static int? appliedBufferSize;

        public static int TargetSampleRate => (int)Math.Round(requestedSampleRate ?? DEFAULT_SAMPLE_RATE);

        public static int TargetBitDepth { get; private set; } = 24;

        public static int ActiveBufferSize => appliedBufferSize ?? 0;

        public static int? ActiveDeviceIndex { get; private set; }

        public static bool IsAvailable
        {
            get
            {
                try
                {
                    _ = BassAsio.DeviceCount;
                    return true;
                }
                catch (DllNotFoundException)
                {
                    return false;
                }
                catch (EntryPointNotFoundException)
                {
                    return false;
                }
                catch (BadImageFormatException)
                {
                    return false;
                }
            }
        }

        public static int GetTargetSampleRate(double? sampleRate) => sampleRate is > 0 ? (int)Math.Round(sampleRate.Value) : DEFAULT_SAMPLE_RATE;

        public static int GetTargetBufferSize(int? bufferSize) => bufferSize is > 0 ? bufferSize.Value : DEFAULT_BUFFER_SIZE;

        public static int GetTargetBitDepth(int? bitDepth) => bitDepth is 16 or 24 ? bitDepth.Value : 24;

        public static AsioSampleFormat GetSampleFormatForBitDepth(int bitDepth) => bitDepth <= 16 ? AsioSampleFormat.Bit16 : AsioSampleFormat.Bit24;

        public static bool TryParseDeviceSelection(string selection, out string deviceName)
        {
            deviceName = string.Empty;

            if (string.IsNullOrEmpty(selection))
                return false;

            if (!selection.EndsWith(device_selection_postfix, StringComparison.Ordinal))
                return false;

            deviceName = selection[..^device_selection_postfix.Length];
            return !string.IsNullOrEmpty(deviceName);
        }

        public static void SetGlobalMixerHandle(int mixerHandle)
        {
            lock (sync_root)
                globalMixerHandle = mixerHandle;

            Logger.Log($"ASIO global mixer handle set: {mixerHandle}", LoggingTarget.Runtime, LogLevel.Debug);
        }

        public static IEnumerable<(int Index, string Name)> AvailableDevices
        {
            get
            {
                if (!IsAvailable)
                    yield break;

                for (int i = 0; i < BassAsio.DeviceCount; i++)
                {
                    if (BassAsio.GetDeviceInfo(i, out AsioDeviceInfo info))
                        yield return (i, info.Name);
                }
            }
        }

        public static bool InitializeDevice(int deviceIndex, double? sampleRateToTry = null, int? bufferSize = null, int? bitDepth = null, bool waitForDevice = false,
                                            int waitTimeoutMs = 30000, bool aggressive = false)
        {
            lock (sync_root)
            {
                if (!IsAvailable)
                    return false;

                try
                {
                    if (!tryGetDeviceInfo(deviceIndex, out AsioDeviceInfo deviceInfo))
                        return false;

                    Logger.Log($"Initializing ASIO device: {deviceInfo.Name} (Driver: {deviceInfo.Driver})", LoggingTarget.Runtime, LogLevel.Important);

                    freeDeviceInternal(resetMixerHandle: false);

                    if (!tryInitialiseDevice(deviceIndex, waitForDevice, waitTimeoutMs, aggressive))
                        return false;

                    TargetBitDepth = GetTargetBitDepth(bitDepth);

                    double successfulRate = tryApplySampleRate(sampleRateToTry ?? DEFAULT_SAMPLE_RATE);

                    if (successfulRate <= 0)
                    {
                        freeDeviceInternal(resetMixerHandle: false);
                        return false;
                    }

                    requestedSampleRate = sampleRateToTry ?? successfulRate;
                    requestedBufferSize = normaliseRequestedBufferSize(bufferSize);
                    appliedBufferSize = null;
                    ActiveDeviceIndex = deviceIndex;

                    if (BassAsio.GetInfo(out AsioInfo info))
                    {
                        Logger.Log(
                            $"ASIO device ready: rate={successfulRate}Hz, bitDepth={TargetBitDepth}, outputs={info.Outputs}, buffer(min={info.MinBufferLength}, preferred={info.PreferredBufferLength}, max={info.MaxBufferLength}, granularity={info.BufferLengthGranularity})",
                            LoggingTarget.Runtime,
                            LogLevel.Debug);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception during ASIO device initialization: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                    return false;
                }
            }
        }

        /// <summary>
        /// Re-initialises the current ASIO device with new format settings without releasing all BASS outputs.
        /// Call <see cref="StartDevice"/> afterwards when the global mixer handle is assigned.
        /// </summary>
        public static bool ReconfigureDevice(int deviceIndex, double? sampleRateToTry, int? bitDepth, int? bufferSize)
        {
            lock (sync_root)
            {
                if (!IsAvailable)
                    return false;

                if (ActiveDeviceIndex != deviceIndex)
                    return false;

                try
                {
                    if (BassAsio.IsStarted)
                        BassAsio.Stop();

                    resetOutputRouting();
                    BassAsio.Free();
                    sleepWithBackoff(2);
                    ActiveDeviceIndex = null;

                    if (!tryInitialiseDevice(deviceIndex, waitForDevice: false, waitTimeoutMs: 0, aggressive: true))
                        return false;

                    TargetBitDepth = GetTargetBitDepth(bitDepth);

                    double successfulRate = tryApplySampleRate(sampleRateToTry ?? requestedSampleRate ?? DEFAULT_SAMPLE_RATE);

                    if (successfulRate <= 0)
                    {
                        freeDeviceInternal(resetMixerHandle: false);
                        return false;
                    }

                    requestedSampleRate = sampleRateToTry ?? successfulRate;
                    requestedBufferSize = normaliseRequestedBufferSize(bufferSize);
                    appliedBufferSize = null;
                    ActiveDeviceIndex = deviceIndex;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception during ASIO device reconfiguration: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns supported sample-rate / bit-depth combinations for the given ASIO device.
        /// May briefly initialise the driver to probe rates when the device is not already active.
        /// </summary>
        public static IReadOnlyList<EzAsioFormatOption> GetSupportedFormats(int deviceIndex)
        {
            if (!IsAvailable)
                return Array.Empty<EzAsioFormatOption>();

            lock (sync_root)
            {
                bool probeOnly = ActiveDeviceIndex != deviceIndex || !BassAsio.GetInfo(out _);

                try
                {
                    if (probeOnly)
                    {
                        freeDeviceInternal(resetMixerHandle: false);

                        if (!tryInitialiseDevice(deviceIndex, waitForDevice: false, waitTimeoutMs: 0, aggressive: true))
                            return Array.Empty<EzAsioFormatOption>();
                    }

                    var formats = new List<EzAsioFormatOption>();

                    foreach (int rate in EzAsioFormatOption.COMMON_SAMPLE_RATES)
                    {
                        if (!BassAsio.CheckRate(rate))
                            continue;

                        foreach (int bits in EzAsioFormatOption.SUPPORTED_BIT_DEPTHS)
                            formats.Add(new EzAsioFormatOption(rate, bits));
                    }

                    if (probeOnly)
                        freeDeviceInternal(resetMixerHandle: false);

                    return formats;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception probing ASIO formats: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                    return Array.Empty<EzAsioFormatOption>();
                }
            }
        }

        /// <summary>
        /// Updates the requested buffer size and restarts output without freeing the ASIO driver.
        /// Requires the device to already be initialised and a valid global mixer handle.
        /// </summary>
        public static bool TryRestartWithBuffer(int? bufferSize)
        {
            lock (sync_root)
            {
                if (!IsAvailable || ActiveDeviceIndex == null)
                    return false;

                try
                {
                    if (BassAsio.IsStarted)
                        BassAsio.Stop();

                    requestedBufferSize = normaliseRequestedBufferSize(bufferSize);
                    appliedBufferSize = null;
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception preparing ASIO buffer restart: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                    return false;
                }
            }
        }

        public static bool StartDevice(int? bufferSize = null, int processingThreads = 1)
        {
            lock (sync_root)
            {
                if (!IsAvailable)
                    return false;

                try
                {
                    int mixerHandle = globalMixerHandle;

                    if (mixerHandle == 0)
                    {
                        Logger.Log("ASIO cannot start because no global mixer handle has been assigned.", LoggingTarget.Runtime, LogLevel.Error);
                        return false;
                    }

                    if (BassAsio.IsStarted)
                        BassAsio.Stop();

                    if (!configureOutputRouting(mixerHandle))
                        return false;

                    int resolvedBufferSize = resolveBufferSize(bufferSize ?? requestedBufferSize);
                    int threadCount = processingThreads > 0 ? processingThreads : 1;

                    if (!BassAsio.Start(resolvedBufferSize, threadCount))
                    {
                        Logger.Log(
                            $"Failed to start ASIO device (buffer={resolvedBufferSize}, threads={threadCount}): {BassAsio.LastError} (Code: {(int)BassAsio.LastError})",
                            LoggingTarget.Runtime,
                            LogLevel.Error);
                        resetOutputRouting();
                        return false;
                    }

                    appliedBufferSize = resolvedBufferSize;

                    Logger.Log($"ASIO device started successfully with buffer size {resolvedBufferSize} samples", LoggingTarget.Runtime, LogLevel.Important);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception during ASIO device start: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                    return false;
                }
            }
        }

        public static void StopDevice()
        {
            lock (sync_root)
            {
                if (!IsAvailable)
                    return;

                try
                {
                    if (BassAsio.IsStarted)
                        BassAsio.Stop();
                }
                catch (DllNotFoundException)
                {
                }
                catch (EntryPointNotFoundException)
                {
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception during ASIO device stop: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                }
            }
        }

        public static void FreeDevice()
        {
            lock (sync_root)
            {
                if (!IsAvailable)
                {
                    appliedBufferSize = null;
                    globalMixerHandle = 0;
                    return;
                }

                freeDeviceInternal(resetMixerHandle: true);
            }
        }

        public static void ForceReset()
        {
            lock (sync_root)
            {
                if (!IsAvailable)
                {
                    appliedBufferSize = null;
                    globalMixerHandle = 0;
                    return;
                }

                freeDeviceInternal(resetMixerHandle: true);
            }
        }

        public static bool IsDeviceRunning()
        {
            lock (sync_root)
            {
                if (!IsAvailable)
                    return false;

                try
                {
                    return BassAsio.IsStarted;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static AsioInfo? GetCurrentDeviceInfo()
        {
            lock (sync_root)
            {
                if (!IsAvailable)
                    return null;

                try
                {
                    return BassAsio.GetInfo(out AsioInfo info) ? info : null;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception getting ASIO device info: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                    return null;
                }
            }
        }

        public static double GetCurrentSampleRate()
        {
            lock (sync_root)
            {
                if (!IsAvailable)
                    return 0;

                try
                {
                    return BassAsio.Rate;
                }
                catch (Exception ex)
                {
                    Logger.Log($"Exception getting ASIO sample rate: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                    return 0;
                }
            }
        }

        public static IEnumerable<(int Index, string Name)> EnumerateAsioDevices()
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Windows || !IsAvailable)
                return Enumerable.Empty<(int, string)>();

            try
            {
                return AvailableDevices;
            }
            catch (DllNotFoundException)
            {
                return Enumerable.Empty<(int, string)>();
            }
            catch (EntryPointNotFoundException)
            {
                return Enumerable.Empty<(int, string)>();
            }
            catch (Exception ex)
            {
                Logger.Log($"Unexpected error enumerating ASIO devices: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                return Enumerable.Empty<(int, string)>();
            }
        }

        public static int? FindAsioDeviceIndex(string deviceName)
        {
            foreach (var device in EnumerateAsioDevices())
            {
                if (device.Name == deviceName)
                    return device.Index;
            }

            return null;
        }

        private static bool tryGetDeviceInfo(int deviceIndex, out AsioDeviceInfo deviceInfo)
        {
            deviceInfo = default;

            if (deviceIndex < 0 || deviceIndex >= BassAsio.DeviceCount)
            {
                Logger.Log($"Invalid ASIO device index: {deviceIndex} (DeviceCount: {BassAsio.DeviceCount})", LoggingTarget.Runtime, LogLevel.Error);
                return false;
            }

            if (!BassAsio.GetDeviceInfo(deviceIndex, out deviceInfo))
            {
                Logger.Log($"Failed to get ASIO device info for index {deviceIndex}", LoggingTarget.Runtime, LogLevel.Error);
                return false;
            }

            return true;
        }

        private static bool tryInitialiseDevice(int deviceIndex, bool waitForDevice, int waitTimeoutMs, bool aggressive)
        {
            var flagsCandidates = new[] { AsioInitFlags.Thread, AsioInitFlags.None };
            DateTime deadline = waitForDevice ? DateTime.UtcNow.AddMilliseconds(waitTimeoutMs) : DateTime.UtcNow;

            do
            {
                foreach (var flags in flagsCandidates)
                {
                    if (tryInitialiseDeviceWithFlags(deviceIndex, flags, aggressive))
                        return true;

                    freeDeviceInternal(resetMixerHandle: false);
                    sleepWithBackoff(1);
                }

                if (!waitForDevice)
                    break;
            } while (DateTime.UtcNow < deadline);

            Logger.Log($"Failed to initialise ASIO device {deviceIndex}", LoggingTarget.Runtime, LogLevel.Error);
            return false;
        }

        private static bool tryInitialiseDeviceWithFlags(int deviceIndex, AsioInitFlags flags, bool aggressive)
        {
            int attempts = aggressive ? 1 : max_retry_count;

            for (int attempt = 1; attempt <= attempts; attempt++)
            {
                if (BassAsio.Init(deviceIndex, flags))
                    return true;

                Errors error = BassAsio.LastError;

                Logger.Log(
                    $"ASIO initialisation failed (device={deviceIndex}, flags={flags}, attempt={attempt}/{attempts}): {error} (Code: {(int)error}) - {getAsioErrorDescription((int)error)}",
                    LoggingTarget.Runtime,
                    LogLevel.Important);

                if (error != Errors.Busy && error != Errors.Already && (int)error != 3)
                    return false;

                if (attempt < attempts)
                    sleepWithBackoff(attempt);
            }

            return false;
        }

        private static void sleepWithBackoff(int attempt)
        {
            int delay = Math.Min(max_backoff_delay_ms, retry_delay_ms * (1 << Math.Max(0, attempt - 1)));
            Thread.Sleep(delay);
        }

        private static double tryApplySampleRate(double desiredRate)
        {
            if (!BassAsio.CheckRate(desiredRate))
            {
                Logger.Log($"Requested ASIO sample rate {desiredRate}Hz is not supported; using device rate if available.", LoggingTarget.Runtime, LogLevel.Important);
                return tryGetUsableDeviceRate();
            }

            try
            {
                BassAsio.Rate = desiredRate;
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to set ASIO sample rate to {desiredRate}Hz: {ex.Message}", LoggingTarget.Runtime, LogLevel.Important);
                return tryGetUsableDeviceRate();
            }

            if (BassAsio.LastError != Errors.OK)
            {
                Logger.Log($"Failed to set ASIO sample rate to {desiredRate}Hz: {BassAsio.LastError}", LoggingTarget.Runtime, LogLevel.Error);
                return tryGetUsableDeviceRate();
            }

            double actualRate = BassAsio.Rate;

            if (Math.Abs(actualRate - desiredRate) >= sample_rate_tolerance)
            {
                Logger.Log($"ASIO device reported a different sample rate after set: requested={desiredRate}Hz, actual={actualRate}Hz", LoggingTarget.Runtime, LogLevel.Important);
                return actualRate > 0 ? actualRate : 0;
            }

            return actualRate;
        }

        private static double tryGetUsableDeviceRate()
        {
            try
            {
                double deviceRate = BassAsio.Rate;

                if (deviceRate > 0)
                    return deviceRate;
            }
            catch (Exception ex)
            {
                Logger.Log($"Could not read ASIO device sample rate: {ex}", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return 0;
        }

        private static bool configureOutputRouting(int mixerHandle)
        {
            resetOutputRouting();

            if (!BassAsio.GetInfo(out AsioInfo info))
            {
                Logger.Log("Unable to get ASIO info while configuring output routing.", LoggingTarget.Runtime, LogLevel.Error);
                return false;
            }

            if (info.Outputs < 2)
            {
                Logger.Log($"ASIO device only exposes {info.Outputs} output channel(s); stereo output requires at least 2.", LoggingTarget.Runtime, LogLevel.Error);
                return false;
            }

            try
            {
                if (tryConfigureOutputRoutingWithBassChannel(info, mixerHandle))
                    return true;
            }
            catch (EntryPointNotFoundException)
            {
                Logger.Log("ASIO native library does not export BASS_ASIO_ChannelEnableBASS; using callback routing fallback.", LoggingTarget.Runtime, LogLevel.Debug);
                resetOutputRouting();
            }

            return tryConfigureOutputRoutingWithCallback(info, mixerHandle);
        }

        private static bool tryConfigureOutputRoutingWithBassChannel(AsioInfo info, int mixerHandle)
        {
            var sampleFormat = GetSampleFormatForBitDepth(TargetBitDepth);

            for (int channel = 0; channel <= info.Outputs - 2; channel++)
            {
                BassAsio.ChannelSetFormat(false, channel, sampleFormat);
                BassAsio.ChannelSetFormat(false, channel + 1, sampleFormat);

                if (BassAsio.ChannelEnableBass(false, channel, mixerHandle, true))
                {
                    Logger.Log($"ASIO output routing configured via BASS channel on channels {channel}/{channel + 1}", LoggingTarget.Runtime, LogLevel.Debug);
                    return true;
                }

                Logger.Log($"Failed to bind mixer {mixerHandle} to ASIO output channel {channel}: {BassAsio.LastError}", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return false;
        }

        private static bool tryConfigureOutputRoutingWithCallback(AsioInfo info, int mixerHandle)
        {
            for (int channel = 0; channel <= info.Outputs - 2; channel++)
            {
                if (!BassAsio.ChannelEnable(false, channel, asio_callback, IntPtr.Zero))
                {
                    Logger.Log($"Failed to enable ASIO callback output channel {channel}: {BassAsio.LastError}", LoggingTarget.Runtime, LogLevel.Debug);
                    continue;
                }

                // Callback path reads float samples from the BASS mixer; keep float ASIO format here.
                if (!BassAsio.ChannelSetFormat(false, channel, AsioSampleFormat.Float))
                    Logger.Log($"Failed to set ASIO callback channel {channel} format to Float: {BassAsio.LastError}", LoggingTarget.Runtime, LogLevel.Debug);

                if (!BassAsio.ChannelSetRate(false, channel, 0))
                    Logger.Log($"Failed to set ASIO callback channel {channel} rate to device rate: {BassAsio.LastError}", LoggingTarget.Runtime, LogLevel.Debug);

                int joinedChannel = channel + 1;

                if (!BassAsio.ChannelJoin(false, joinedChannel, channel))
                {
                    Logger.Log($"Failed to join ASIO channel {joinedChannel} to {channel}: {BassAsio.LastError}", LoggingTarget.Runtime, LogLevel.Debug);
                    BassAsio.ChannelReset(false, channel, AsioChannelResetFlags.Enable | AsioChannelResetFlags.Join | AsioChannelResetFlags.Joined);
                    continue;
                }

                globalMixerHandle = mixerHandle;
                Logger.Log($"ASIO output routing configured via callback on channels {channel}/{joinedChannel}", LoggingTarget.Runtime, LogLevel.Debug);
                return true;
            }

            Logger.Log("Failed to configure ASIO stereo routing on any output channel pair.", LoggingTarget.Runtime, LogLevel.Error);
            return false;
        }

        private static void resetOutputRouting()
        {
            if (!BassAsio.GetInfo(out AsioInfo info))
                return;

            const AsioChannelResetFlags reset_flags = AsioChannelResetFlags.Enable | AsioChannelResetFlags.Join | AsioChannelResetFlags.Pause | AsioChannelResetFlags.Format
                                                      | AsioChannelResetFlags.Rate |
                                                      AsioChannelResetFlags.Volume | AsioChannelResetFlags.Joined;

            for (int channel = 0; channel < info.Outputs; channel++)
                BassAsio.ChannelReset(false, channel, reset_flags);
        }

        private static int resolveBufferSize(int? requested)
        {
            if (!BassAsio.GetInfo(out AsioInfo info))
                return normaliseRequestedBufferSize(requested) ?? DEFAULT_BUFFER_SIZE;

            int desired = normaliseRequestedBufferSize(requested) ?? DEFAULT_BUFFER_SIZE;
            int min = info.MinBufferLength > 0 ? info.MinBufferLength : desired;
            int max = info.MaxBufferLength > 0 ? info.MaxBufferLength : Math.Max(min, desired);
            int preferred = info.PreferredBufferLength > 0 ? info.PreferredBufferLength : desired;

            if (min > max)
                (min, max) = (max, min);

            desired = clamp(desired, min, max);

            if (info.BufferLengthGranularity == 0)
                return clamp(preferred, min, max);

            if (info.BufferLengthGranularity < 0)
                return clampToPowerOfTwo(desired, min, max, preferred);

            int steps = (int)Math.Round((double)(desired - min) / info.BufferLengthGranularity);
            int resolved = min + steps * info.BufferLengthGranularity;
            return clamp(resolved, min, max);
        }

        private static int? normaliseRequestedBufferSize(int? requested)
        {
            if (!requested.HasValue)
                return null;

            return GetTargetBufferSize(requested.Value);
        }

        private static int clampToPowerOfTwo(int desired, int min, int max, int preferred)
        {
            int candidate = 1;

            while (candidate < min)
                candidate <<= 1;

            int best = clamp(preferred > 0 ? preferred : candidate, min, max);
            int bestDistance = Math.Abs(best - desired);

            while (candidate > 0 && candidate <= max)
            {
                if (candidate >= min)
                {
                    int distance = Math.Abs(candidate - desired);

                    if (distance < bestDistance)
                    {
                        best = candidate;
                        bestDistance = distance;
                    }
                }

                if (candidate > int.MaxValue / 2)
                    break;

                candidate <<= 1;
            }

            return best;
        }

        private static int clamp(int value, int min, int max) => Math.Min(Math.Max(value, min), max);

        private static void freeDeviceInternal(bool resetMixerHandle)
        {
            try
            {
                if (BassAsio.IsStarted)
                    BassAsio.Stop();
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception while stopping ASIO device: {ex}", LoggingTarget.Runtime, LogLevel.Debug);
            }

            try
            {
                resetOutputRouting();
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception while resetting ASIO output routing: {ex}", LoggingTarget.Runtime, LogLevel.Debug);
            }

            try
            {
                BassAsio.Free();
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception during ASIO device release: {ex}", LoggingTarget.Runtime, LogLevel.Error);
            }

            appliedBufferSize = null;
            ActiveDeviceIndex = null;

            if (resetMixerHandle)
                globalMixerHandle = 0;

            sleepWithBackoff(2);
        }

        private static string getAsioErrorDescription(int errorCode)
        {
            return errorCode switch
            {
                1 => "ASIO 驱动不存在或无效。",
                2 => "ASIO 驱动没有输入或输出通道。",
                3 => "ASIO 驱动繁忙、不可用或打开失败。",
                6 => "ASIO 驱动不支持请求的采样格式。",
                8 => "ASIO 驱动已经初始化，通常说明上一次释放不完整。",
                23 => "ASIO 设备不存在，可能已断开连接。",
                _ => $"未知 ASIO 错误（代码 {errorCode}）。"
            };
        }

        private static int asioProcedure(bool input, int channel, IntPtr buffer, int length, IntPtr user)
        {
            if (input)
                return 0;

            int mixerHandle = globalMixerHandle;

            if (mixerHandle == 0)
            {
                fillBufferWithSilence(buffer, length);
                return length;
            }

            try
            {
                int bytesRead = Bass.ChannelGetData(mixerHandle, buffer, length | (int)DataFlags.Float);

                if (bytesRead <= 0)
                {
                    fillBufferWithSilence(buffer, length);
                    return length;
                }

                if (bytesRead < length)
                    clearRemainingBuffer(buffer, bytesRead, length);

                return length;
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception in ASIO callback routing: {ex}", LoggingTarget.Runtime, LogLevel.Error);
                fillBufferWithSilence(buffer, length);
                return length;
            }
        }

        private static unsafe void fillBufferWithSilence(IntPtr buffer, int length)
        {
            float* bufferPtr = (float*)buffer;

            for (int i = 0; i < length / sizeof(float); i++)
                bufferPtr[i] = 0;
        }

        private static unsafe void clearRemainingBuffer(IntPtr buffer, int bytesRead, int totalLength)
        {
            float* start = (float*)buffer + bytesRead / sizeof(float);
            int remainingSamples = (totalLength - bytesRead) / sizeof(float);

            for (int i = 0; i < remainingSamples; i++)
                start[i] = 0;
        }
    }
}
