// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseNewSpriteText : TestCase
    {
        public TestCaseNewSpriteText()
        {
            var pairs = new List<Drawable[]>
            {
                new Drawable[] { new TestOldSpriteText { Text = "Old" }, new TestOldSpriteText { Text = "New" } },
                new Drawable[] { new TestOldSpriteText { Text = "Basic: Hello world!" }, new TestNewSpriteText { Text = "Basic: Hello world!" } },
                new Drawable[] { new TestOldSpriteText { Text = "Text size = 15", TextSize = 15 }, new TestNewSpriteText { Text = "Text size = 15", TextSize = 15 } },
                new Drawable[] { new TestOldSpriteText { Text = "Colour = green", Colour = Color4.Green }, new TestNewSpriteText { Text = "Colour = green", Colour = Color4.Green } },
                new Drawable[] { new TestOldSpriteText { Text = "Rotation = 45", Rotation = 45 }, new TestNewSpriteText { Text = "Rotation = 45", Rotation = 45 } },
                new Drawable[] { new TestOldSpriteText { Text = "Scale = 2", Scale = new Vector2(2) }, new TestNewSpriteText { Text = "Scale = 2", Scale = new Vector2(2) } },
                new Drawable[]
                {
                    new CircularContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        AutoSizeAxes = Axes.Both,
                        Child = new TestOldSpriteText { Text = "||MASKED||" }
                    },
                    new CircularContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        AutoSizeAxes = Axes.Both,
                        Child = new TestNewSpriteText { Text = "||MASKED||" }
                    }
                },
                new Drawable[] { new TestOldSpriteText { Text = "Explicit width", AutoSizeAxes = Axes.Y, Width = 50 }, new TestNewSpriteText { Text = "Explicit width", Width = 50 } },
                new Drawable[]
                {
                    new TestOldSpriteText { Text = "AllowMultiline = false", AutoSizeAxes = Axes.Y, Width = 50, AllowMultiline = false },
                    new TestNewSpriteText { Text = "AllowMultiline = false", Width = 50, AllowMultiline = false }
                },
                new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 50,
                        AutoSizeAxes = Axes.Y,
                        Child = new TestOldSpriteText { Text = "Relative size", AutoSizeAxes = Axes.Y, RelativeSizeAxes = Axes.X }
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 50,
                        AutoSizeAxes = Axes.Y,
                        Child = new TestNewSpriteText { Text = "Relative size", RelativeSizeAxes = Axes.X }
                    },
                },
                new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 50,
                        AutoSizeAxes = Axes.Y,
                        Child = new TestOldSpriteText { Text = "GlyphHeight = false", AutoSizeAxes = Axes.Y, RelativeSizeAxes = Axes.X, UseFullGlyphHeight = false }
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = 50,
                        AutoSizeAxes = Axes.Y,
                        Child = new TestNewSpriteText { Text = "GlyphHeight = false", RelativeSizeAxes = Axes.X, UseFullGlyphHeight = false }
                    },
                },
                new Drawable[] { new TestOldSpriteText { Text = "FixedWidth = true", FixedWidth = true }, new TestNewSpriteText { Text = "FixedWidth = true", FixedWidth = true } },
                new Drawable[] { new TestOldSpriteText { Text = "Scale = -1", Y = 20, Scale = new Vector2(-1) }, new TestNewSpriteText { Text = "Scale = -1", Y = 20, Scale = new Vector2(-1) } },
                new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both },
                            new TestOldSpriteText { Text = "Shadow = true", Shadow = true }
                        }
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both },
                            new TestNewSpriteText { Text = "Shadow = true", Shadow = true }
                        }
                    }
                },
                new Drawable[] { new TestOldSpriteText { Text = "Spacing = 5", Spacing = new Vector2(5) }, new TestNewSpriteText { Text = "Spacing = 5", Spacing = new Vector2(5) } },
                new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.SlateGray },
                            new TestOldSpriteText { Text = "Padded (autosize)", Padding = new MarginPadding(10) },
                        }
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.SlateGray },
                            new TestNewSpriteText { Text = "Padded (autosize)", Padding = new MarginPadding(10) },
                        }
                    }
                },
                new Drawable[]
                {
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.SlateGray },
                            new TestOldSpriteText { Text = "Padded (fixed size)", AutoSizeAxes = Axes.Y, Width = 50, Padding = new MarginPadding(10) },
                        }
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.SlateGray },
                            new TestNewSpriteText { Text = "Padded (fixed size)", Width = 50, Padding = new MarginPadding(10) },
                        }
                    }
                }
            };

            var rowDimensions = new List<Dimension>();
            for (int i = 0; i < pairs.Count; i++)
                rowDimensions.Add(new Dimension(GridSizeMode.AutoSize));

            Child = new AutoSizeGridContainer
            {
                AutoSizeAxes = Axes.Y,
                Content = pairs.ToArray(),
                RowDimensions = rowDimensions.ToArray(),
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 300),
                    new Dimension(GridSizeMode.Absolute, 300),
                }
            };
        }

        private class TestOldSpriteText : SpriteText
        {
            public TestOldSpriteText()
            {
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;
            }
        }

        private class TestNewSpriteText : NewSpriteText
        {
            public TestNewSpriteText()
            {
                Anchor = Anchor.TopCentre;
                Origin = Anchor.TopCentre;
            }
        }

        private class AutoSizeGridContainer : GridContainer
        {
            public new Axes AutoSizeAxes
            {
                get => base.AutoSizeAxes;
                set => base.AutoSizeAxes = value;
            }
        }
    }
}
