// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.IO.Stores;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    [Category("httpbin")]
    public class TestOnlineStore
    {
        private const string default_protocol = "http";

        private static readonly string host;
        private static readonly IEnumerable<string> protocols;

        private bool oldAllowInsecureRequests;
        private OnlineStore store = null!;

        static TestOnlineStore()
        {
            bool localHttpBin = Environment.GetEnvironmentVariable("OSU_TESTS_LOCAL_HTTPBIN") == "1";

            if (localHttpBin)
            {
                // httpbin very frequently falls over and causes random tests to fail
                // Thus github actions builds rely on a local httpbin instance to run the tests

                host = "127.0.0.1:8080";
                protocols = new[] { default_protocol };
            }
            else
            {
                host = "httpbin.org";
                protocols = new[] { default_protocol, "https" };
            }
        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            oldAllowInsecureRequests = FrameworkEnvironment.AllowInsecureRequests;
            FrameworkEnvironment.AllowInsecureRequests = true;
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            FrameworkEnvironment.AllowInsecureRequests = oldAllowInsecureRequests;
        }

        [SetUp]
        public void Setup()
        {
            store = new OnlineStore();
        }

        [Test, Retry(5)]
        public void TestValidUrlReturnsData([ValueSource(nameof(protocols))] string protocol, [Values(true, false)] bool async)
        {
            byte[]? result = async
                ? store.GetAsync($"{protocol}://{host}/image/png").GetResultSafely()
                : store.Get($"{protocol}://{host}/image/png");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Has.Length.GreaterThan(0));
        }

        [Test]
        public void TestMissingSchemeReturnsNull([Values(true, false)] bool async)
        {
            byte[]? result = async
                ? store.GetAsync($"{host}/image/png").GetResultSafely()
                : store.Get($"{host}/image/png");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestInvalidUrlReturnsNull()
        {
            byte[]? result = store.Get("this is not a valid url");
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestNullUrlReturnsNull()
        {
            // Not sure if this store should accept a null URL, but let's test it anyway.
            byte[]? result = store.Get(null);
            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestFileUrlFails([Values(true, false)] bool async)
        {
            // Known, guaranteed file path.
            string path = new Uri(AppContext.BaseDirectory).AbsoluteUri;

            byte[]? result = async
                ? store.GetAsync(path).GetResultSafely()
                : store.Get(path);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TestBadWebRequest([ValueSource(nameof(protocols))] string protocol, [Values(true, false)] bool async)
        {
            byte[]? result = async
                ? store.GetAsync($"{protocol}://{host}/status/500").GetResultSafely()
                : store.Get($"{protocol}://{host}/status/500");

            Assert.That(result, Is.Null);
        }
    }
}
