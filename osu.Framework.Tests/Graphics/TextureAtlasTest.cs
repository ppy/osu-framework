// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering.Dummy;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class TextureAtlasTest
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(984, 20)]
        [TestCase(985, 20)]
        [TestCase(1008, 20)]
        [TestCase(1020, 20)]
        [TestCase(1024, 20)]
        [TestCase(1025, 20)]
        [TestCase(20, 984)]
        [TestCase(20, 985)]
        [TestCase(20, 1008)]
        [TestCase(20, 1020)]
        [TestCase(20, 1024)]
        [TestCase(20, 1500)]
        [TestCase(984, 984)]
        [TestCase(985, 985)]
        [TestCase(1020, 985)]
        [TestCase(1500, 985)]
        [TestCase(1008, 1008)]
        [TestCase(1020, 1020)]
        [TestCase(1500, 1500)]
        public void TestAtlasAdd(int width, int height)
        {
            Assert.DoesNotThrow(() => testWithSize(width, height));
        }

        private void testWithSize(int width, int height)
        {
            TextureAtlas atlas = new TextureAtlas(new DummyRenderer(), 1024, 1024);
            Texture texture = atlas.Add(width, height);

            if (texture != null)
            {
                Assert.AreEqual(texture.Width, width, message: $"Width: {texture.Width} != {width} for texture {width}x{height}");
                Assert.AreEqual(texture.Height, height, message: $"Height: {texture.Height} != {height} for texture {width}x{height}");

                RectangleF rect = texture.GetTextureRect();
                Assert.LessOrEqual(rect.X + rect.Width, 1, message: $"Returned texture is wider than TextureAtlas for texture {width}x{height}");
                Assert.LessOrEqual(rect.Y + rect.Height, 1, message: $"Returned texture is taller than TextureAtlas for texture {width}x{height}");
            }
            else
            {
                Assert.True(width > 1024 - TextureAtlas.PADDING * 2 || height > 1024 - TextureAtlas.PADDING * 2 ||
                            (width > 1024 - TextureAtlas.PADDING * 2 - TextureAtlas.WHITE_PIXEL_SIZE
                             && height > 1024 - TextureAtlas.PADDING * 2 - TextureAtlas.WHITE_PIXEL_SIZE),
                    message: $"Returned texture is null, but should have fit: {width}x{height}");
            }
        }

        [Test]
        public void TestAtlasFirstRowAddRespectsWhitePixelSize()
        {
            const int atlas_size = 1024;

            var atlas = new TextureAtlas(new DummyRenderer(), atlas_size, atlas_size);

            Texture texture = atlas.Add(64, 64);

            RectangleF rect = texture.AsNonNull().GetTextureRect();
            Assert.GreaterOrEqual(atlas_size * rect.X, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, message: "Texture is placed on top of the white pixel");
            Assert.GreaterOrEqual(atlas_size * rect.Y, TextureAtlas.PADDING, message: "Texture has insufficient padding");
        }

        [Test]
        public void TestAtlasSecondRowAddRespectsWhitePixelSize()
        {
            const int atlas_size = 1024;

            var atlas = new TextureAtlas(new DummyRenderer(), atlas_size, atlas_size);

            Texture texture = atlas.Add(atlas_size - 2 * TextureAtlas.PADDING, 64);

            RectangleF rect = texture.AsNonNull().GetTextureRect();
            Assert.GreaterOrEqual(atlas_size * rect.X, TextureAtlas.PADDING, message: "Texture has insufficient padding");
            Assert.GreaterOrEqual(atlas_size * rect.Y, TextureAtlas.WHITE_PIXEL_SIZE + TextureAtlas.PADDING, message: "Texture is placed on top of the white pixel");
        }

        [Test]
        public void TestSubTextureIsAtlasTexture()
        {
            const int atlas_size = 1024;

            var atlas = new TextureAtlas(new DummyRenderer(), atlas_size, atlas_size);

            // Direct atlas texture.
            var tex1 = atlas.Add(1, 1).AsNonNull();

            // TextureRegion.
            var tex2 = tex1.Crop(new RectangleF(0, 0, 1, 1));

            // Redirected texture.
            var tex3 = new Texture(tex1);

            Assert.True(tex1.IsAtlasTexture);
            Assert.True(tex2.IsAtlasTexture);
            Assert.True(tex3.IsAtlasTexture);
        }
    }
}
