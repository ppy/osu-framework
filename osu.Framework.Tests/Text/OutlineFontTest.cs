
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
        private RawFontVariation? variation = null;

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

            variation = outlineFont.DecodeFontVariation(new FontVariation
            {
                NamedInstance = "Roboto-Regular",
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
    }
}
