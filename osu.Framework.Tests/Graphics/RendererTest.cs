// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Tests.Graphics
{
    public class RendererTest
    {
        [Test]
        public void TestWhitePixelReuseUpdatesTextureWrapping()
        {
            DummyRenderer renderer = new DummyRenderer();

            renderer.BindTexture(renderer.WhitePixel, 0, WrapMode.None, WrapMode.None);
            Assert.That(renderer.CurrentWrapModeS, Is.EqualTo(WrapMode.None));
            Assert.That(renderer.CurrentWrapModeS, Is.EqualTo(WrapMode.None));

            renderer.BindTexture(renderer.WhitePixel, 0, WrapMode.ClampToEdge, WrapMode.ClampToEdge);
            Assert.That(renderer.CurrentWrapModeS, Is.EqualTo(WrapMode.ClampToEdge));
            Assert.That(renderer.CurrentWrapModeS, Is.EqualTo(WrapMode.ClampToEdge));

            renderer.BindTexture(renderer.WhitePixel, 0, WrapMode.None, WrapMode.None);
            Assert.That(renderer.CurrentWrapModeS, Is.EqualTo(WrapMode.None));
            Assert.That(renderer.CurrentWrapModeS, Is.EqualTo(WrapMode.None));
        }

        [Test]
        public void TestTextureAtlasReuseUpdatesTextureWrapping()
        {
            DummyRenderer renderer = new DummyRenderer();

            TextureAtlas atlas = new TextureAtlas(renderer, 1024, 1024);

            Texture textureWrapNone = atlas.Add(100, 100, WrapMode.None, WrapMode.None)!;
            Texture textureWrapClamp = atlas.Add(100, 100, WrapMode.ClampToEdge, WrapMode.ClampToEdge)!;

            renderer.BindTexture(textureWrapNone, 0, null, null);
            Assert.That(renderer.CurrentWrapModeS, Is.EqualTo(WrapMode.None));
            Assert.That(renderer.CurrentWrapModeT, Is.EqualTo(WrapMode.None));

            renderer.BindTexture(textureWrapClamp, 0, null, null);
            Assert.That(renderer.CurrentWrapModeS, Is.EqualTo(WrapMode.ClampToEdge));
            Assert.That(renderer.CurrentWrapModeT, Is.EqualTo(WrapMode.ClampToEdge));

            renderer.BindTexture(textureWrapNone, 0, null, null);
            Assert.That(renderer.CurrentWrapModeS, Is.EqualTo(WrapMode.None));
            Assert.That(renderer.CurrentWrapModeT, Is.EqualTo(WrapMode.None));
        }
    }
}
