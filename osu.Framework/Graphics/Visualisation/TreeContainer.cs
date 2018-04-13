// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shapes;

namespace osu.Framework.Graphics.Visualisation
{
    internal enum TreeContainerStatus
    {
        Onscreen,
        Offscreen
    }

    internal class TreeContainer : Container, IStateful<TreeContainerStatus>
    {
        private readonly ScrollContainer scroll;

        private readonly SpriteText waitingText;

        public Action ChooseTarget;
        public Action GoUpOneParent;
        public Action ToggleProperties;

        protected override Container<Drawable> Content => scroll;

        private readonly Container titleBar;

        private const float width = 400;
        private const float height = 600;

        internal PropertyDisplay PropertyDisplay { get; private set; }

        private TreeContainerStatus state;

        public event Action<TreeContainerStatus> StateChanged;

        public TreeContainerStatus State
        {
            get { return state; }

            set
            {
                if (state == value)
                    return;
                state = value;

                switch (state)
                {
                    case TreeContainerStatus.Offscreen:
                        this.Delay(500).FadeTo(0.7f, 300);
                        break;
                    case TreeContainerStatus.Onscreen:
                        this.FadeIn(300, Easing.OutQuint);
                        break;
                }

                StateChanged?.Invoke(State);
            }
        }

        public TreeContainer()
        {
            Masking = true;
            CornerRadius = 5;
            Position = new Vector2(100, 100);

            AutoSizeAxes = Axes.X;
            Height = height;

            Color4 buttonBackground = new Color4(50, 50, 50, 255);
            Color4 buttonBackgroundHighlighted = new Color4(80, 80, 80, 255);
            const float button_width = width / 3 - 1;

            Button propertyButton;

            AddRangeInternal(new Drawable[]
            {
                new Box
                {
                    Colour = new Color4(15, 15, 15, 255),
                    RelativeSizeAxes = Axes.Both,
                    Depth = 0
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        titleBar = new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Size = new Vector2(1, 25),
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = Color4.BlueViolet,
                                },
                                new SpriteText
                                {
                                    Anchor = Anchor.Centre,
                                    Origin = Anchor.Centre,
                                    Text = "draw visualiser (Ctrl+F1 to toggle)",
                                    Alpha = 0.8f,
                                },
                            }
                        },
                        new Container //toolbar
                        {
                            RelativeSizeAxes = Axes.X,
                            Size = new Vector2(1, 40),
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = new Color4(20, 20, 20, 255),
                                    RelativeSizeAxes = Axes.Both,
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Spacing = new Vector2(1),
                                    Children = new Drawable[]
                                    {
                                        new Button
                                        {
                                            BackgroundColour = buttonBackground,
                                            Size = new Vector2(button_width, 1),
                                            RelativeSizeAxes = Axes.Y,
                                            Text = @"choose target",
                                            Action = delegate { ChooseTarget?.Invoke(); }
                                        },
                                        new Button
                                        {
                                            BackgroundColour = buttonBackground,
                                            Size = new Vector2(button_width, 1),
                                            RelativeSizeAxes = Axes.Y,
                                            Text = @"up one parent",
                                            Action = delegate { GoUpOneParent?.Invoke(); },
                                        },
                                        propertyButton = new Button
                                        {
                                            BackgroundColour = buttonBackground,
                                            Size = new Vector2(button_width, 1),
                                            RelativeSizeAxes = Axes.Y,
                                            Text = @"view properties",
                                            Action = delegate { ToggleProperties?.Invoke(); },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Y,
                    AutoSizeAxes = Axes.X,
                    Direction = FillDirection.Horizontal,
                    Padding = new MarginPadding { Top = 65 },
                    Children = new Drawable[]
                    {
                        scroll = new ScrollContainer
                        {
                            Padding = new MarginPadding(10),
                            RelativeSizeAxes = Axes.Y,
                            Width = width
                        },
                        PropertyDisplay = new PropertyDisplay()
                    }
                },
                waitingText = new SpriteText
                {
                    Text = @"Waiting for target selection...",
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                }
            });

            PropertyDisplay.StateChanged += v => propertyButton.BackgroundColour = v == Visibility.Visible ? buttonBackgroundHighlighted : buttonBackground;
        }

        protected override void Update()
        {
            waitingText.Alpha = scroll.Children.Any() ? 0 : 1;
            base.Update();
        }

        protected override bool OnHover(InputState state)
        {
            State = TreeContainerStatus.Onscreen;
            return true;
        }

        protected override void OnHoverLost(InputState state)
        {
            State = TreeContainerStatus.Offscreen;
            base.OnHoverLost(state);
        }

        protected override bool OnDragStart(InputState state) => titleBar.ReceiveMouseInputAt(state.Mouse.NativeState.Position);

        protected override bool OnDrag(InputState state)
        {
            Position += state.Mouse.Delta;
            return base.OnDrag(state);
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args) => true;

        protected override bool OnClick(InputState state) => true;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            State = TreeContainerStatus.Offscreen;
        }
    }
}
