// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Visualisation
{
    internal class TreeContainer : ToolWindow
    {
        private readonly SpriteText waitingText;

        public Action ChooseTarget;
        public Action GoUpOneParent;
        public Action ToggleProperties;

        internal PropertyDisplay PropertyDisplay { get; private set; }

        [Resolved]
        private DrawVisualiser visualiser { get; set; }

        public VisualisedDrawable Target
        {
            set
            {
                if (value == null)
                    ScrollContent.Clear(false);
                else
                    ScrollContent.Child = value;
            }
        }

        public TreeContainer()
            : base("Draw Visualiser", "(Ctrl+F1 to toggle)")
        {
            const float button_width = 140;
            const float button_height = 40;

            AddInternal(waitingText = new SpriteText
            {
                Text = @"Waiting for target selection...",
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            ToolbarContent.Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(5),
                Padding = new MarginPadding(5),
                Children = new Drawable[]
                {
                    new Button
                    {
                        Size = new Vector2(button_width, button_height),
                        Text = @"choose target",
                        Action = delegate { ChooseTarget?.Invoke(); }
                    },
                    new Button
                    {
                        Size = new Vector2(button_width, button_height),
                        Text = @"up one parent",
                        Action = delegate { GoUpOneParent?.Invoke(); },
                    },
                    new Button
                    {
                        Size = new Vector2(button_width, button_height),
                        Text = @"view properties",
                        Action = delegate { ToggleProperties?.Invoke(); },
                    },
                },
            });

            MainHorizontalContent.Add(PropertyDisplay = new PropertyDisplay());
        }

        protected override void Update()
        {
            waitingText.Alpha = visualiser.Searching ? 1 : 0;
            base.Update();
        }

        protected override bool OnClick(ClickEvent e) => true;
    }
}
