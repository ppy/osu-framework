// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.Windows.Native
{
    internal class WindowsNativeSleep : INativeSleep
    {
        private IntPtr waitableTimer;

        public WindowsNativeSleep()
        {
            createWaitableTimer();
        }

        private void createWaitableTimer()
        {
            try
            {
                // Attempt to use CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, only available since Windows 10, version 1803.
                waitableTimer = Execution.CreateWaitableTimerEx(IntPtr.Zero, null,
                    Execution.CreateWaitableTimerFlags.CREATE_WAITABLE_TIMER_MANUAL_RESET | Execution.CreateWaitableTimerFlags.CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, Execution.TIMER_ALL_ACCESS);

                if (waitableTimer == IntPtr.Zero)
                {
                    // Fall back to a more supported version. This is still far more accurate than Thread.Sleep.
                    waitableTimer = Execution.CreateWaitableTimerEx(IntPtr.Zero, null, Execution.CreateWaitableTimerFlags.CREATE_WAITABLE_TIMER_MANUAL_RESET, Execution.TIMER_ALL_ACCESS);
                }
            }
            catch
            {
                // Any kind of unexpected exception should fall back to Thread.Sleep.
            }
        }

        public bool Sleep(TimeSpan duration)
        {
            if (waitableTimer == IntPtr.Zero) return false;

            // Not sure if we want to fall back to Thread.Sleep on failure here, needs further investigation.
            if (Execution.SetWaitableTimerEx(waitableTimer, Execution.CreateFileTime(duration), 0, null, default, IntPtr.Zero, 0))
            {
                Execution.WaitForSingleObject(waitableTimer, Execution.INFINITE);
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            if (waitableTimer != IntPtr.Zero)
            {
                Execution.CloseHandle(waitableTimer);
                waitableTimer = IntPtr.Zero;
            }
        }
    }
}
