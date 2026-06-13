
// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using osu.Framework.Text;

namespace osu.Framework.Tests.Text
{
    [TestFixture]
    public class OutlineFontTest
    {
        private OutlineFont outlineFont = null!;
        private RawFontVariation? variation;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            outlineFont = new OutlineFont(
                new NamespacedResourceStore<byte[]>(
                    new DllResourceStore(typeof(Game).Assembly), @"Resources"
                ),
                @"Fonts/Roboto/Roboto"
            );
            outlineFont.LoadAsync().WaitSafely();

            variation = outlineFont.DecodeFontVariation(new FontVariation
            {
                NamedInstance = @"Roboto-Regular",
            });
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            outlineFont.Dispose();
        }

        [Test]
        public void TestGetGlyphIndex()
        {
            Assert.NotZero(outlineFont.GetGlyphIndex('A'));
            Assert.Zero(outlineFont.GetGlyphIndex('\ufffe'));
        }

        [Test]
        public void TestGetMetrics()
        {
            uint glyph = outlineFont.GetGlyphIndex('A');
            var metrics = outlineFont.GetMetrics(glyph, variation);

            Assert.NotNull(metrics);
            Assert.AreEqual(metrics!.Character, '\uffff');

            Assert.Null(outlineFont.GetMetrics(int.MaxValue, variation));
        }

        [Test]
        public void TestGlyphStores()
        {
            using (var regular = new OutlineGlyphStore(outlineFont, @"Roboto-Regular"))
            using (var bold = new OutlineGlyphStore(outlineFont, @"Roboto-Bold"))
            {
                var regularA = regular.Get('A');
                var boldA = bold.Get('A');
                Assert.NotNull(regularA);
                Assert.NotNull(boldA);

                Assert.AreNotEqual(regularA, boldA);

                // the same OutlineGlyphStore should return identical
                // metrics for the same character (required as a single
                // OutlineFont can be shared by multiple OutlineGlyphStores)
                var regularA2 = regular.Get('A');
                Assert.NotNull(regularA2);

                Assert.AreEqual(regularA!.Character, regularA2!.Character);
                Assert.AreEqual(regularA.Baseline, regularA2.Baseline);
                Assert.AreEqual(regularA.XOffset, regularA2.XOffset);
                Assert.AreEqual(regularA.YOffset, regularA2.YOffset);
                Assert.AreEqual(regularA.XAdvance, regularA2.XAdvance);
            }
        }
    }
}
