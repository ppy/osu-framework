// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using NUnit.Framework;
using osu.Framework.Localisation;

namespace osu.Framework.Tests.Localisation
{
    [TestFixture]
    public class CultureInfoHelperTest
    {
        private const string invariant_culture = "";

        [TestCase("en-US", true, "en-US")]
        [TestCase("invalid name", false, invariant_culture)]
        [TestCase(invariant_culture, true, invariant_culture)]
        [TestCase("ko_KR", false, invariant_culture)]
        public void TestTryGetCultureInfo(string name, bool expectedReturnValue, string expectedCultureName)
        {
            CultureInfo expectedCulture;

            switch (expectedCultureName)
            {
                case invariant_culture:
                    expectedCulture = CultureInfo.InvariantCulture;
                    break;

                default:
                    expectedCulture = CultureInfo.GetCultureInfo(expectedCultureName);
                    break;
            }

            bool retVal = CultureInfoHelper.TryGetCultureInfo(name, out var culture);

            Assert.That(retVal, Is.EqualTo(expectedReturnValue));
            Assert.That(culture, Is.EqualTo(expectedCulture));
        }
    }
}
