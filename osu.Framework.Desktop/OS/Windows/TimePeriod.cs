using System;
using OpenTK;
using System.Runtime.InteropServices;

namespace osu.Framework.Desktop.OS.Windows
{
    /// <summary>
    /// Set the windows multimedia timer to a specific accuracy.
    /// </summary>
    internal class TimePeriod : IDisposable
    {
        private static TIMECAPS timeCapabilities;
        private readonly int period;

        [DllImport(@"winmm.dll", ExactSpelling = true)]
        private static extern int timeGetDevCaps(ref TIMECAPS ptc, int cbtc);

        [DllImport(@"winmm.dll", ExactSpelling = true)]
        private static extern int timeBeginPeriod(int uPeriod);

        [DllImport(@"winmm.dll", ExactSpelling = true)]
        private static extern int timeEndPeriod(int uPeriod);

        internal static int MinimumPeriod => timeCapabilities.wPeriodMin;
        internal static int MaximumPeriod => timeCapabilities.wPeriodMax;

        bool success;

        internal TimePeriod(int period)
        {
            this.period = period;

            try
            {
                success = MaximumPeriod > 0 || 0 == timeGetDevCaps(ref timeCapabilities, Marshal.SizeOf(typeof(TIMECAPS)));
                success &= 0 == timeBeginPeriod(MathHelper.Clamp(period, MinimumPeriod, MaximumPeriod));
            }
            catch { }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                try
                {
                    timeEndPeriod(period);
                }
                catch { }

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
        private struct TIMECAPS
        {
            internal int wPeriodMin;
            internal int wPeriodMax;
        }
    }
}
