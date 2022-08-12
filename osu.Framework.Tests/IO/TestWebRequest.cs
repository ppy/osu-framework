// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.IO.Network;
using WebRequest = osu.Framework.IO.Network.WebRequest;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    [Category("httpbin")]
    public class TestWebRequest
    {
        private const string default_protocol = "http";
        private const string invalid_get_url = "a.ppy.shhhhh";

        private static readonly string host;
        private static readonly IEnumerable<string> protocols;

        static TestWebRequest()
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

        [Test, Retry(5)]
        public void TestValidGet([ValueSource(nameof(protocols))] string protocol, [Values(true, false)] bool async)
        {
            string url = $"{protocol}://{host}/get";
            var request = new JsonWebRequest<HttpBinGetResponse>(url)
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true
            };

            testValidGetInternal(async, request, "osu-framework");
        }

        /// <summary>
        /// Ensure that synchronous requests can be run without issue from within a TPL thread pool thread.
        /// Not recommended as it would block the thread, but we've deemed to allow this for now.
        /// </summary>
        [Test, Retry(5)]
        public void TestValidGetFromTask([ValueSource(nameof(protocols))] string protocol)
        {
            string url = $"{protocol}://{host}/get";
            var request = new JsonWebRequest<HttpBinGetResponse>(url)
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true
            };

            Task.Run(() => testValidGetInternal(false, request, "osu-framework")).WaitSafely();
        }

        [Test, Retry(5)]
        public void TestCustomUserAgent([ValueSource(nameof(protocols))] string protocol, [Values(true, false)] bool async)
        {
            string url = $"{protocol}://{host}/get";
            var request = new CustomUserAgentWebRequest(url)
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true
            };

            testValidGetInternal(async, request, "custom-ua");
        }

        private static void testValidGetInternal(bool async, JsonWebRequest<HttpBinGetResponse> request, string expectedUserAgent)
        {
            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            if (async)
                Assert.DoesNotThrowAsync(() => request.PerformAsync());
            else
                Assert.DoesNotThrow(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(responseObject != null);
            Assert.IsTrue(responseObject.Headers.UserAgent == expectedUserAgent);

            // disabled due to hosted version returning incorrect response (https://github.com/postmanlabs/httpbin/issues/545)
            // Assert.AreEqual(url, responseObject.Url);

            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests async execution is correctly yielding during IO wait time.
        /// </summary>
        [Test]
        public void TestConcurrency()
        {
            const int request_count = 10;
            const int induced_delay = 5;

            int finished = 0;
            int failed = 0;
            int started = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            List<long> startTimes = new List<long>();

            List<Task> running = new List<Task>();

            for (int i = 0; i < request_count; i++)
            {
                var request = new DelayedWebRequest
                {
                    Method = HttpMethod.Get,
                    AllowInsecureRequests = true,
                    Delay = induced_delay
                };

                request.Started += () =>
                {
                    Interlocked.Increment(ref started);
                    lock (startTimes)
                        startTimes.Add(sw.ElapsedMilliseconds);
                };
                request.Finished += () => Interlocked.Increment(ref finished);
                request.Failed += _ =>
                {
                    Interlocked.Increment(ref failed);
                    Interlocked.Increment(ref finished);
                };

                running.Add(request.PerformAsync());
            }

            Task.WaitAll(running.ToArray());

            Assert.Zero(failed);

            // in the case threads are not yielding, the time taken will be greater than double the induced delay (after considering latency).
            Assert.Less(sw.ElapsedMilliseconds, induced_delay * 2 * 1000);

            Assert.AreEqual(request_count, started);

            Assert.AreEqual(request_count, finished);

            Assert.AreEqual(request_count, startTimes.Count);

            // another case would be requests starting too late into the test. just to make sure.
            for (int i = 0; i < request_count; i++)
                Assert.Less(startTimes[i] - startTimes[0], induced_delay * 1000);
        }

        [Test, Retry(5)]
        public void TestInvalidGetExceptions([ValueSource(nameof(protocols))] string protocol, [Values(true, false)] bool async)
        {
            var request = new WebRequest($"{protocol}://{invalid_get_url}")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true
            };

            Exception finishedException = null;
            request.Failed += exception => finishedException = exception;

            if (async)
                Assert.ThrowsAsync<HttpRequestException>(() => request.PerformAsync());
            else
                Assert.Throws<HttpRequestException>(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsTrue(request.GetResponseString() == null);
            Assert.IsNotNull(finishedException);
        }

        [Test, Retry(5)]
        public void TestBadStatusCode([Values(true, false)] bool async)
        {
            var request = new WebRequest($"{default_protocol}://{host}/hidden-basic-auth/user/passwd")
            {
                AllowInsecureRequests = true,
            };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            if (async)
                Assert.ThrowsAsync<WebException>(() => request.PerformAsync());
            else
                Assert.Throws<WebException>(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsTrue(hasThrown);
        }

        [Test, Retry(5)]
        public void TestJsonWebRequestThrowsCorrectlyOnMultipleErrors([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<Drawable>("badrequest://www.google.com")
            {
                AllowInsecureRequests = true,
            };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            if (async)
                Assert.ThrowsAsync<NotSupportedException>(() => request.PerformAsync());
            else
                Assert.Throws<NotSupportedException>(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsNull(request.GetResponseString());
            Assert.IsNull(request.ResponseObject);

            Assert.IsTrue(hasThrown);
        }

        /// <summary>
        /// Tests aborting the <see cref="WebRequest"/> after response has been received from the server
        /// but before data has been read.
        /// </summary>
        [Test, Retry(5)]
        public void TestAbortReceive([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinGetResponse>($"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;
            request.Started += () => request.Abort();

            if (async)
                Assert.DoesNotThrowAsync(() => request.PerformAsync());
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
            var request = new JsonWebRequest<HttpBinGetResponse>($"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            Task.Run(() => request.PerformAsync());

            Assert.DoesNotThrow(request.Abort);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsTrue(request.ResponseObject == null);

            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests not being able to perform a request after an abort (before any perform).
        /// </summary>
        [Test, Retry(5)]
        public void TestStartAfterAbort([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinGetResponse>($"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            Assert.DoesNotThrow(request.Abort);

            if (async)
                Assert.ThrowsAsync<OperationCanceledException>(() => request.PerformAsync());
            else
                Assert.Throws<TaskCanceledException>(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(responseObject == null);
            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests not being able to perform a request after an initial perform-abort sequence.
        /// </summary>
        [Test, Retry(5)]
        public void TestRestartAfterAbort([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinGetResponse>($"{default_protocol}://{host}/delay/10")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            var _ = request.PerformAsync();

            Assert.DoesNotThrow(request.Abort);

            if (async)
                Assert.ThrowsAsync<OperationCanceledException>(() => request.PerformAsync());
            else
                Assert.Throws<TaskCanceledException>(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(responseObject == null);
            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests cancelling the <see cref="WebRequest"/> after response has been received from the server
        /// but before data has been read.
        /// </summary>
        [Test, Retry(5)]
        public void TestCancelReceive()
        {
            var cancellationSource = new CancellationTokenSource();
            var request = new JsonWebRequest<HttpBinGetResponse>($"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;
            request.Started += () => cancellationSource.Cancel();

            Assert.DoesNotThrowAsync(() => request.PerformAsync(cancellationSource.Token));

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsTrue(request.ResponseObject == null);
            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests aborting the <see cref="WebRequest"/> before the request is sent to the server.
        /// </summary>
        [Test, Retry(5)]
        public async Task TestCancelRequest()
        {
            var cancellationSource = new CancellationTokenSource();
            var request = new JsonWebRequest<HttpBinGetResponse>($"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            cancellationSource.Cancel();
            await request.PerformAsync(cancellationSource.Token).ConfigureAwait(false);

            Assert.IsTrue(request.Completed);
            Assert.IsTrue(request.Aborted);

            Assert.IsTrue(request.ResponseObject == null);

            Assert.IsFalse(hasThrown);
        }

        /// <summary>
        /// Tests being able to cancel + restart a request.
        /// </summary>
        [Test, Retry(5)]
        public void TestRestartAfterAbortViaCancellationToken()
        {
            var cancellationSource = new CancellationTokenSource();
            var request = new JsonWebRequest<HttpBinGetResponse>($"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            bool hasThrown = false;
            request.Failed += exception => hasThrown = exception != null;

            cancellationSource.Cancel();
            request.PerformAsync(cancellationSource.Token).WaitSafely();

            Assert.ThrowsAsync<OperationCanceledException>(() => request.PerformAsync(cancellationSource.Token));

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
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
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
            var request = new WebRequest($"{default_protocol}://{host}/delay/4")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
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
            var request = new JsonWebRequest<HttpBinGetResponse>($"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            request.Started += () => { };
            request.Failed += _ => { };
            request.DownloadProgress += (_, _) => { };
            request.UploadProgress += (_, _) => { };

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
            var request = new JsonWebRequest<HttpBinGetResponse>($"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            using (request)
            {
                request.Started += () => { };
                request.Failed += _ => { };
                request.DownloadProgress += (_, _) => { };
                request.UploadProgress += (_, _) => { };

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
        public void TestGetWithQueryStringParameters()
        {
            const string test_key_1 = "testkey1";
            const string test_val_1 = "testval1 that ends with a #";

            const string test_key_2 = "testkey2";
            const string test_val_2 = "testval2 that ends with a space ";

            var request = new JsonWebRequest<HttpBinGetResponse>($@"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true
            };

            request.AddParameter(test_key_1, test_val_1);
            request.AddParameter(test_key_2, test_val_2);

            Assert.DoesNotThrow(request.Perform);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            Assert.NotNull(responseObject.Arguments);

            Assert.True(responseObject.Arguments.ContainsKey(test_key_1));
            Assert.AreEqual(test_val_1, responseObject.Arguments[test_key_1]);

            Assert.True(responseObject.Arguments.ContainsKey(test_key_2));
            Assert.AreEqual(test_val_2, responseObject.Arguments[test_key_2]);
        }

        [Test, Retry(5)]
        public void TestPostWithJsonResponse([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinPostResponse>($"{default_protocol}://{host}/post")
            {
                Method = HttpMethod.Post,
                AllowInsecureRequests = true,
            };

            request.AddParameter("testkey1", "testval1");
            request.AddParameter("testkey2", "testval2");

            if (async)
                Assert.DoesNotThrowAsync(() => request.PerformAsync());
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

            Assert.IsTrue(responseObject.Headers.ContentType.StartsWith("multipart/form-data; boundary=", StringComparison.Ordinal));
        }

        [Test, Retry(5)]
        public void TestPostWithJsonRequest([Values(true, false)] bool async)
        {
            var request = new JsonWebRequest<HttpBinPostResponse>($"{default_protocol}://{host}/post")
            {
                Method = HttpMethod.Post,
                AllowInsecureRequests = true,
                ContentType = "application/json"
            };

            var testObject = new TestObject();
            request.AddRaw(JsonConvert.SerializeObject(testObject));

            if (async)
                Assert.DoesNotThrowAsync(() => request.PerformAsync());
            else
                Assert.DoesNotThrow(request.Perform);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            Assert.IsTrue(responseObject.Headers.ContentLength > 0);
            Assert.IsTrue(responseObject.Json != null);
            Assert.AreEqual(testObject.TestString, responseObject.Json.TestString);
        }

        [Test, Retry(5)]
        public void TestNoContentPost([Values(true, false)] bool async)
        {
            var request = new WebRequest($"{default_protocol}://{host}/post")
            {
                Method = HttpMethod.Post,
                AllowInsecureRequests = true,
            };

            if (async)
                Assert.DoesNotThrowAsync(() => request.PerformAsync());
            else
                Assert.DoesNotThrow(request.Perform);

            var responseJson = JsonConvert.DeserializeObject<HttpBinPostResponse>(request.GetResponseString());

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);
            Assert.AreEqual(0, responseJson?.Headers.ContentLength);
        }

        [Test, Retry(5)]
        public void TestPutWithQueryAndFormParams()
        {
            const string test_key_1 = "param1";
            const string test_val_1 = "in query! ";

            const string test_key_2 = "param2";
            const string test_val_2 = "in form!";

            const string test_key_3 = "param3";
            const string test_val_3 = "in form by default!";

            var request = new JsonWebRequest<HttpBinPutResponse>($"{default_protocol}://{host}/put")
            {
                Method = HttpMethod.Put,
                AllowInsecureRequests = true,
            };

            request.AddParameter(test_key_1, test_val_1, RequestParameterType.Query);
            request.AddParameter(test_key_2, test_val_2, RequestParameterType.Form);
            request.AddParameter(test_key_3, test_val_3);

            Assert.DoesNotThrow(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            var response = request.ResponseObject;

            Assert.NotNull(response.Arguments);
            Assert.True(response.Arguments.ContainsKey(test_key_1));
            Assert.AreEqual(test_val_1, response.Arguments[test_key_1]);

            Assert.NotNull(response.Form);
            Assert.True(response.Form.ContainsKey(test_key_2));
            Assert.AreEqual(test_val_2, response.Form[test_key_2]);

            Assert.NotNull(response.Form);
            Assert.True(response.Form.ContainsKey(test_key_3));
            Assert.AreEqual(test_val_3, response.Form[test_key_3]);
        }

        [Test]
        public void TestFormParamsNotSupportedForGet()
        {
            var request = new JsonWebRequest<HttpBinPutResponse>($"{default_protocol}://{host}/get")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };

            Assert.Throws<ArgumentException>(() => request.AddParameter("cannot", "work", RequestParameterType.Form));
        }

        /// <summary>
        /// Ensure the work load, and importantly, the continuations do not run on the TPL thread pool.
        /// Since we have our own task schedulers handling these load tasks.
        /// </summary>
        [Test]
        public void TestSynchronousFlowTplReliance()
        {
            int workerMin;
            int completionMin;
            int workerMax;
            int completionMax;

            // set limited threadpool capacity
            ThreadPool.GetMinThreads(out workerMin, out completionMin);
            ThreadPool.GetMaxThreads(out workerMax, out completionMax);

            try
            {
                /*
                Note that we explicitly choose two threads here to reproduce a classic thread pool deadlock scenario (which was surfacing due to a `.Wait()` call from within an `async` context).
                If set to one, a task required by the NUnit hosting process (usage of ManualResetEventSlim.Wait) will cause requests to never work.
                If set to above two, the deadlock will not reliably reproduce.

                Also note that the TPL thread pool generally gets much higher values than this (based on logical core count) and will expand with demand.
                This is explicitly testing for a case that came up on Github Actions due to limited processor count and refusal to expand the thread pool (for whatever reason).

                This may require adjustment in the future if we end up using more thread pool threads in the background, or if NUnit changes how they used them.
                */

                ThreadPool.SetMinThreads(2, 2);
                ThreadPool.SetMaxThreads(2, 2);

                var request = new DelayedWebRequest
                {
                    Method = HttpMethod.Get,
                    AllowInsecureRequests = true,
                    Timeout = 1000,
                    Delay = 2
                };

                request.CompleteInvoked = () => request.Delay = 0;

                request.Perform();
            }
            finally
            {
                // restore capacity
                ThreadPool.SetMinThreads(workerMin, completionMin);
                ThreadPool.SetMaxThreads(workerMax, completionMax);
            }
        }

        [Test, Retry(5)]
        public void TestGetBinaryData([Values(true, false)] bool async, [Values(true, false)] bool chunked)
        {
            const int bytes_count = 65536;
            const int chunk_size = 1024;

            string endpoint = chunked ? "stream-bytes" : "bytes";

            WebRequest request = new WebRequest($"{default_protocol}://{host}/{endpoint}/{bytes_count}")
            {
                Method = HttpMethod.Get,
                AllowInsecureRequests = true,
            };
            if (chunked)
                request.AddParameter("chunk_size", chunk_size.ToString());

            if (async)
                Assert.DoesNotThrowAsync(() => request.PerformAsync());
            else
                Assert.DoesNotThrow(request.Perform);

            Assert.IsTrue(request.Completed);
            Assert.IsFalse(request.Aborted);

            Assert.AreEqual(bytes_count, request.ResponseStream.Length);
        }

        private static Dictionary<string, string> convertDictionary(Dictionary<string, object> dict)
        {
            var result = new Dictionary<string, string>();

            foreach (var kvp in dict)
            {
                switch (kvp.Value)
                {
                    case string strValue:
                        result[kvp.Key] = strValue;
                        break;

                    case JArray strArray:
                        result[kvp.Key] = strArray.Count == 0 ? null : strArray[0].ToString();
                        break;
                }
            }

            return result;
        }

        private static T convertObject<T>(object obj) where T : IConvertible
        {
            switch (obj)
            {
                case int intVal:
                    return (T)Convert.ChangeType(intVal, typeof(T));

                case string strVal:
                    return (T)Convert.ChangeType(strVal, typeof(T));

                case JArray strArray:
                    return (T)Convert.ChangeType(strArray.Count == 0 ? string.Empty : strArray[0], typeof(T));

                default:
                    return default;
            }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptIn)]
        private class HttpBinGetResponse
        {
            public Dictionary<string, string> Arguments => convertDictionary(arguments);

            [JsonProperty("headers")]
            public HttpBinHeaders Headers { get; set; }

            [JsonProperty("url")]
            public string Url { get; set; }

            [JsonProperty("args")]
            private Dictionary<string, object> arguments { get; set; }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptIn)]
        private class HttpBinPostResponse
        {
            [JsonProperty("data")]
            public string Data { get; set; }

            public Dictionary<string, string> Form => convertDictionary(form);

            [JsonProperty("headers")]
            public HttpBinHeaders Headers { get; set; }

            [JsonProperty("json")]
            public TestObject Json { get; set; }

            [JsonProperty("form")]
            private Dictionary<string, object> form { get; set; }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptIn)]
        private class HttpBinPutResponse
        {
            public Dictionary<string, string> Arguments => convertDictionary(arguments);

            public Dictionary<string, string> Form => convertDictionary(form);

            [JsonProperty("args")]
            private Dictionary<string, object> arguments { get; set; }

            [JsonProperty("form")]
            private Dictionary<string, object> form { get; set; }
        }

        [Serializable]
        [JsonObject(MemberSerialization.OptIn)]
        public class HttpBinHeaders
        {
            public int ContentLength => convertObject<int>(contentLength);

            public string ContentType => convertObject<string>(contentType);

            public string UserAgent => convertObject<string>(userAgent);

            [JsonProperty("Content-Length")]
            private object contentLength { get; set; }

            [JsonProperty("Content-Type")]
            private object contentType { get; set; }

            [JsonProperty("User-Agent")]
            private object userAgent { get; set; }
        }

        [Serializable]
        public class TestObject
        {
            public string TestString = "readable";
        }

        private class CustomUserAgentWebRequest : JsonWebRequest<HttpBinGetResponse>
        {
            public CustomUserAgentWebRequest(string url)
                : base(url)
            {
            }

            protected override string UserAgent => "custom-ua";
        }

        private class DelayedWebRequest : WebRequest
        {
            public Action CompleteInvoked;

            private int delay;

            public int Delay
            {
                get => delay;
                set
                {
                    delay = value;
                    Url = $"{default_protocol}://{host}/delay/{delay}";
                }
            }

            public DelayedWebRequest()
                : base($"{default_protocol}://{host}/delay/0")
            {
            }

            protected override Task Complete(Exception e = null)
            {
                CompleteInvoked?.Invoke();
                return base.Complete(e);
            }
        }
    }
}
