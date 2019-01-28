﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicCheckbox : Checkbox
    {
        public Color4 CheckedColor { get; set; } = Color4.White;
        public Color4 UncheckedColor { get; set; } = Color4.White.Opacity(0.2f);

        public int FadeDuration { get; set; } = 50;

        public string LabelText
        {
            get => labelSpriteText?.Text;
            set
            {
                if (labelSpriteText != null)
                    labelSpriteText.Text = value;
            }
        }

        public MarginPadding LabelPadding
        {
            get => labelSpriteText?.Padding ?? new MarginPadding();
            set
            {
                if (labelSpriteText != null)
                    labelSpriteText.Padding = value;
            }
        }

        private readonly SpriteText labelSpriteText;

        public BasicCheckbox()
        {
            Box box;

            AutoSizeAxes = Axes.Both;

            Child = new FillFlowContainer
            {
                Direction = FillDirection.Horizontal,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Container
                    {
                        BorderColour= Color4.White,
                        BorderThickness = 3,
                        Masking = true,
                        Size = new Vector2(20, 20),
                        Child = box = new Box
                        {
                            RelativeSizeAxes = Axes.Both
                        }
                    },
                    labelSpriteText = new SpriteText
                    {
                        Padding = new MarginPadding
                        {
                            Left = 10
                        },
                        Depth = float.MinValue
                    },
                }
            };

            Current.ValueChanged += c => box.FadeColour(c ? CheckedColor : UncheckedColor, FadeDuration);
            Current.TriggerChange();
        }
    }
}
