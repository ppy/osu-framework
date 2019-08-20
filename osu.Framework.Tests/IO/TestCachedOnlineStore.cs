// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using MonkeyCache;
using MonkeyCache.FileStore;
using NUnit.Framework;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestCachedOnlineStore
    {
        private const string ru_flag_url = "https://osu.ppy.sh/images/flags/RU.png";
        private const string jp_flag_url = "https://osu.ppy.sh/images/flags/JP.png";

        private CachedOnlineStore onlineStore;
        private IBarrel onlineStoreCache;

        [SetUp]
        public void SetUp()
        {
            onlineStore = new CachedOnlineStore(onlineStoreCache = Barrel.Create(new TemporaryNativeStorage(Guid.NewGuid().ToString()).GetFullPath(string.Empty)), TimeSpan.FromHours(1));
        }

        [TearDown]
        public void TearDown()
        {
            onlineStore.Dispose();
        }

        [Test]
        public void TestGet()
        {
            Assert.That(onlineStoreCache.Exists(ru_flag_url), Is.False);
            onlineStore.Get(ru_flag_url);
            Assert.That(onlineStoreCache.Exists(ru_flag_url), Is.True);
        }

        [Test]
        public void TestGetFromCache()
        {
            var cacheItemReturned = false;
            onlineStore.CacheItemReturned += _ => { cacheItemReturned = true; };

            onlineStore.Get(ru_flag_url);
            Assert.That(cacheItemReturned, Is.False);

            onlineStore.Get(ru_flag_url);
            Assert.That(cacheItemReturned, Is.True);
        }

        [Test]
        public async Task TestGetAsync()
        {
            Assert.That(onlineStoreCache.Exists(ru_flag_url), Is.False);
            await onlineStore.GetAsync(ru_flag_url);
            Assert.That(onlineStoreCache.Exists(ru_flag_url), Is.True);
        }

        [Test]
        public async Task TestGetFromCacheAsync()
        {
            var cacheItemReturned = false;
            onlineStore.CacheItemReturned += _ => { cacheItemReturned = true; };

            await onlineStore.GetAsync(ru_flag_url);
            Assert.That(cacheItemReturned, Is.False);

            await onlineStore.GetAsync(ru_flag_url);
            Assert.That(cacheItemReturned, Is.True);
        }

        [Test]
        public void TestGetStream()
        {
            Assert.That(onlineStoreCache.Exists(ru_flag_url), Is.False);
            var stream = onlineStore.GetStream(ru_flag_url);
            Assert.That(stream, Is.Not.Null);
            Assert.That(onlineStoreCache.Exists(ru_flag_url), Is.True);
        }

        [Test]
        public void TestGetStreamFromCache()
        {
            var cacheItemReturned = false;
            onlineStore.CacheItemReturned += _ => { cacheItemReturned = true; };

            onlineStore.GetStream(ru_flag_url);
            Assert.That(cacheItemReturned, Is.False);

            onlineStore.GetStream(ru_flag_url);
            Assert.That(cacheItemReturned, Is.True);
        }

        [Test]
        public void TestClearExpired()
        {
            throw new NotImplementedException();
        }
    }
}
