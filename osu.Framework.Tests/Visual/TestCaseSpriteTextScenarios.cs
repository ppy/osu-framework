// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    [System.ComponentModel.Description("various visual SpriteText displays")]
    public class TestCaseSpriteTextScenarios : GridTestCase
    {
        public TestCaseSpriteTextScenarios()
            : base(4, 4)
        {
            Cell(0, 0).Child = new SpriteText { Text = "Basic: Hello world!" };

            Cell(1, 0).Child = new SpriteText
            {
                Text = "Text size = 15",
                TextSize = 15
            };

            Cell(2, 0).Child = new SpriteText
            {
                Text = "Colour = green",
                Colour = Color4.Green
            };

            Cell(3, 0).Child = new SpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = "Rotation = 45",
                Rotation = 45
            };

            Cell(0, 1).Child = new SpriteText
            {
                Text = "Scale = 2",
                Scale = new Vector2(2)
            };

            Cell(1, 1).Child = new CircularContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Masking = true,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both },
                    new SpriteText
                    {
                        Colour = Color4.Red,
                        Text = "||MASKED||"
                    }
                }
            };

            Cell(2, 1).Child = new SpriteText
            {
                Text = "Explicit width",
                Width = 50
            };

            Cell(3, 1).Child = new SpriteText
            {
                Text = "AllowMultiline = false",
                Width = 50,
                AllowMultiline = false
            };

            Cell(0, 2).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 50,
                AutoSizeAxes = Axes.Y,
                Child = new SpriteText
                {
                    Text = "Relative size",
                    RelativeSizeAxes = Axes.X
                }
            };

            Cell(1, 2).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 50,
                AutoSizeAxes = Axes.Y,
                Child = new SpriteText
                {
                    Text = "GlyphHeight = false",
                    RelativeSizeAxes = Axes.X,
                    UseFullGlyphHeight = false
                }
            };

            Cell(2, 2).Child = new SpriteText
            {
                Text = "FixedWidth = true",
                FixedWidth = true
            };

            Cell(3, 2).Child = new SpriteText
            {
                Text = "Scale = -1",
                Y = 20,
                Scale =new Vector2(-1)
            };

            Cell(0, 3).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both },
                    new SpriteText
                    {
                        Text = "Shadow = true",
                        Shadow = true
                    }
                }
            };

            Cell(1, 3).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.SlateGray
                    },
                    new SpriteText
                    {
                        Text = "Padded (autosize)",
                        Padding = new MarginPadding(10)
                    },
                }
            };

            Cell(2, 3).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.SlateGray
                    },
                    new SpriteText
                    {
                        Text = "Padded (fixed size)",
                        Width = 50,
                        Padding = new MarginPadding(10)
                    },
                }
            };

            Cell(3, 3).Child = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both },
                    new SpriteText
                    {
                        Text = "Red text + pink shadow",
                        Shadow = true,
                        Colour = Color4.Red,
                        ShadowColour = Color4.Pink.Opacity(0.5f)
                    }
                }
            };
        }
    }
}
