// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Windows
{
    /// <summary>
    /// Set the windows multimedia timer to a specific accuracy.
    /// </summary>
    internal class TimePeriod : IDisposable
    {
        private static readonly TimeCaps time_capabilities;

        private readonly int period;

        [DllImport(@"winmm.dll", ExactSpelling = true)]
        private static extern int timeGetDevCaps(ref TimeCaps ptc, int cbtc);

        [DllImport(@"winmm.dll", ExactSpelling = true)]
        private static extern int timeBeginPeriod(int uPeriod);

        [DllImport(@"winmm.dll", ExactSpelling = true)]
        private static extern int timeEndPeriod(int uPeriod);

        internal static int MinimumPeriod => time_capabilities.wPeriodMin;
        internal static int MaximumPeriod => time_capabilities.wPeriodMax;

        private readonly bool didAdjust;

        static TimePeriod()
        {
            timeGetDevCaps(ref time_capabilities, Marshal.SizeOf(typeof(TimeCaps)));
        }

        internal TimePeriod(int period)
        {
            this.period = period;

            if (MaximumPeriod <= 0)
                return;

            try
            {
                didAdjust = timeBeginPeriod(Math.Clamp(period, MinimumPeriod, MaximumPeriod)) == 0;
            }
            catch { }
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;

                if (!didAdjust)
                    return;

                try
                {
                    timeEndPeriod(period);
                }
                catch { }
            }
        }

        ~TimePeriod()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

#pragma warning disable IDE1006 // Naming style

        [StructLayout(LayoutKind.Sequential)]
        private readonly struct TimeCaps
        {
            internal readonly int wPeriodMin;
            internal readonly int wPeriodMax;
        }
    }
}
