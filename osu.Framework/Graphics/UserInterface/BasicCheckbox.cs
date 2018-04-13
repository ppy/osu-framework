// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.Color4Extensions;
using OpenTK;
using OpenTK.Graphics;
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
            get { return labelSpriteText?.Text; }
            set
            {
                if (labelSpriteText != null)
                    labelSpriteText.Text = value;
            }
        }

        public MarginPadding LabelPadding
        {
            get { return labelSpriteText?.Padding ?? new MarginPadding(); }
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
                    labelSpriteText = new SpriteText
                    {
                        Padding = new MarginPadding
                        {
                            Left = 10
                        },
                        Depth = float.MinValue
                    },
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
                    }
                }
            };

            Current.ValueChanged += c => box.FadeColour(c ? CheckedColor : UncheckedColor, FadeDuration);
            Current.TriggerChange();
        }
    }
}
