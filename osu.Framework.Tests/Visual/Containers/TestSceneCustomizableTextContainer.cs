// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osuTK;

namespace osu.Framework.Tests.Visual.Containers
{
    public class TestSceneCustomizableTextContainer : FrameworkTestScene
    {
        [Resolved]
        private FrameworkConfigManager configManager { get; set; }

        [Test]
        public void TestLanguageSwitch()
        {
            CustomizableTextContainer container = null;

            AddStep("create container", () => Child = container = new CustomizableTextContainer
            {
                RelativeSizeAxes = Axes.Both
            });

            AddStep("add content to container", () =>
            {
                int first = container.AddPlaceholder(new SpriteIcon
                {
                    Size = new Vector2(16),
                    Icon = FontAwesome.Regular.Comment
                });

                int second = container.AddPlaceholder(new CircularContainer
                {
                    Size = new Vector2(30, 16),
                    Masking = true,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Colour4.Goldenrod
                    }
                });

                container.AddText(new RomanisableString(
                    $"this original [{first}] text has [{second}] placeholder",
                    $"this romanised text [{first}] has placeholder [{second}]"));
            });

            AddStep("prefer unicode", () => configManager.SetValue(FrameworkSetting.ShowUnicode, true));
            AddStep("prefer ASCII", () => configManager.SetValue(FrameworkSetting.ShowUnicode, false));
        }
    }
}
