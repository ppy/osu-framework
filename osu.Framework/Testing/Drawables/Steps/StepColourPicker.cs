// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;

namespace osu.Framework.Testing.Drawables.Steps
{
    public partial class StepColourPicker : CompositeDrawable
    {
        private const float scale_adjust = 0.5f;

        public Action<Colour4>? ValueChanged { get; set; }

        private readonly Bindable<Colour4> current;

        public StepColourPicker(string description, Colour4 initialColour)
        {
            current = new Bindable<Colour4>(initialColour);

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = FrameworkColour.GreenDark,
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        new SpriteText
                        {
                            Text = description,
                            Padding = new MarginPadding(5),
                            Font = FrameworkFont.Regular.With(size: 14),
                        },
                        new BasicColourPicker
                        {
                            RelativeSizeAxes = Axes.X,
                            Width = 1f / scale_adjust,
                            Scale = new Vector2(scale_adjust),
                            Current = current,
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            current.BindValueChanged(v => ValueChanged?.Invoke(v.NewValue), true);
        }
    }
}
