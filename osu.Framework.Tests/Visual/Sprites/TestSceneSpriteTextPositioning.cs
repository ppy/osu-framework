// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.IO.Stores;
using osu.Framework.Testing;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.Sprites
{
    [System.ComponentModel.Description("Color backed characters to visualize size and spacing")]
    public class TestSceneSpriteTextPositioning : GridTestScene
    {
        public override IReadOnlyList<Type> RequiredTypes => new[]
        {
            typeof(GlyphStore),
        };

        private readonly List<Drawable> textDrawables = new List<Drawable>();

        public TestSceneSpriteTextPositioning()
            : base(7, 7)
        {
            textDrawables.Add(Cell(0, 0).Child = new ColorBackedContainer("Time"));
            textDrawables.Add(Cell(1, 0).Child = new ColorBackedContainer("T"));
            textDrawables.Add(Cell(1, 1).Child = new ColorBackedContainer("i"));
            textDrawables.Add(Cell(1, 2).Child = new ColorBackedContainer("m"));
            textDrawables.Add(Cell(1, 3).Child = new ColorBackedContainer("e"));

            AddToggleStep("Toggle fixed width", b => textDrawables.ForEach(d => (d as ColorBackedContainer)?.ToggleFixedWidth(b)));
        }

        public class ColorBackedContainer : Container
        {
            private readonly SpriteText spriteText;

            public ColorBackedContainer(string text)
            {
                Children = new Drawable[]
                {
                    new Box
                    {
                        Colour = Color4.Crimson,
                        RelativeSizeAxes = Axes.Both
                    },
                    spriteText = new SpriteText
                    {
                        Text = text,
                        Font = new FontUsage(fixedWidth: false, size: 80),
                    }
                };
                AutoSizeAxes = Axes.Both;
            }

            public void ToggleFixedWidth(bool fixedWidth) => spriteText.Font = new FontUsage(fixedWidth: fixedWidth, size: 80);
        }
    }
}
