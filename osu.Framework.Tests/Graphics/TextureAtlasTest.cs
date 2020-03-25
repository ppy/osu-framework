using NUnit.Framework;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using System;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class TextureAtlasTest
    {
        private void testWithSize(int width, int height)
        {
            TextureAtlas atlas = new TextureAtlas(1024, 1024);
            TextureGL texture = atlas.Add(width, height);
            if(texture != null)
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

        [Test]
        public void TestAtlasAdd()
        {
            var testSizes = new[]{ (0, 0), (1, 1), (984, 20), (985, 20), (1020, 20), (1024, 20), (1025, 20),
                                    (20, 984), (20, 985), (20, 1020), (20, 1024), (20, 1500),
                                    (985, 985), (1020, 985), (1500, 985),
                                    (1020, 1020), (1500, 1500) };
            foreach (var size in testSizes)
            {
                Assert.DoesNotThrow(() => testWithSize(size.Item1, size.Item2), $"Size {size.Item1}x{size.Item2} has thrown an exception");
            }
        }
    }
}