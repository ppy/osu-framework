// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.IO.Network;
using HttpMethod = osu.Framework.IO.Network.HttpMethod;
using WebRequest = osu.Framework.IO.Network.WebRequest;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestWebRequest
    {
        private const string valid_get_url = "httpbin.org/get";
        private const string invalid_get_url = "a.ppy.shhhhh";

        [Test, Retry(5)]
        public void TestValidGet([Values("http", "https")] string protocol, [Values(true, false)] bool async)
        {
            var url = $"{protocol}://httpbin.org/get";
            var request = new JsonWebRequest<HttpBinGetResponse>(url) { Method = HttpMethod.GET };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(responseObject != null);
            Assert.IsTrue(responseObject.Headers.UserAgent == "osu!");
            Assert.IsTrue(responseObject.Url == url);

            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests async execution is correctly yielding during IO wait time.
        /// </summary>
        [Test]
        [Ignore("failing too often on appveyor")]
        public void TestConcurrency()
        {
            const int request_count = 10;
            const int induced_delay = 5;

            int finished = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<long> startTimes = new List<long>();

            List<Task> running = new List<Task>();
            for (int i = 0; i < request_count; i++)
            {
                var request = new DelayedWebRequest
                {
                    Method = HttpMethod.GET,
                    Delay = induced_delay
                };

                request.Started += () => startTimes.Add(sw.ElapsedMilliseconds);
                request.Finished += () => Interlocked.Increment(ref finished);
                request.Failed += _ => Interlocked.Increment(ref finished);
                running.Add(request.PerformAsync());
            }

            Task.WaitAll(running.ToArray());

            // in the case threads are not yielding, the time taken will be greater than double the induced delay (after considering latency).
            Assert.Less(sw.ElapsedMilliseconds, induced_delay * 2 * 1000);

            Assert.AreEqual(request_count, startTimes.Count);

            // another case would be requests starting too late into the test. just to make sure.
            for (int i = 0; i < request_count; i++)
                Assert.Less(startTimes[i] - startTimes[0], induced_delay * 1000);

            Assert.AreEqual(request_count, finished);
        }

        [Test, Retry(5)]
        public void TestInvalidGetExceptions([Values("http", "https")] string protocol, [Values(true, false)] bool async)
        {
            var request = new WebRequest($"{protocol}://{invalid_get_url}") { Method = HttpMethod.GET };

            Exception finishedException = null;
            request.Failed += exception => finishedException = exception;

            if (async)
                Assert.ThrowsAsync<HttpRequestException>(request.PerformAsync);
            else
                Assert.Throws<HttpRequestException>(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsTrue(request.ResponseString == null);
            Assert.IsNotNull(finishedException);
        }

        [Test, Retry(5)]
        public void TestBadStatusCode([Values(true, false)] bool async)
        {
            var request = new WebRequest("https://httpbin.org/hidden-basic-auth/user/passwd");

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            if (async)
                Assert.ThrowsAsync<WebException>(request.PerformAsync);
            else
                Assert.Throws<WebException>(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsEmpty(request.ResponseString);

            Assert.IsTrue(hasThrown);
        }

        /// <summary>
        /// Tests aborting the <see cref="WebRequest"/> after response has been received from the server
        /// but before data has been read.
        /// </summary>
        [Test, Retry(5)]
        public void TestAbortReceive([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinGetResponse>("https://httpbin.org/get") { Method = HttpMethod.GET };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;
            request.Started += () => request.Abort();

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsTrue(request.ResponseObject == null);

            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests aborting the <see cref="WebRequest"/> before the request is sent to the server.
        /// </summary>
        [Test, Retry(5)]
        public void TestAbortRequest()
        {
            var request = new JsonWebRequest<HttpBinGetResponse>("https://httpbin.org/get") { Method = HttpMethod.GET };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

#pragma warning disable 4014
            request.PerformAsync();
#pragma warning restore 4014

            Assert.DoesNotThrow(request.Abort);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsTrue(request.ResponseObject == null);

            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests being able to abort + restart a request.
        /// </summary>
        [Test, Retry(5)]
        public void TestRestartAfterAbort([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinGetResponse>("https://httpbin.org/get") { Method = HttpMethod.GET };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

#pragma warning disable 4014
            request.PerformAsync();
#pragma warning restore 4014

            Assert.DoesNotThrow(request.Abort);

            if (async)
                Assert.ThrowsAsync<InvalidOperationException>(request.PerformAsync);
            else
                Assert.Throws<InvalidOperationException>(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(responseObject == null);
            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests that specifically-crafted <see cref="WebRequest"/> is completed after one timeout.
        /// </summary>
        [Test, Retry(5)]
        public void TestOneTimeout()
        {
            var request = new DelayedWebRequest
            {
                Method = HttpMethod.GET,
                Timeout = 1000,
                Delay = 2
            };

            Exception thrownException = null;
            request.Failed += e => thrownException = e;
            request.CompleteInvoked = () => request.Delay = 0;

            Assert.DoesNotThrow(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            Assert.IsTrue(thrownException == null);
            Assert.AreEqual(WebRequest.MAX_RETRIES, request.RetryCount);
        }

        /// <summary>
        /// Tests that a <see cref="WebRequest"/> will only timeout a maximum of <see cref="WebRequest.MAX_RETRIES"/> times before being aborted.
        /// </summary>
        [Test, Retry(5)]
        public void TestFailTimeout()
        {
            var request = new WebRequest("https://httpbin.org/delay/4")
            {
                Method = HttpMethod.GET,
                Timeout = 1000
            };

            Exception thrownException = null;
            request.Failed += e => thrownException = e;

            Assert.Throws<WebException>(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsTrue(thrownException != null);
            Assert.AreEqual(WebRequest.MAX_RETRIES, request.RetryCount);
            Assert.AreEqual(typeof(WebException), thrownException.GetType());
        }

        /// <summary>
        /// Tests being able to abort + restart a request.
        /// </summary>
        [Test, Retry(5)]
        public void TestEventUnbindOnCompletion([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinGetResponse>("https://httpbin.org/get") { Method = HttpMethod.GET };

            request.Started += () => { };
            request.Failed += e => { };
            request.DownloadProgress += (l1, l2) => { };
            request.UploadProgress += (l1, l2) => { };

            Assert.DoesNotThrow(request.Perform);

            var events = request.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public);
            foreach (var e in events)
            {
                var field = request.GetType().GetField(e.Name, BindingFlags.Instance | BindingFlags.Public);
                Assert.IsFalse(((Delegate)field?.GetValue(request))?.GetInvocationList().Length > 0);
            }
        }

        /// <summary>
        /// Tests being able to abort + restart a request.
        /// </summary>
        [Test, Retry(5)]
        public void TestUnbindOnDispose([Values(true, false)] bool async)
        {
            WebRequest request;
            using (request = new JsonWebRequest<HttpBinGetResponse>("https://httpbin.org/get") { Method = HttpMethod.GET })
            {
                request.Started += () => { };
                request.Failed += e => { };
                request.DownloadProgress += (l1, l2) => { };
                request.UploadProgress += (l1, l2) => { };

                Assert.DoesNotThrow(request.Perform);
            }

            var events = request.GetType().GetEvents(BindingFlags.Instance | BindingFlags.Public);
            foreach (var e in events)
            {
                var field = request.GetType().GetField(e.Name, BindingFlags.Instance | BindingFlags.Public);
                Assert.IsFalse(((Delegate)field?.GetValue(request))?.GetInvocationList().Length > 0);
            }
        }

        [Test, Retry(5)]
        public void TestPostWithJsonResponse([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinPostResponse>("https://httpbin.org/post") { Method = HttpMethod.POST };

            request.AddParameter("testkey1", "testval1");
            request.AddParameter("testkey2", "testval2");

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            Assert.IsTrue(responseObject.Form != null);
            Assert.IsTrue(responseObject.Form.Count == 2);

            Assert.IsTrue(responseObject.Headers.ContentLength > 0);

            Assert.IsTrue(responseObject.Form.ContainsKey("testkey1"));
            Assert.IsTrue(responseObject.Form["testkey1"] == "testval1");

            Assert.IsTrue(responseObject.Form.ContainsKey("testkey2"));
            Assert.IsTrue(responseObject.Form["testkey2"] == "testval2");

            Assert.IsTrue(responseObject.Headers.ContentType.StartsWith("multipart/form-data; boundary="));
        }

        [Test, Retry(5)]
        public void TestPostWithJsonRequest([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinPostResponse>("https://httpbin.org/post") { Method = HttpMethod.POST };

            var testObject = new TestObject();
            request.AddRaw(JsonConvert.SerializeObject(testObject));

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            Assert.IsTrue(responseObject.Headers.ContentLength > 0);
            Assert.IsTrue(responseObject.Json != null);
            Assert.AreEqual(testObject.TestString, responseObject.Json.TestString);

            Assert.IsTrue(responseObject.Headers.ContentType == null);
        }

        [Test, Retry(5)]
        public void TestGetBinaryData([Values(true, false)] bool async, [Values(true, false)] bool chunked)
        {
            const int bytes_count = 65536;
            const int chunk_size = 1024;

            string endpoint = chunked ? "stream-bytes" : "bytes";

            WebRequest request = new WebRequest($"http://httpbin.org/{endpoint}/{bytes_count}") { Method = HttpMethod.GET };
            if (chunked)
                request.AddParameter("chunk_size", chunk_size.ToString());

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            Assert.AreEqual(bytes_count, request.ResponseStream.Length);
        }

        [Serializable]
        private class HttpBinGetResponse
        {
            [JsonProperty("headers")]
            public HttpBinHeaders Headers { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }
        }


        [Serializable]
        private class HttpBinPostResponse
        {
            [JsonProperty("data")]
            public string Data { get; set; }

            [JsonProperty("form")]
            public IDictionary<string, string> Form { get; set; }

            [JsonProperty("headers")]
            public HttpBinHeaders Headers { get; set; }

            [JsonProperty("json")]
            public TestObject Json { get; set; }
        }

        [Serializable]
        public class HttpBinHeaders
        {
            [JsonProperty("Content-Length")]
            public int ContentLength { get; set; }

            [JsonProperty("Content-Type")]
            public string ContentType { get; set; }

            [JsonProperty("User-Agent")]
            public string UserAgent { get; set; }
        }

        [Serializable]
        public class TestObject
        {
            public string TestString = "readable";
        }

        private class DelayedWebRequest : WebRequest
        {
            public Action CompleteInvoked;

            private int delay;

            public int Delay
            {
                get { return delay; }
                set
                {
                    delay = value;
                    Url = $"http://httpbin.org/delay/{delay}";
                }
            }

            public DelayedWebRequest()
                : base("http://httpbin.org/delay/0")
            {
            }

            protected override void Complete(Exception e = null)
            {
                CompleteInvoked?.Invoke();
                base.Complete(e);
            }
        }
    }
}
