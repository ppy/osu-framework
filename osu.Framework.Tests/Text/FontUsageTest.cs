// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Tests.Text
{
    [TestFixture]
    public class FontUsageTest
    {
        [Test]
        public void PlainFont()
        {
            var usage = new FontUsage("Inter");

            Assert.Multiple(() =>
            {
                Assert.That(usage.Family, Is.EqualTo("Inter"));
                Assert.That(usage.FontName, Is.EqualTo("Inter"));
                Assert.That(usage.Weight, Is.Null);
                Assert.That(usage.Italics, Is.False);
                Assert.That(usage.FontNameNoFamily, Is.EqualTo(string.Empty));
            });
        }

        [Test]
        public void ItalicFont()
        {
            var usage = new FontUsage("Inter", italics: true);

            Assert.Multiple(() =>
            {
                Assert.That(usage.Family, Is.EqualTo("Inter"));
                Assert.That(usage.FontName, Is.EqualTo("Inter-Italic"));
                Assert.That(usage.Weight, Is.Null);
                Assert.That(usage.Italics, Is.True);
                Assert.That(usage.FontNameNoFamily, Is.EqualTo("-Italic"));
            });
        }

        [Test]
        public void FontWithWeight()
        {
            var usage = new FontUsage("Inter", weight: "SemiBold");

            Assert.Multiple(() =>
            {
                Assert.That(usage.Family, Is.EqualTo("Inter"));
                Assert.That(usage.FontName, Is.EqualTo("Inter-SemiBold"));
                Assert.That(usage.Weight, Is.EqualTo("SemiBold"));
                Assert.That(usage.Italics, Is.False);
                Assert.That(usage.FontNameNoFamily, Is.EqualTo("-SemiBold"));
            });
        }

        [Test]
        public void ItalicFontWithWeight()
        {
            var usage = new FontUsage("Inter", italics: true, weight: "SemiBold");

            Assert.Multiple(() =>
            {
                Assert.That(usage.Family, Is.EqualTo("Inter"));
                Assert.That(usage.FontName, Is.EqualTo("Inter-SemiBoldItalic"));
                Assert.That(usage.Weight, Is.EqualTo("SemiBold"));
                Assert.That(usage.Italics, Is.True);
                Assert.That(usage.FontNameNoFamily, Is.EqualTo("-SemiBoldItalic"));
            });
        }

        [Test]
        public void StructCtor()
        {
            var usage = new FontUsage();

            Assert.Multiple(() =>
            {
                Assert.That(usage.Family, Is.Null);
                Assert.That(usage.FontName, Is.Null);
                Assert.That(usage.Weight, Is.Null);
                Assert.That(usage.Italics, Is.False);
                Assert.That(usage.FontNameNoFamily, Is.Null);
            });
        }

        [Test]
        public void Default()
        {
            var usage = FontUsage.Default; // equivalent to `new FontUsage(null)` which IS NOT equivalent to `new FontUsage()`

            Assert.Multiple(() =>
            {
                Assert.That(usage.Family, Is.Null);
                Assert.That(usage.FontName, Is.Empty);
                Assert.That(usage.Weight, Is.Null);
                Assert.That(usage.Italics, Is.False);
                Assert.That(usage.FontNameNoFamily, Is.Empty);
            });
        }

        [Test]
        public void EmptyFamilyWithWeightAndItalics()
        {
            var usage = new FontUsage(weight: "Thin", italics: true);

            Assert.Multiple(() =>
            {
                Assert.That(usage.Family, Is.Null);
                Assert.That(usage.FontName, Is.EqualTo("-ThinItalic"));
                Assert.That(usage.Weight, Is.EqualTo("Thin"));
                Assert.That(usage.Italics, Is.True);
                Assert.That(usage.FontNameNoFamily, Is.EqualTo("-ThinItalic"));
            });
        }

        [Test]
        public void FontFamilyEndsWithHyphen()
        {
            var usage = new FontUsage("Hyphens-");

            Assert.Multiple(() =>
            {
                Assert.That(usage.Family, Is.EqualTo("Hyphens-"));
                Assert.That(usage.FontName, Is.EqualTo("Hyphens-"));
                Assert.That(usage.Weight, Is.Null);
                Assert.That(usage.Italics, Is.False);
                Assert.That(usage.FontNameNoFamily, Is.EqualTo(string.Empty));
            });
        }

        [Test]
        public void FontFamilyEndsWithHyphen_AlsoWithWeightAndItalics()
        {
            var usage = new FontUsage("Hyphens-", italics: true, weight: "SemiBold");

            Assert.Multiple(() =>
            {
                Assert.That(usage.Family, Is.EqualTo("Hyphens-"));
                Assert.That(usage.FontName, Is.EqualTo("Hyphens--SemiBoldItalic"));
                Assert.That(usage.Weight, Is.EqualTo("SemiBold"));
                Assert.That(usage.Italics, Is.True);
                Assert.That(usage.FontNameNoFamily, Is.EqualTo("-SemiBoldItalic"));
            });
        }
    }
}
