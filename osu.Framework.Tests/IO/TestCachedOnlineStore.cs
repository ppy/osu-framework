// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestCachedOnlineStore
    {
        private const string ru_flag_url = "https://osu.ppy.sh/images/flags/RU.png";
        private const string jp_flag_url = "https://osu.ppy.sh/images/flags/JP.png";

        private CachedOnlineStore sut;
        private TemporaryNativeStorage sutCache;

        [SetUp]
        public void SetUp()
        {
            sut = new CachedOnlineStore(sutCache = new TemporaryNativeStorage(Guid.NewGuid().ToString()), TimeSpan.FromHours(1));
        }

        [TearDown]
        public void TearDown()
        {
            sutCache.Dispose();
        }

        [Test]
        public void TestGet()
        {
            Assert.That(sutCache.Exists(ru_flag_url.ComputeMD5Hash()), Is.False);
            sut.Get(ru_flag_url);
            Assert.That(sutCache.Exists(ru_flag_url.ComputeMD5Hash()), Is.True);
        }

        [Test]
        public void TestGetFromCache()
        {
            sut.Get(ru_flag_url);
            var accessTime = sutCache.GetLastAccessTime(ru_flag_url.ComputeMD5Hash());

            sut.Get(ru_flag_url);
            Assert.That(sutCache.GetLastAccessTime(ru_flag_url.ComputeMD5Hash()), Is.GreaterThan(accessTime));
        }

        [Test]
        public async Task TestGetAsync()
        {
            Assert.That(sutCache.Exists(ru_flag_url.ComputeMD5Hash()), Is.False);
            await sut.GetAsync(ru_flag_url);
            Assert.That(sutCache.Exists(ru_flag_url.ComputeMD5Hash()), Is.True);
        }

        [Test]
        public async Task TestGetFromCacheAsync()
        {
            await sut.GetAsync(ru_flag_url);
            var accessTime = sutCache.GetLastAccessTime(ru_flag_url.ComputeMD5Hash());

            await sut.GetAsync(ru_flag_url);
            Assert.That(sutCache.GetLastAccessTime(ru_flag_url.ComputeMD5Hash()), Is.GreaterThan(accessTime));
        }

        [Test]
        public void TestGetStream()
        {
            Assert.That(sutCache.Exists(ru_flag_url.ComputeMD5Hash()), Is.False);
            var stream = sut.GetStream(ru_flag_url);

            Assert.That(stream, Is.Not.Null);
            Assert.That(sutCache.Exists(ru_flag_url.ComputeMD5Hash()), Is.True);
        }

        [Test]
        public void TestClearExpired()
        {
            sut.Get(ru_flag_url);
            sut.Get(jp_flag_url);

            Assert.That(sutCache.Exists(ru_flag_url.ComputeMD5Hash()), Is.True);
            Assert.That(sutCache.Exists(jp_flag_url.ComputeMD5Hash()), Is.True);

            Assert.That(sut.Clear(), Is.EqualTo(0));

            sutCache.SetLastAccessTime(jp_flag_url.ComputeMD5Hash(), DateTime.Now.AddDays(-1));

            Assert.That(sut.Clear(), Is.EqualTo(1));
            Assert.That(sutCache.Exists(ru_flag_url.ComputeMD5Hash()), Is.True);
            Assert.That(sutCache.Exists(jp_flag_url.ComputeMD5Hash()), Is.False);
        }
    }
}
