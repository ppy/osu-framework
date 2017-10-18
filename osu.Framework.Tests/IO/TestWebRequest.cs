// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

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
    }
}
