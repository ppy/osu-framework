// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Visualisation
{
    internal class TreeContainer : Container
    {
        private readonly ScrollContainer<Drawable> scroll;

        private readonly SpriteText waitingText;

        public Action ChooseTarget;
        public Action GoUpOneParent;
        public Action ToggleProperties;

        protected override Container<Drawable> Content => scroll;

        private const float width = 500;
        private const float height = 600;

        internal PropertyDisplay PropertyDisplay { get; private set; }

        [Resolved]
        private DrawVisualiser visualiser { get; set; }

        public TreeContainer()
        {
            Position = new Vector2(100, 100);

            AutoSizeAxes = Axes.X;
            Height = height;

            const float button_width = 140;

            AddRangeInternal(new Drawable[]
            {
                new Box
                {
                    Colour = FrameworkColour.GreenDark,
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
                        new TitleBar(this),
                        new Container //toolbar
                        {
                            RelativeSizeAxes = Axes.X,
                            Size = new Vector2(1, 40),
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    Colour = FrameworkColour.BlueGreenDark,
                                    RelativeSizeAxes = Axes.Both,
                                },
                                new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Spacing = new Vector2(5),
                                    Padding = new MarginPadding(5),
                                    Children = new Drawable[]
                                    {
                                        new Button
                                        {
                                            Size = new Vector2(button_width, 1),
                                            RelativeSizeAxes = Axes.Y,
                                            Text = @"choose target",
                                            Action = delegate { ChooseTarget?.Invoke(); }
                                        },
                                        new Button
                                        {
                                            Size = new Vector2(button_width, 1),
                                            RelativeSizeAxes = Axes.Y,
                                            Text = @"up one parent",
                                            Action = delegate { GoUpOneParent?.Invoke(); },
                                        },
                                        new Button
                                        {
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
                    Padding = new MarginPadding { Top = 80 },
                    Children = new Drawable[]
                    {
                        scroll = new BasicScrollContainer<Drawable>
                        {
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
        }

        protected override void Update()
        {
            waitingText.Alpha = visualiser.Searching ? 1 : 0;
            base.Update();
        }

        protected override bool OnClick(ClickEvent e) => true;
    }
}
