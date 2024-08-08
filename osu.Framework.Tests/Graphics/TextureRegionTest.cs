// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Dummy;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class TextureRegionTest
    {
        [Test]
        public void TestRegionHasCorrectSize()
        {
            var tex = new DummyRenderer().CreateTexture(100, 100);

            Assert.That(tex.Width, Is.EqualTo(100));
            Assert.That(tex.Height, Is.EqualTo(100));
            Assert.That(tex.DisplayWidth, Is.EqualTo(100));
            Assert.That(tex.DisplayHeight, Is.EqualTo(100));
            Assert.That(tex.GetTextureRect(), Is.EqualTo(new RectangleF(0, 0, 1, 1)));

            var subTex = tex.Crop(new RectangleF(0, 0, 50, 50));

            Assert.That(subTex.Width, Is.EqualTo(50));
            Assert.That(subTex.Height, Is.EqualTo(50));
            Assert.That(subTex.DisplayWidth, Is.EqualTo(50));
            Assert.That(subTex.DisplayHeight, Is.EqualTo(50));
            Assert.That(subTex.GetTextureRect(), Is.EqualTo(new RectangleF(0, 0, 0.5f, 0.5f)));
        }

        [Test]
        public void TestRegionHasCorrectSizeWithOffset()
        {
            var tex = new DummyRenderer().CreateTexture(100, 100);

            Assert.That(tex.Width, Is.EqualTo(100));
            Assert.That(tex.Height, Is.EqualTo(100));
            Assert.That(tex.DisplayWidth, Is.EqualTo(100));
            Assert.That(tex.DisplayHeight, Is.EqualTo(100));
            Assert.That(tex.GetTextureRect(), Is.EqualTo(new RectangleF(0, 0, 1, 1)));

            var subTex = tex.Crop(new RectangleF(25, 25, 50, 50));

            Assert.That(subTex.Width, Is.EqualTo(50));
            Assert.That(subTex.Height, Is.EqualTo(50));
            Assert.That(subTex.DisplayWidth, Is.EqualTo(50));
            Assert.That(subTex.DisplayHeight, Is.EqualTo(50));
            Assert.That(subTex.GetTextureRect(), Is.EqualTo(new RectangleF(0.25f, 0.25f, 0.5f, 0.5f)));
        }

        [Test]
        public void TestScaleAdjustOnlyAffectsDisplaySize()
        {
            var tex = new DummyRenderer().CreateTexture(100, 100);
            tex.ScaleAdjust = 2;

            Assert.That(tex.Width, Is.EqualTo(100));
            Assert.That(tex.Height, Is.EqualTo(100));
            Assert.That(tex.DisplayWidth, Is.EqualTo(50));
            Assert.That(tex.DisplayHeight, Is.EqualTo(50));
            Assert.That(tex.GetTextureRect(), Is.EqualTo(new RectangleF(0, 0, 1, 1)));

            var subTex = tex.Crop(new RectangleF(0, 0, 50, 50));

            Assert.That(subTex.Width, Is.EqualTo(50));
            Assert.That(subTex.Height, Is.EqualTo(50));
            Assert.That(subTex.DisplayWidth, Is.EqualTo(50));
            Assert.That(subTex.DisplayHeight, Is.EqualTo(50));
            Assert.That(subTex.GetTextureRect(), Is.EqualTo(new RectangleF(0, 0, 0.5f, 0.5f)));

            var subTexWithScaleAdjust = tex.Crop(new RectangleF(0, 0, 50, 50));
            subTexWithScaleAdjust.ScaleAdjust = 2;

            Assert.That(subTexWithScaleAdjust.Width, Is.EqualTo(50));
            Assert.That(subTexWithScaleAdjust.Height, Is.EqualTo(50));
            Assert.That(subTexWithScaleAdjust.DisplayWidth, Is.EqualTo(25));
            Assert.That(subTexWithScaleAdjust.DisplayHeight, Is.EqualTo(25));
            Assert.That(subTexWithScaleAdjust.GetTextureRect(), Is.EqualTo(new RectangleF(0, 0, 0.5f, 0.5f)));
        }
    }
}
