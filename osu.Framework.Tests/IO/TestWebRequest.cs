// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.IO.Network;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestWebRequest
    {
        private const string valid_get_url = "httpbin.org/get";
        private const string invalid_get_url = "a.ppy.shhhhh";

        [TestCase("http", false)]
        [TestCase("https", false)]
        [TestCase("http", true)]
        [TestCase("https", true)]
        public void TestValidGet(string protocol, bool async)
        {
            var url = $"{protocol}://httpbin.org/get";
            var request = new JsonWebRequest<HttpBinGetResponse>(url) { Method = HttpMethod.GET };

            bool hasThrown = false;
            request.Finished += (webRequest, exception) => hasThrown = exception != null;

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            var responseObject = request.ResponseObject;

            Assert.IsTrue(responseObject != null);
            Assert.IsTrue(responseObject.Headers.UserAgent == "osu!");
            Assert.IsTrue(responseObject.Url == url);

            Assert.IsFalse(request.Aborted);
            Assert.IsTrue(request.Completed);
            Assert.IsFalse(hasThrown);
        }

        [TestCase("http", false)]
        [TestCase("https", false)]
        [TestCase("http", true)]
        [TestCase("https", true)]
        public void TestInvalidGetExceptions(string protocol, bool async)
        {
            var request = new WebRequest($"{protocol}://{invalid_get_url}") { Method = HttpMethod.GET };

            Exception finishedException = null;
            request.Finished += (webRequest, exception) => finishedException = exception;

            if (async)
                Assert.ThrowsAsync<AggregateException>(request.PerformAsync);
            else
                Assert.Throws<AggregateException>(request.Perform);

            Assert.IsTrue(request.ResponseString == null);

            Assert.IsTrue(request.Aborted);
            Assert.IsFalse(request.Completed);

            Assert.IsNotNull(finishedException);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestBadStatusCode(bool async)
        {
            var request = new WebRequest("https://httpbin.org/hidden-basic-auth/user/passwd");

            bool hasThrown = false;
            request.Finished += (webRequest, exception) => hasThrown = exception != null;

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            Assert.IsEmpty(request.ResponseString);

            Assert.IsFalse(request.Aborted);
            Assert.IsTrue(request.Completed);
            Assert.IsFalse(hasThrown);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestAbortGet(bool async)
        {
            var request = new JsonWebRequest<HttpBinGetResponse>("https://httpbin.org/get") { Method = HttpMethod.GET };

            bool hasThrown = false;
            request.Finished += (webRequest, exception) => hasThrown = exception != null;
            request.Started += webRequest => request.Abort();

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            Assert.IsTrue(request.ResponseObject == null);

            Assert.IsTrue(request.Aborted);
            Assert.IsFalse(request.Completed);
            Assert.IsFalse(hasThrown);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestPostWithJsonResponse(bool async)
        {
            var request = new JsonWebRequest<HttpBinPostResponse>("https://httpbin.org/post") { Method = HttpMethod.POST };

            request.AddParameter("testkey1", "testval1");
            request.AddParameter("testkey2", "testval2");

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            var responseObject = request.ResponseObject;

            Assert.IsFalse(request.Aborted);
            Assert.IsTrue(request.Completed);

            Assert.IsTrue(responseObject.Form != null);
            Assert.IsTrue(responseObject.Form.Count == 2);

            Assert.IsTrue(responseObject.Headers.ContentLength > 0);

            Assert.IsTrue(responseObject.Form.ContainsKey("testkey1"));
            Assert.IsTrue(responseObject.Form["testkey1"] == "testval1");

            Assert.IsTrue(responseObject.Form.ContainsKey("testkey2"));
            Assert.IsTrue(responseObject.Form["testkey2"] == "testval2");

            Assert.IsTrue(responseObject.Headers.ContentType.StartsWith("multipart/form-data; boundary="));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestPostWithJsonRequest(bool async)
        {
            var request = new JsonWebRequest<HttpBinPostResponse>("https://httpbin.org/post") { Method = HttpMethod.POST };

            var testObject = new TestObject();
            request.AddRaw(JsonConvert.SerializeObject(testObject));

            if (async)
                Assert.DoesNotThrowAsync(request.PerformAsync);
            else
                Assert.DoesNotThrow(request.Perform);

            var responseObject = request.ResponseObject;

            Assert.IsFalse(request.Aborted);
            Assert.IsTrue(request.Completed);

            Assert.IsTrue(responseObject.Headers.ContentLength > 0);
            Assert.IsTrue(responseObject.Json != null);
            Assert.AreEqual(testObject.TestString, responseObject.Json.TestString);

            Assert.IsTrue(responseObject.Headers.ContentType == null);
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
    }
}
