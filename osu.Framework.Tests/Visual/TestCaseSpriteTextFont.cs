// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseSpriteTextFont : TestCase
    {
        public TestCaseSpriteTextFont()
        {
            SpriteText sprite2;
            SpriteText sprite1;
            FillFlowContainer flow;

            Children = new Drawable[]
            {
                flow = new FillFlowContainer
                {
                    Anchor = Anchor.TopLeft,
                    AutoSizeAxes = Axes.Y,
                    RelativeSizeAxes = Axes.X,
                    Direction = FillDirection.Vertical,
                }
            };

            flow.Add(sprite1 = new SpriteText
            {
                Text = "THE QUICK RED FOX JUMPS OVER THE LAZY BROWN DOG",
                Font = new FontUsage(),
            });

            flow.Add(sprite2 = new SpriteText
            {
                Text = "the quick red fox jumps over the lazy brown dog",
                Font = new FontUsage(),
            });

            AddStep("weight regular", () =>
            {
                sprite1.Font.Weight = "Regular";
                sprite2.Font.Weight = "Regular";
            });

            AddStep("weight bold", () =>
            {
                sprite1.Font.Weight = "Bold";
                sprite2.Font.Weight = "Bold";
            });

            AddStep("regular italics", () =>
            {
                sprite1.Font.Weight = null;
                sprite2.Font.Weight = string.Empty;

                sprite1.Font.Italics = true;
                sprite2.Font.Italics = true;
            });

            AddStep("make bold", () =>
            {
                sprite1.Font.Weight = "Bold";
                sprite2.Font.Weight = "Bold";
            });

            AddStep("Clear font usage", () =>
            {
                sprite1.Font = new FontUsage();
                sprite2.Font = new FontUsage();
            });
        }
    }
}

