// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.States;
using OpenTK.Graphics;

namespace osu.Framework.Graphics.UserInterface
{
    public class Breadcrumb : ClickableContainer, IHasCurrentValue<string>
    {
        public Bindable<string> Current { get; }

        protected SpriteText TextSprite;

        public Breadcrumb(string value)
        {
            Current = new Bindable<string>(value);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = CreateVisualRepresentation();

            Current.ValueChanged += value => TextSprite.Text = value;
        }

        protected virtual Drawable CreateVisualRepresentation()
        {
            return new Container
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,

                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Gray
                    },
                    TextSprite = new SpriteText
                    {
                        Origin = Anchor.CentreLeft,
                        Anchor = Anchor.CentreLeft,
                        Margin = new MarginPadding(3),
                        Text = Current.Value ?? string.Empty
                    }
                }
            };
        }

        protected override bool OnClick(InputState state)
        {
            Clicked?.Invoke();

            return base.OnClick(state);
        }


        public event Action Clicked;
    }
}
