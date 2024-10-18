// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Linux.Native
{
    internal class UnixNativeSleep : INativeSleep
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct TimeSpec
        {
            public long Seconds;
            public long NanoSeconds;
        }

        private delegate int NanoSleepDelegate(in TimeSpec duration, out TimeSpec rem);

        private static readonly NanoSleepDelegate? nanosleep;

        // Android and some platforms don't have version in lib name.
        [DllImport("c", EntryPoint = "nanosleep", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int nanosleep_c(in TimeSpec duration, out TimeSpec rem);

        [DllImport("libc.so.6", EntryPoint = "nanosleep", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int nanosleep_libc6(in TimeSpec duration, out TimeSpec rem);

        private const int interrupt_error = 4;

        static UnixNativeSleep()
        {
            TimeSpec test = new TimeSpec
            {
                Seconds = 0,
                NanoSeconds = 1,
            };

            try
            {
                nanosleep_c(in test, out _);
                nanosleep = nanosleep_c;
            }
            catch
            {
            }

            if (nanosleep == null)
            {
                try
                {
                    nanosleep_libc6(in test, out _);
                    nanosleep = nanosleep_libc6;
                }
                catch
                {
                }
            }

            // if nanosleep is null at this point, Thread.Sleep should be used.
        }

        public bool Sleep(TimeSpan duration)
        {
            if (nanosleep == null)
                return false;

            const int ns_per_second = 1000 * 1000 * 1000;

            long ns = (long)duration.TotalNanoseconds;

            TimeSpec timeSpec = new TimeSpec
            {
                Seconds = ns / ns_per_second,
                NanoSeconds = ns % ns_per_second,
            };

            int ret;

            while ((ret = nanosleep(in timeSpec, out var remaining)) == -1 && Marshal.GetLastPInvokeError() == interrupt_error)
            {
                // The pause can be interrupted by a signal that was delivered to the thread.
                // Sleep again with remaining time if it happened.
                timeSpec = remaining;
            }

            return ret == 0; // Any errors other than interrupt_error should return false.
        }

        public void Dispose()
        {
        }
    }
}
