// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK.Graphics;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using System;
using osu.Framework.Input;
using osu.Framework.Extensions.Color4Extensions;

namespace osu.Framework.Testing.Drawables.Steps
{
    public class StepSlider<T> : SliderBar<T>
        where T : struct, IComparable, IConvertible
    {
        private readonly Box selection;
        private readonly Box background;
        private readonly SpriteText spriteText;

        private readonly string text;

        public Action<T> ValueChanged;

        public StepSlider(string description, T min, T max, T start)
        {
            text = description;

            // Styling
            Height = 25;
            RelativeSizeAxes = Axes.X;

            AddRangeInternal(new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.RoyalBlue.Darken(0.75f),
                },
                selection = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.RoyalBlue,
                },
                spriteText = new SpriteText
                {
                    Depth = -1,
                    Origin = Anchor.CentreLeft,
                    Anchor = Anchor.CentreLeft,
                },
            });

            CornerRadius = 2;
            Masking = true;

            spriteText.Anchor = Anchor.CentreLeft;
            spriteText.Origin = Anchor.CentreLeft;
            spriteText.Padding = new MarginPadding(5);

            // Bind to the underlying sliderbar
            var currentNumber = (BindableNumber<T>)Current;
            currentNumber.MinValue = min;
            currentNumber.MaxValue = max;
            currentNumber.Default = start;
            currentNumber.SetDefault();
        }

        protected override bool OnDragEnd(InputState state)
        {
            var flash = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = Color4.RoyalBlue,
                Blending = BlendingMode.Additive,
                Alpha = 0.6f,
            };

            Add(flash);
            flash.FadeOut(200).Expire();

            Success();
            return base.OnDragEnd(state);
        }

        protected override void UpdateValue(float normalizedValue)
        {
            var value = Current.Value;

            ValueChanged?.Invoke(value);
            spriteText.Text = $"{text}: {Convert.ToDouble(value):G3}";
            selection.ResizeWidthTo(normalizedValue);
        }

        protected void Success()
        {
            background.Alpha = 0.4f;
            selection.Alpha = 0.4f;
            spriteText.Alpha = 0.8f;
        }

        public override string ToString() => spriteText.Text;
    }
}
