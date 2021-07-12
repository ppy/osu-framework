// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;

namespace osu.Framework.Tests.Visual.Sprites
{
    public class TestSceneSpriteTextTextTransforms : TestScene
    {
        private readonly BasicDropdown<TextTransform> textTransformDropdown;

        private SpriteText sprite;

        public TestSceneSpriteTextTextTransforms()
        {
            Child = new FillFlowContainer()
            {
                Direction = FillDirection.Vertical,
                Spacing = new osuTK.Vector2(0, 25),
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = "text is displayed without any case transforms (lowercase)",
                        TextTransform = TextTransform.None,
                    },
                    new SpriteText
                    {
                        Text = "text is displayed in uppercase",
                        TextTransform = TextTransform.Uppercase,
                    },
                    new SpriteText
                    {
                        Text = "text is displayed in capitalized case aka title case",
                        TextTransform = TextTransform.Capitalize,
                    },
                    new SpriteText
                    {
                        Text = "text is displayed in lowercase",
                        TextTransform = TextTransform.Lowercase,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new osuTK.Vector2(15, 0),
                        Children = new Drawable[]
                        {
                            textTransformDropdown = new BasicDropdown<TextTransform>
                            {
                                Width = 200,
                                Items = (TextTransform[])Enum.GetValues(typeof(TextTransform))
                            },
                            sprite = new SpriteText
                            {
                                Text = "The quick brown fox jumps over the lazy dog"
                            }
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            textTransformDropdown.Current.BindValueChanged(e => sprite.TextTransform = e.NewValue);
            base.LoadComplete();
        }
    }
}
