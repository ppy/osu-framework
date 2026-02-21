
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using osu.Framework.Text;

namespace osu.Framework.Tests.Text
{
    [TestFixture]
    public class FontVariationTest
    {
        private OutlineFont outlineFont = null!;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            outlineFont = new OutlineFont(
                new NamespacedResourceStore<byte[]>(
                    new DllResourceStore(typeof(Game).Assembly), @"Resources"
                ),
                "Fonts/Roboto/Roboto"
            );

            outlineFont.LoadAsync().WaitSafely();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            outlineFont.Dispose();
        }

        [Test]
        public void TestAxes()
        {
            var variation = new FontVariation
            {
                Axes = new Dictionary<string, double>
                {
                    { @"wght", 123 },
                    { @"wdth", 78.5 },
                },
            };

            Assert.AreEqual(variation.GenerateInstanceName(@"Roboto"), @"Roboto_123wght_78.5wdth");

            var rawVariation = outlineFont.DecodeFontVariation(variation);
            Assert.NotNull(rawVariation);
            Assert.NotZero(rawVariation!.Axes.Length);
            Assert.Zero(rawVariation.NamedInstance);
        }

        [Test]
        public void TestNamedInstance()
        {
            var variation = new FontVariation
            {
                NamedInstance = "Roboto-Regular",
            };

            Assert.AreEqual(variation.GenerateInstanceName(@"Roboto"), @"Roboto-Regular");
            Assert.AreEqual(variation.GenerateInstanceName(@"some other font"), @"Roboto-Regular");

            var rawVariation = outlineFont.DecodeFontVariation(variation);
            Assert.NotNull(rawVariation);
            Assert.Zero(rawVariation!.Axes.Length);
            Assert.NotZero(rawVariation.NamedInstance);
        }

        [Test]
        public void TestAxesOverrideNamedInstance()
        {
            var variation = new FontVariation
            {
                Axes = new Dictionary<string, double> { { @"wght", 123 } },
                NamedInstance = "Roboto-Regular",
            };

            Assert.AreEqual(variation.GenerateInstanceName(@"Roboto"), @"Roboto_123wght");

            var rawVariation = outlineFont.DecodeFontVariation(variation);
            Assert.NotNull(rawVariation);
            Assert.NotZero(rawVariation!.Axes.Length);
            Assert.Zero(rawVariation.NamedInstance);
        }

        [Test]
        public void TestNull()
        {
            var rawVariation = outlineFont.DecodeFontVariation(null);
            Assert.Null(rawVariation);
        }

        [Test]
        public void TestManyAxes()
        {
            var variation = new FontVariation
            {
                Axes = new Dictionary<string, double>
                {
                    { @"wght", 1000 },
                    { @"wdth", 50.12345 },
                    { @"opsz", 12 },
                    { @"GRAD", -100 },
                    { @"slnt", -10 },
                    { @"XTRA", 456.78901 },
                    { @"XOPQ", 34.56789 },
                    { @"YOPQ", 56.78901 },
                    { @"YTLC", 500.12345 },
                    { @"YTUC", 600.12345 },
                    { @"YTAS", 700.12345 },
                    { @"YTDE", -555.55555 },
                    { @"YTFI", 600.12345 },
                },
            };

            const string expected = @"RobotoFlex-F8439096C93C09CF75D5DC6A16A20B99E7A65C932EC1E18731F74415AA77DCDB";
            Assert.AreEqual(variation.GenerateInstanceName(@"RobotoFlex"), expected);
        }
    }
}
