// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace osu.Framework.Platform.Windows.Native
{
    internal static class Execution
    {
        [DllImport("kernel32.dll")]
        internal static extern uint SetThreadExecutionState(ExecutionState state);

        [Flags]
        internal enum ExecutionState : uint
        {
            AwaymodeRequired = 0x00000040,
            Continuous = 0x80000000,
            DisplayRequired = 0x00000002,
            SystemRequired = 0x00000001,
            UserPresent = 0x00000004,
        }

        [DllImport("kernel32.dll")]
        internal static extern bool SetWaitableTimerEx(IntPtr hTimer, in FILETIME lpDueTime, int lPeriod, TimerApcProc? routine, IntPtr lpArgToCompletionRoutine, IntPtr reason, uint tolerableDelay);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr CreateWaitableTimerEx(IntPtr lpTimerAttributes, string? lpTimerName, CreateWaitableTimerFlags dwFlags, uint dwDesiredAccess);

        internal const uint TIMER_ALL_ACCESS = 2031619U;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        public delegate void TimerApcProc([In] IntPtr lpArgToCompletionRoutine, uint dwTimerLowValue, uint dwTimerHighValue);

        [Flags]
        internal enum CreateWaitableTimerFlags : uint
        {
            CREATE_WAITABLE_TIMER_MANUAL_RESET = 0x00000001,
            CREATE_WAITABLE_TIMER_HIGH_RESOLUTION = 0x00000002,
        }

        public const uint INFINITE = 0xffffffff;

        [DllImport("kernel32.dll")]
        internal static extern bool WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        internal static FILETIME CreateFileTime(TimeSpan ts)
        {
            ulong ul = unchecked((ulong)-ts.Ticks);
            return new FILETIME { dwHighDateTime = (int)(ul >> 32), dwLowDateTime = (int)(ul & 0xFFFFFFFF) };
        }

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}
