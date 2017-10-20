// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.IO.Network;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestWebRequest
    {
        private const string valid_get_url = "a.ppy.sh/2";
        private const string invalid_get_url = "a.ppy.shhhhh";

        [TestCase("http")]
        [TestCase("https")]
        public void TestValidGet(string protocol)
        {
            var request = new WebRequest($"{protocol}://{valid_get_url}") { Method = HttpMethod.GET };

            bool hasThrown = false;
            request.Finished += (webRequest, exception) => hasThrown = exception != null;

            Assert.DoesNotThrowAsync(request.PerformAsync);
            Assert.AreNotEqual(0, request.ResponseData.Length);
            Assert.IsFalse(request.Aborted);
            Assert.IsTrue(request.Completed);
            Assert.IsFalse(hasThrown);
        }

        [TestCase("http")]
        [TestCase("https")]
        public void TestInvalidGet(string protocol)
        {
            var request = new WebRequest($"{protocol}://{invalid_get_url}") { Method = HttpMethod.GET };

            bool hasThrown = false;
            request.Finished += (webRequest, exception) => hasThrown = exception != null;

            Assert.DoesNotThrowAsync(request.PerformAsync);
            Assert.AreEqual(null, request.ResponseData);
            Assert.IsTrue(request.Aborted);
            Assert.IsFalse(request.Completed);
            Assert.IsTrue(hasThrown);
        }

        [Test]
        public void TestBadStatusCode()
        {
            var request = new WebRequest("https://httpbin.org/hidden-basic-auth/user/passwd");

            bool hasThrown = false;
            request.Finished += (webRequest, exception) => hasThrown = exception != null;

            Assert.DoesNotThrowAsync(request.PerformAsync);

            Assert.AreEqual(string.Empty, request.ResponseString);
            Assert.IsFalse(request.Aborted);
            Assert.IsTrue(request.Completed);
            Assert.IsFalse(hasThrown);
        }

        [Test]
        public void TestAbortGet()
        {
            var request = new WebRequest($"https://{valid_get_url}") { Method = HttpMethod.GET };

            bool hasThrown = false;
            request.Finished += (webRequest, exception) => hasThrown = exception != null;

            request.PerformAsync();
            request.Abort();

            Thread.Sleep(100);

            Assert.AreEqual(null, request.ResponseData);
            Assert.IsTrue(request.Aborted);
            Assert.IsFalse(request.Completed);
            Assert.IsFalse(hasThrown);
        }

        [Test]
        public void TestPostWithJsonResponse()
        {
            var request = new JsonWebRequest<HttpBinResponse>("https://httpbin.org/post") { Method = HttpMethod.POST };

            request.AddParameter("testkey1", "testval1");
            request.AddParameter("testkey2", "testval2");

            Assert.DoesNotThrowAsync(request.PerformAsync);

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

        [Test]
        public void TestPostWithJsonRequest()
        {
            var request = new JsonWebRequest<HttpBinResponse>("https://httpbin.org/post") { Method = HttpMethod.POST };

            var testObject = new TestObject();
            request.AddRaw(JsonConvert.SerializeObject(testObject));

            Assert.DoesNotThrowAsync(request.PerformAsync);

            var responseObject = request.ResponseObject;

            Assert.IsFalse(request.Aborted);
            Assert.IsTrue(request.Completed);

            Assert.IsTrue(responseObject.Headers.ContentLength > 0);
            Assert.IsTrue(responseObject.Json != null);
            Assert.AreEqual(testObject.TestString, responseObject.Json.TestString);

            Assert.IsTrue(responseObject.Headers.ContentType == null);
        }

        [Serializable]
        private class HttpBinResponse
        {
            [JsonProperty("data")]
            public string Data { get; set; }

            [JsonProperty("form")]
            public IDictionary<string, string> Form { get; set; }

            [JsonProperty("headers")]
            public HttpBinHeaders Headers { get; set; }

            [JsonProperty("json")]
            public TestObject Json { get; set; }

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
        }

        [Serializable]
        public class TestObject
        {
            public string TestString = "readable";
        }
    }
}
