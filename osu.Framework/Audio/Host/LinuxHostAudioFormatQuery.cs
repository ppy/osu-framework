// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using osu.Framework.Audio.Asio;
using osu.Framework.Logging;

namespace osu.Framework.Audio.Host
{
    /// <summary>
    /// Read-only PulseAudio queries for sink-reported sample formats on Linux (PipeWire-compatible).
    /// </summary>
    internal static class LinuxHostAudioFormatQuery
    {
        private const int pa_sample_s16_le = 3;
        private const int pa_sample_s24_le = 9;
        private const int pa_sample_s24_32_le = 12;
        private const int pa_sample_s32_le = 4;
        private const int pa_sample_float32_le = 5;

        // offsetof(pa_sink_info, sample_spec) on common x86_64 libpulse builds.
        private const int sample_spec_offset = 96;

        public static IReadOnlyList<EzAsioFormatOption> GetReportedPlaybackFormats(string? bassDeviceName)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Linux)
                return Array.Empty<EzAsioFormatOption>();

            try
            {
                if (!tryQuerySinkFormat(bassDeviceName, out int rate, out int bits))
                    return Array.Empty<EzAsioFormatOption>();

                return new[] { new EzAsioFormatOption(rate, bits) };
            }
            catch (Exception ex)
            {
                Logger.Log($"Linux audio format lookup failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Debug);
                return Array.Empty<EzAsioFormatOption>();
            }
        }

        public static (int sampleRate, int bitDepth)? TryGetDefaultPlaybackFormat()
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Linux)
                return null;

            return tryQuerySinkFormat(null, out int rate, out int bits) ? (rate, bits) : null;
        }

        public static (int sampleRate, int channels)? TryGetMixFormat(string? bassDeviceName)
        {
            if (RuntimeInfo.OS != RuntimeInfo.Platform.Linux)
                return null;

            if (!tryQuerySinkFormat(bassDeviceName, out int rate, out _))
                return null;

            return (rate, 2);
        }

        private static bool tryQuerySinkFormat(string? bassDeviceName, out int sampleRate, out int bitDepth)
        {
            sampleRate = 0;
            bitDepth = 0;

            if (!PulseAudioNative.TryLoad())
                return false;

            var state = new PulseQueryState { TargetName = bassDeviceName };
            return PulseAudioNative.RunQuery(state, out sampleRate, out bitDepth);
        }

        private static int bitsFromPulseFormat(int format)
        {
            switch (format)
            {
                case pa_sample_s16_le:
                    return 16;

                case pa_sample_s24_le:
                case pa_sample_s24_32_le:
                case pa_sample_s32_le:
                case pa_sample_float32_le:
                    return 24;

                default:
                    return 0;
            }
        }

        private sealed class PulseQueryState
        {
            public string? TargetName { get; init; }
            public int SampleRate { get; set; }
            public int BitDepth { get; set; }
            public bool Found { get; set; }
        }

        private static class PulseAudioNative
        {
            private const string library = "libpulse.so.0";
            private static IntPtr libraryHandle;

            public static bool TryLoad() => libraryHandle != IntPtr.Zero || NativeLibrary.TryLoad(library, out libraryHandle);

            public static bool RunQuery(PulseQueryState state, out int sampleRate, out int bitDepth)
            {
                sampleRate = 0;
                bitDepth = 0;

                if (libraryHandle == IntPtr.Zero)
                    return false;

                IntPtr mainloop = pa_threaded_mainloop_new();
                IntPtr context = IntPtr.Zero;
                var stateHandle = GCHandle.Alloc(state);

                try
                {
                    pa_threaded_mainloop_start(mainloop);
                    context = pa_context_new(pa_threaded_mainloop_get_api(mainloop), "osu-framework-format-query");

                    pa_context_set_state_callback(context, context_state_callback, IntPtr.Zero);
                    pa_context_connect(context, null, 0, IntPtr.Zero);

                    if (!waitUntilReady(mainloop, context))
                        return false;

                    var cb = new SinkInfoCallback(sink_info_callback);
                    IntPtr cbPtr = Marshal.GetFunctionPointerForDelegate(cb);

                    if (string.IsNullOrWhiteSpace(state.TargetName))
                        pa_context_get_sink_info_by_name(context, "@DEFAULT_SINK@", cbPtr, GCHandle.ToIntPtr(stateHandle));
                    else
                        pa_context_get_sink_info_list(context, cbPtr, GCHandle.ToIntPtr(stateHandle));

                    waitForCompletion(mainloop);
                    GC.KeepAlive(cb);

                    if (!state.Found)
                    {
                        // Named lookup failed — fall back to the default sink format.
                        state.Found = false;
                        pa_context_get_sink_info_by_name(context, "@DEFAULT_SINK@", cbPtr, GCHandle.ToIntPtr(stateHandle));
                        waitForCompletion(mainloop);
                    }

                    sampleRate = state.SampleRate;
                    bitDepth = state.BitDepth;
                    return state.Found && bitDepth > 0;
                }
                finally
                {
                    stateHandle.Free();

                    if (context != IntPtr.Zero)
                    {
                        pa_context_disconnect(context);
                        pa_context_unref(context);
                    }

                    pa_threaded_mainloop_stop(mainloop);
                    pa_threaded_mainloop_free(mainloop);
                }
            }

            private static bool waitUntilReady(IntPtr mainloop, IntPtr context)
            {
                for (int i = 0; i < 200; i++)
                {
                    var ctxState = pa_context_get_state(context);

                    if (ctxState == ContextState.Ready)
                        return true;

                    if (ctxState == ContextState.Failed || ctxState == ContextState.Terminated)
                        return false;

                    pa_threaded_mainloop_wait(mainloop);
                }

                return false;
            }

            private static void waitForCompletion(IntPtr mainloop)
            {
                for (int i = 0; i < 200; i++)
                    pa_threaded_mainloop_wait(mainloop);
            }

            private static void context_state_callback(IntPtr context, IntPtr userdata)
            {
                IntPtr mainloop = pa_context_get_threaded_mainloop(context);
                pa_threaded_mainloop_signal(mainloop, 0);
            }

            private static void sink_info_callback(IntPtr context, IntPtr info, int eol, IntPtr userdata)
            {
                var state = (PulseQueryState)GCHandle.FromIntPtr(userdata).Target!;
                IntPtr mainloop = pa_context_get_threaded_mainloop(context);

                if (eol != 0)
                {
                    pa_threaded_mainloop_signal(mainloop, 0);
                    return;
                }

                if (!state.Found)
                {
                    string? sinkName = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(info));

                    bool matches = string.IsNullOrWhiteSpace(state.TargetName)
                                   || (sinkName != null && state.TargetName != null
                                                        && (sinkName.Contains(state.TargetName, StringComparison.OrdinalIgnoreCase)
                                                            || state.TargetName.Contains(sinkName, StringComparison.OrdinalIgnoreCase)));

                    if (matches && tryReadSampleSpec(info, out int rate, out int bits))
                    {
                        state.SampleRate = rate;
                        state.BitDepth = bits;
                        state.Found = bits > 0;
                    }
                }

                pa_threaded_mainloop_signal(mainloop, 0);
            }

            private static bool tryReadSampleSpec(IntPtr info, out int sampleRate, out int bitDepth)
            {
                sampleRate = 0;
                bitDepth = 0;

                try
                {
                    int format = Marshal.ReadInt32(info, sample_spec_offset);
                    sampleRate = Marshal.ReadInt32(info, sample_spec_offset + 4);
                    bitDepth = bitsFromPulseFormat(format);
                    return sampleRate > 0 && bitDepth > 0;
                }
                catch
                {
                    return false;
                }
            }

            private enum ContextState
            {
                Unconnected = 0,
                Connecting = 1,
                Authorizing = 2,
                SettingName = 3,
                Ready = 4,
                Terminated = 5,
                Failed = 6,
            }

            private delegate void SinkInfoCallback(IntPtr context, IntPtr info, int eol, IntPtr userdata);

            [DllImport(library)]
            private static extern IntPtr pa_threaded_mainloop_new();

            [DllImport(library)]
            private static extern void pa_threaded_mainloop_free(IntPtr m);

            [DllImport(library)]
            private static extern int pa_threaded_mainloop_start(IntPtr m);

            [DllImport(library)]
            private static extern void pa_threaded_mainloop_stop(IntPtr m);

            [DllImport(library)]
            private static extern void pa_threaded_mainloop_wait(IntPtr m);

            [DllImport(library)]
            private static extern void pa_threaded_mainloop_signal(IntPtr m, int waitForAccept);

            [DllImport(library)]
            private static extern IntPtr pa_threaded_mainloop_get_api(IntPtr m);

            [DllImport(library)]
            private static extern IntPtr pa_context_new(IntPtr mainloopAPI, [MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(library)]
            private static extern void pa_context_unref(IntPtr context);

            [DllImport(library)]
            private static extern int pa_context_connect(IntPtr context, [MarshalAs(UnmanagedType.LPStr)] string? server, uint flags, IntPtr api);

            [DllImport(library)]
            private static extern void pa_context_disconnect(IntPtr context);

            [DllImport(library)]
            private static extern ContextState pa_context_get_state(IntPtr context);

            [DllImport(library)]
            private static extern void pa_context_set_state_callback(IntPtr context, ContextStateCallback cb, IntPtr userdata);

            [DllImport(library)]
            private static extern IntPtr pa_context_get_threaded_mainloop(IntPtr context);

            [DllImport(library)]
            private static extern void pa_context_get_sink_info_list(IntPtr context, IntPtr cb, IntPtr userdata);

            [DllImport(library)]
            private static extern void pa_context_get_sink_info_by_name(IntPtr context, [MarshalAs(UnmanagedType.LPStr)] string name, IntPtr cb, IntPtr userdata);

            private delegate void ContextStateCallback(IntPtr context, IntPtr userdata);
        }
    }
}
