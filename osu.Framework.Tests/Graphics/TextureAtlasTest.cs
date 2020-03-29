// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
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
        [TestCase(1020, 20)]
        [TestCase(1024, 20)]
        [TestCase(1025, 20)]
        [TestCase(20, 984)]
        [TestCase(20, 985)]
        [TestCase(20, 1020)]
        [TestCase(20, 1024)]
        [TestCase(20, 1500)]
        [TestCase(985, 985)]
        [TestCase(1020, 985)]
        [TestCase(1500, 985)]
        [TestCase(1020, 1020)]
        [TestCase(1500, 1500)]
        public void TestAtlasAdd(int width, int height)
        {
            Assert.DoesNotThrow(() => testWithSize(width, height));
        }

        private void testWithSize(int width, int height)
        {
            TextureAtlas atlas = new TextureAtlas(1024, 1024);
            TextureGL texture = atlas.Add(width, height);

            if (texture != null)
            {
                Assert.AreEqual(texture.Width, width, message: $"Width: {texture.Width} != {width} for texture {width}x{height}");
                Assert.AreEqual(texture.Height, height, message: $"Height: {texture.Height} != {height} for texture {width}x{height}");

                RectangleF rect = texture.GetTextureRect(null);
                Assert.LessOrEqual(rect.X + rect.Width, 1, message: $"Returned texture is wider than TextureAtlas for texture {width}x{height}");
                Assert.LessOrEqual(rect.Y + rect.Height, 1, message: $"Returned texture is taller than TextureAtlas for texture {width}x{height}");
            }
            else
            {
                Assert.True(width > 1024 - TextureAtlas.PADDING || height > 1008 - TextureAtlas.PADDING ||
                            (width > 1024 - TextureAtlas.PADDING - TextureAtlas.WHITE_PIXEL_SIZE
                             && height > 1024 - TextureAtlas.PADDING - TextureAtlas.WHITE_PIXEL_SIZE),
                    message: $"Returned texture is null, but should have fit: {width}x{height}");
            }
        }
    }
}
