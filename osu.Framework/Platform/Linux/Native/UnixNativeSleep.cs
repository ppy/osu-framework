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
            public nint Seconds;
            public nint NanoSeconds;
        }

        [DllImport("libc", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        private static extern int nanosleep(in TimeSpec duration, out TimeSpec rem);

        private const int interrupt_error = 4;

        public static bool Available { get; private set; }

        // Just a safe check before actually using it.
        // .NET tries possible library names if 'libc' is given, but it may fail to find it.
        private static bool testNanoSleep()
        {
            TimeSpec test = new TimeSpec
            {
                Seconds = 0,
                NanoSeconds = 1,
            };

            try
            {
                nanosleep(in test, out _);
                return true;
            }
            catch
            {
                return false;
            }
        }

        static UnixNativeSleep()
        {
            Available = testNanoSleep();
        }

        public bool Sleep(TimeSpan duration)
        {
            const int ns_per_second = 1000 * 1000 * 1000;

            long ns = (long)duration.TotalNanoseconds;

            TimeSpec timeSpec = new TimeSpec
            {
                Seconds = (nint)(ns / ns_per_second),
                NanoSeconds = (nint)(ns % ns_per_second),
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
