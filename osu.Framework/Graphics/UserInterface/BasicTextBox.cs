// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        protected virtual Color4 SelectionColour => FrameworkColour.YellowGreen;

        protected Color4 BackgroundCommit { get; set; } = FrameworkColour.Green;

        public BasicTextBox()
        {
            BackgroundFocused = FrameworkColour.BlueGreen;
            BackgroundUnfocused = FrameworkColour.BlueGreenDark;
            TextFlow.Height = 0.75f;
        }

        protected override void NotifyInputError() => Background.FlashColour(InputErrorColour, 200);

        protected override void Commit()
        {
            base.Commit();

            Background.Colour = ReleaseFocusOnCommit ? BackgroundUnfocused : BackgroundFocused;
            Background.ClearTransforms();
            Background.FlashColour(BackgroundCommit, 400);
        }

        protected override void OnFocusLost(FocusLostEvent e)
        {
            base.OnFocusLost(e);

            Background.ClearTransforms();
            Background.Colour = BackgroundFocused;
            Background.FadeColour(BackgroundUnfocused, 200, Easing.OutExpo);
        }

        protected override void OnFocus(FocusEvent e)
        {
            base.OnFocus(e);

            Background.ClearTransforms();
            Background.Colour = BackgroundUnfocused;
            Background.FadeColour(BackgroundFocused, 200, Easing.Out);
        }

        protected override Drawable GetDrawableCharacter(char c) => new SpriteText { Text = c.ToString(), Font = FrameworkFont.Condensed.With(size: CalculatedTextSize) };

        protected override SpriteText CreatePlaceholder() => new FadingPlaceholderText
        {
            Colour = FrameworkColour.YellowGreen,
            Font = FrameworkFont.Condensed,
            Anchor = Anchor.CentreLeft,
            Origin = Anchor.CentreLeft,
            X = CaretWidth,
        };

        public class FadingPlaceholderText : SpriteText
        {
            public override void Show() => this.FadeIn(200);

            public override void Hide() => this.FadeOut(200);
        }

        protected override DrawableCaret CreateCaret() => new BasicDrawableCaret
        {
            CaretWidth = CaretWidth,
            SelectionColour = SelectionColour,
        };

        public class BasicDrawableCaret : DrawableCaret
        {
            private const float caret_move_time = 60;

            public BasicDrawableCaret()
            {
                RelativeSizeAxes = Axes.Y;
                Size = new Vector2(1, 0.9f);
                Alpha = 0;
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

            public override float? SelectionWidth
            {
                set
                {
                    if (value != null)
                    {
                        this.ResizeWidthTo(value.Value + CaretWidth / 2, caret_move_time, Easing.Out);
                        this
                            .FadeTo(0.5f, 200, Easing.Out)
                            .FadeColour(SelectionColour, 200, Easing.Out);
                    }
                    else
                    {
                        this.ResizeWidthTo(CaretWidth, caret_move_time, Easing.Out);
                        this
                            .FadeColour(Color4.White, 200, Easing.Out)
                            .Loop(c => c.FadeTo(0.7f).FadeTo(0.4f, 500, Easing.InOutSine));
                    }
                }
            }

            public override Vector2 CursorPosition { set => this.MoveTo(new Vector2(value.X - CaretWidth / 2, value.Y), 60, Easing.Out); }
        }
    }
}
