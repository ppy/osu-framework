// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace osu.Framework.Allocation
{
    /// <summary>
    /// A key-value store which supports cleaning up items after a specified expiry time.
    /// </summary>
    public class TimedExpiryCache<TKey, TValue> : IDisposable
    {
        private ConcurrentDictionary<TKey, TimedObject<TValue>> dictionary = new ConcurrentDictionary<TKey, TimedObject<TValue>>();

        /// <summary>
        /// Time in milliseconds after last access after which items will be cleaned up.
        /// </summary>
        public int ExpiryTime = 5000;

        /// <summary>
        /// The interval in milliseconds between checks for expiry.
        /// </summary>
        public int CheckPeriod = 5000;

        public TimedExpiryCache()
        {
            checkExpiryAsync();
        }

        internal void Add(TKey key, TValue value)
        {
            dictionary.TryAdd(key, new TimedObject<TValue>(value));
        }

        private async Task checkExpiryAsync()
        {
            var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

            foreach (var v in dictionary)
            {
                TimedObject<TValue> val;
                if (now - v.Value.LastAccessTime > ExpiryTime)
                    dictionary.TryRemove(v.Key, out val);
            }

            await Task.Delay(CheckPeriod);

            if (isDisposed)
                return;

            Task.Run(checkExpiryAsync);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            TimedObject<TValue> timed;

            if (!dictionary.TryGetValue(key, out timed))
            {
                value = default(TValue);
                return false;
            }

            value = timed.Value;
            return true;
        }

        #region IDisposable Support
        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
                isDisposed = true;
        }

        ~TimedExpiryCache()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        class TimedObject<T>
        {
            public long LastAccessTime;

            private readonly T value;

            public T Value
            {
                get
                {

                    updateAccessTime();
                    return value;
                }
            }

            public TimedObject(T value)
            {
                this.value = value;
                updateAccessTime();
            }

            private void updateAccessTime()
            {
                LastAccessTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            }
        }
    }
}