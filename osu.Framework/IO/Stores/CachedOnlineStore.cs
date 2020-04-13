﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Threading.Tasks;
using MonkeyCache;

namespace osu.Framework.IO.Stores
{
    /// <summary>
    /// An <see cref="OnlineStore"/> with file system caching
    /// </summary>
    public class CachedOnlineStore : OnlineStore
    {
        /// <summary>
        /// Fired on getting item from cache. This event is required for testing.
        /// </summary>
        public event Action<string> CacheItemReturned;

        private readonly IBarrel cache;
        public readonly TimeSpan Duration;

        /// <summary>
        /// Constructs a <see cref="CachedOnlineStore"/>
        /// </summary>
        /// <param name="cache"><see cref="IBarrel"/> implementation used for caching</param>
        /// <param name="duration">The amount of time since last access after which a file will be considered expired.</param>
        public CachedOnlineStore(IBarrel cache, TimeSpan duration)
        {
            this.cache = cache;
            Duration = duration;
        }

        /// <summary>
        /// Retrieves an object from cache asynchronously if it is possible. Otherwise calls an <see cref="OnlineStore"/> implementation and caches its result."/>
        /// </summary>
        /// <param name="url">The address of the object.</param>
        /// <returns>The object.</returns>
        public override async Task<byte[]> GetAsync(string url)
        {
            if (!cache.IsExpired(url))
            {
                CacheItemReturned?.Invoke(url);
                return cache.Get<byte[]>(url);
            }

            var data = await base.GetAsync(url);
            cache.Add(url, data, Duration);

            return data;
        }

        /// <summary>
        /// Retrieves an object from cache if it is possible. Otherwise calls an <see cref="OnlineStore"/> implementation and caches its result."/>
        /// </summary>
        /// <param name="url">The address of the object.</param>
        /// <returns>The object.</returns>
        public override byte[] Get(string url)
        {
            if (!cache.IsExpired(url))
            {
                CacheItemReturned?.Invoke(url);
                return cache.Get<byte[]>(url);
            }

            var data = base.Get(url);
            cache.Add(url, data, Duration);

            return data;
        }

        public override Stream GetStream(string url)
        {
            if (!cache.IsExpired(url))
            {
                CacheItemReturned?.Invoke(url);
                return new MemoryStream(cache.Get<byte[]>(url));
            }

            var stream = base.GetStream(url);

            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            cache.Add(url, bytes, Duration);

            return stream;
        }

        /// <summary>
        /// Clears cache
        /// </summary>
        /// <returns>The number of removed objects</returns>
        public int Clear()
        {
            var removed = 0;
            var expiredEntries = cache.GetKeys(CacheState.Expired);

            foreach (var expiredEntry in expiredEntries)
            {
                cache.Empty(expiredEntry);
                removed++;
            }

            return removed;
        }
    }
}
