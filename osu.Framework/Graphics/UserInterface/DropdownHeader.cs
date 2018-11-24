// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osuTK.Graphics;
using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osuTK.Input;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class DropdownHeader : ClickableContainer, IKeyBindingHandler<PlatformAction>
    {
        public event Action<SelectionChange> ChangeSelection;

        protected Container Background;
        protected Container Foreground;

        private Color4 backgroundColour = Color4.DarkGray;

        protected Color4 BackgroundColour
        {
            get => backgroundColour;
            set
            {
                backgroundColour = value;
                Background.Colour = value;
            }
        }

        protected Color4 BackgroundColourHover { get; set; } = Color4.Gray;

        protected override Container<Drawable> Content => Foreground;

        protected internal abstract string Label { get; set; }

        protected DropdownHeader()
        {
            Masking = true;
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Width = 1;
            InternalChildren = new Drawable[]
            {
                Background = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.DarkGray,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                    },
                },
                Foreground = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                },
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            Background.Colour = BackgroundColourHover;
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            Background.Colour = BackgroundColour;
            base.OnHoverLost(e);
        }

        public override bool HandleNonPositionalInput => IsHovered;

        protected override bool Handle(UIEvent e)
        {
            switch (e)
            {
                case KeyDownEvent keyDown:
                    switch (keyDown.Key)
                    {
                        case Key.Up:
                            ChangeSelection?.Invoke(SelectionChange.Previous);
                            return true;
                        case Key.Down:
                            ChangeSelection?.Invoke(SelectionChange.Next);
                            return true;
                        default:
                            return base.Handle(e);
                    }
                default: return base.Handle(e);
            }
        }

        public bool OnPressed(PlatformAction action)
        {
            switch (action.ActionType)
            {
                case PlatformActionType.ListStart:
                    ChangeSelection?.Invoke(SelectionChange.First);
                    return true;
                case PlatformActionType.ListEnd:
                    ChangeSelection?.Invoke(SelectionChange.Last);
                    return true;
                default:
                    return false;
            }
        }

        public bool OnReleased(PlatformAction action) => false;

        public enum SelectionChange
        {
            Previous,
            Next,
            First,
            Last,
            FirstVisible,
            LastVisible
        }
    }
}
