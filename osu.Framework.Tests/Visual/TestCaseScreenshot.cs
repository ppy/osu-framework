// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Platform;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseScreenshot : TestCase
    {
        [Resolved]
        private GameHost host { get; set; }

        private Sprite display;

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.5f),
                Masking = true,
                BorderColour = Color4.Green,
                BorderThickness = 2,
                Child = display = new Sprite { RelativeSizeAxes = Axes.Both }
            };

            AddStep("take screenshot", takeScreenshot);
        }

        private void takeScreenshot()
        {
            if (host.Window == null)
                return;

            host.TakeScreenshotAsync().ContinueWith(t => Schedule(() =>
            {
                var image = t.Result;

                var tex = new Texture(image.Width, image.Height);
                tex.SetData(new TextureUpload(image));

                display.Texture = tex;
            }));
        }
    }
}
