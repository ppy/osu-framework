// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Development;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Framework.Tests.Visual.Platform
{
    public partial class TestSceneClipboard : FrameworkTestScene
    {
        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        [Resolved]
        private GameHost host { get; set; } = null!;

        [Resolved]
        private Clipboard clipboard { get; set; } = null!;

        private Image<Rgba32>? originalImage;
        private Image<Rgba32>? clipboardImage;

        // empty text doesn't really matter, since it doesn't actually make sense (text editors generally don't allow copying nothing)
        // it's here to just show how it behaves
        [TestCase("")]
        [TestCase("hello!")]
        public void TestText(string text)
        {
            AddStep("set clipboard text", () => clipboard.SetText(text));
            AddAssert("clipboard text is expected", () => clipboard.GetText(), () => Is.EqualTo(text));
            AddAssert("clipboard image is null", () => clipboard.GetImage<Rgba32>(), () => Is.Null);
        }

        [Test]
        public void TestImage()
        {
            if (DebugUtils.IsNUnitRunning)
                Assert.Ignore("This test cannot run in headless mode (a window instance is required).");

            AddStep("clear previous screenshots", Clear);

            AddStep("screenshot screen", () =>
            {
                host.TakeScreenshotAsync().ContinueWith(t =>
                {
                    var image = t.GetResultSafely();
                    originalImage = image.Clone();
                    image.Dispose();
                });
            });

            AddUntilStep("screenshot taken", () => originalImage != null);

            AddStep("copy image to clipboard", () =>
            {
                clipboard.SetImage(originalImage!);
            });

            AddAssert("clipboard text is null", () => clipboard.GetText(), () => Is.Null);

            AddStep("retrieve image from clipboard", () =>
            {
                var image = clipboard.GetImage<Rgba32>();
                clipboardImage = image!.Clone();

                var texture = renderer.CreateTexture(image.Width, image.Height);
                texture.SetData(new TextureUpload(image));

                Child = new Sprite
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fit,
                    Texture = texture
                };
            });

            AddAssert("image retrieved", () => clipboardImage != null);

            AddAssert("compare images", () =>
            {
                if (originalImage!.Width != clipboardImage!.Width || originalImage.Height != clipboardImage.Height)
                    return false;

                for (int x = 0; x < originalImage.Width; x++)
                {
                    for (int y = 0; y < originalImage.Height; y++)
                    {
                        if (originalImage[x, y] != clipboardImage[x, y])
                            return false;
                    }
                }

                return true;
            });
        }
    }
}
