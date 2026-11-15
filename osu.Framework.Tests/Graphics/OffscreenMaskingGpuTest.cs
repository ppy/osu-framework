// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Development;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Veldrid;
using osu.Framework.IO.Stores;
using osuTK;
using osuTK.Graphics;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Graphics
{
    [TestFixture]
    public class OffscreenMaskingGpuTest
    {
        private const int size = 64;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            if (OperatingSystem.IsLinux() && !hasVulkanIcd())
                Assert.Ignore("No Vulkan ICD available on this host.");
        }

        [Test]
        public void TestUnmaskedQuadIsVisible()
        {
            withOfflineRenderer(renderer =>
            {
                var pixels = render(renderer, mask: false);

                Assert.That(pixels[index(size / 2, size / 2)], Is.EqualTo(new Rgba32(255, 0, 0, 255)));
                Assert.That(pixels[index(2, 2)], Is.EqualTo(new Rgba32(255, 0, 0, 255)));
            });
        }

        [Test]
        public void TestMaskedQuadClipsOutsidePixels()
        {
            withOfflineRenderer(renderer =>
            {
                var pixels = render(renderer, mask: true);

                Assert.That(pixels[index(size / 2, size / 2)], Is.EqualTo(new Rgba32(255, 255, 255, 255)),
                    "Interior of the mask disappeared.");
                Assert.That(pixels[index(2, 2)], Is.EqualTo(new Rgba32(0, 0, 0, 255)),
                    "Pixels outside the mask were not clipped.");
                Assert.That(pixels[index(size - 3, size - 3)], Is.EqualTo(new Rgba32(0, 0, 0, 255)),
                    "Pixels outside the mask were not clipped.");
            });
        }

        private static void withOfflineRenderer(Action<VeldridRenderer> action)
        {
            bool wasDrawThread = ThreadSafety.IsDrawThread;
            ThreadSafety.IsDrawThread = true;
            var renderer = new VeldridRenderer();

            try
            {
                try
                {
                    renderer.InitialiseOffline(size, size);
                }
                catch (Exception e)
                {
                    Assert.Ignore($"Offline GPU device unavailable: {e.Message}");
                    return;
                }

                action(renderer);
            }
            finally
            {
                if (renderer.IsInitialised)
                    renderer.DisposeOffline();
                ThreadSafety.IsDrawThread = wasDrawThread;
            }
        }

        private static Rgba32[] render(VeldridRenderer renderer, bool mask)
        {
            using var shaders = new ShaderManager(renderer, new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), "Resources/Shaders"));
            var shader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            using var target = renderer.CreateFrameBuffer();
            target.Size = new Vector2(size);

            renderer.BeginFrame(new Vector2(size));
            target.Bind();
            renderer.PushViewport(new RectangleI(0, 0, size, size));
            renderer.PushDepthInfo(new DepthInfo(false, false));
            renderer.Clear(new ClearInfo(Color4.Black));
            renderer.SetBlend(BlendingParameters.None);
            shader.Bind();

            if (mask)
            {
                renderer.PushMaskingInfo(new MaskingInfo
                {
                    ScreenSpaceAABB = new RectangleI(16, 16, 32, 32),
                    MaskingRect = new RectangleF(16, 16, 32, 32),
                    ToMaskingSpace = Matrix3.Identity,
                    CornerExponent = 2,
                    BlendRange = 0,
                    AlphaExponent = 1,
                });
            }

            renderer.DrawQuad(renderer.WhitePixel, new Quad(0, 0, size, size),
                ColourInfo.SingleColour(mask ? Color4.White : Color4.Red));

            if (mask)
                renderer.PopMaskingInfo();

            shader.Unbind();
            renderer.PopDepthInfo();
            renderer.PopViewport();
            target.Unbind();
            renderer.FinishFrame();
            renderer.WaitUntilIdle();

            using var image = renderer.ExtractFrameBufferData(target);
            Assert.That(image, Is.Not.Null);

            var pixels = new Rgba32[image!.Width * image.Height];
            image.CopyPixelDataTo(pixels);
            return pixels;
        }

        private static int index(int x, int y) => y * size + x;

        private static bool hasVulkanIcd()
        {
            string? customIcd = Environment.GetEnvironmentVariable("VK_DRIVER_FILES") ?? Environment.GetEnvironmentVariable("VK_ICD_FILENAMES");

            if (!string.IsNullOrWhiteSpace(customIcd))
                return customIcd.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).All(File.Exists);

            string[] dirs = { "/etc/vulkan/icd.d", "/usr/local/share/vulkan/icd.d", "/usr/share/vulkan/icd.d" };
            return dirs.Any(d => Directory.Exists(d) && Directory.EnumerateFiles(d, "*.json").Any());
        }
    }
}
