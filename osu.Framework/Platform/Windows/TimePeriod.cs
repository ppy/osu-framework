// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Runtime.InteropServices;
using OpenTK;

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

        private bool canAdjust = MaximumPeriod > 0;

        static TimePeriod()
        {
            timeGetDevCaps(ref time_capabilities, Marshal.SizeOf(typeof(TimeCaps)));
        }

        internal TimePeriod(int period)
        {
            this.period = period;
        }

        private bool active;

        internal bool Active
        {
            get { return active; }
            set
            {
                if (value == active || !canAdjust) return;
                active = value;

                try
                {
                    if (active)
                    {
                        canAdjust &= 0 == timeBeginPeriod(MathHelper.Clamp(period, MinimumPeriod, MaximumPeriod));
                    }
                    else
                    {
                        timeEndPeriod(period);
                    }
                }
                catch
                {
                }
            }
        }

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Active = false;
                disposedValue = true;
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

        [StructLayout(LayoutKind.Sequential)]
        private struct TimeCaps
        {
            internal readonly int wPeriodMin;
            internal readonly int wPeriodMax;
        }
    }
}
