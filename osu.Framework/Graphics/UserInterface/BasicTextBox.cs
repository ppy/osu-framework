// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class BasicTextBox : TextBox
    {
        protected virtual float CaretWidth => 2;

        private const float caret_move_time = 60;

        protected virtual Color4 SelectionColour => FrameworkColour.YellowGreen;

        protected Color4 BackgroundCommit { get; set; } = FrameworkColour.Green;

        private Color4 backgroundFocused = new Color4(100, 100, 100, 255);
        private Color4 backgroundUnfocused = new Color4(100, 100, 100, 120);

        private readonly Box background;

        protected Color4 BackgroundFocused
        {
            get => backgroundFocused;
            set
            {
                backgroundFocused = value;
                if (HasFocus)
                    background.Colour = value;
            }
        }

        protected Color4 BackgroundUnfocused
        {
            get => backgroundUnfocused;
            set
            {
                backgroundUnfocused = value;
                if (!HasFocus)
                    background.Colour = value;
            }
        }

        protected virtual Color4 InputErrorColour => Color4.Red;

        public BasicTextBox()
        {
            Add(background = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Depth = 1,
                Colour = BackgroundUnfocused,
            });

            BackgroundFocused = FrameworkColour.BlueGreen;
            BackgroundUnfocused = FrameworkColour.BlueGreenDark;
            TextContainer.Height = 0.75f;
        }

        protected override void NotifyInputError() => background.FlashColour(InputErrorColour, 200);

        protected override void OnTextCommitted(bool textChanged)
        {
            base.OnTextCommitted(textChanged);

            background.Colour = ReleaseFocusOnCommit ? BackgroundUnfocused : BackgroundFocused;
            background.ClearTransforms();
            background.FlashColour(BackgroundCommit, 400);
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            base.OnFocusLost(e);

            background.ClearTransforms();
            background.Colour = BackgroundFocused;
            background.FadeColour(BackgroundUnfocused, 200, Easing.OutExpo);
        }

        protected override void OnFocus(FocusEvent e)
        {
            base.OnFocus(e);

            background.ClearTransforms();
            background.Colour = BackgroundUnfocused;
            background.FadeColour(BackgroundFocused, 200, Easing.Out);
        }

        protected override Drawable GetDrawableCharacter(char c) => new FallingDownContainer
        {
            AutoSizeAxes = Axes.Both,
            Child = new SpriteText { Text = c.ToString(), Font = FrameworkFont.Condensed.With(size: CalculatedTextSize) }
        };

        protected override SpriteText CreatePlaceholder() => new FadingPlaceholderText
        {
            Colour = FrameworkColour.YellowGreen,
            Font = FrameworkFont.Condensed,
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            X = CaretWidth,
        };

        public class FallingDownContainer : Container
        {
            public override void Show()
            {
                var col = (Color4)Colour;
                this.FadeColour(col.Opacity(0)).FadeColour(col, caret_move_time * 2, Easing.Out);
            }

            public override void Hide()
            {
                this.FadeOut(200);
                this.MoveToY(DrawSize.Y, 200, Easing.InExpo);
            }
        }

        public class FadingPlaceholderText : SpriteText
        {
            public override void Show() => this.FadeIn(200);

            public override void Hide() => this.FadeOut(200);
        }

        protected override Caret CreateCaret() => new BasicCaret
        {
            CaretWidth = CaretWidth,
            SelectionColour = SelectionColour,
        };

        public class BasicCaret : Caret
        {
            public BasicCaret()
            {
                RelativeSizeAxes = Axes.Y;
                Size = new Vector2(1, 0.9f);

                Colour = Color4.Transparent;
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;

                Masking = true;
                CornerRadius = 1;

                InternalChild = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.White,
                };
            }

            public override void Hide() => this.FadeOut(200);

            public float CaretWidth { get; set; }

            public Color4 SelectionColour { get; set; }

            public override void DisplayAt(Vector2 position, float? selectionWidth)
            {
                if (selectionWidth != null)
                {
                    this.MoveTo(new Vector2(position.X, position.Y), 60, Easing.Out);
                    this.ResizeWidthTo(selectionWidth.Value + CaretWidth / 2, caret_move_time, Easing.Out);
                    this
                        .FadeTo(0.5f, 200, Easing.Out)
                        .FadeColour(SelectionColour, 200, Easing.Out);
                }
                else
                {
                    this.MoveTo(new Vector2(position.X - CaretWidth / 2, position.Y), 60, Easing.Out);
                    this.ResizeWidthTo(CaretWidth, caret_move_time, Easing.Out);
                    this
                        .FadeColour(Color4.White, 200, Easing.Out)
                        .Loop(c => c.FadeTo(0.7f).FadeTo(0.4f, 500, Easing.InOutSine));
                }
            }
        }
    }
}
