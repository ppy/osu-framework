// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Visualisation
{
    internal class TreeContainer : ToolWindow
    {
        private readonly SpriteText waitingText;

        private readonly SearchContainer searchContainer;

        private BasicTextBox queryTextBox;

        public Action ChooseTarget;
        public Action GoUpOneParent;
        public Action ToggleInspector;

        internal DrawableInspector DrawableInspector { get; private set; }

        [Resolved]
        private DrawVisualiser visualiser { get; set; }

        public VisualisedDrawable Target
        {
            set
            {
                if (value == null)
                    searchContainer.Clear(false);
                else
                    searchContainer.Child = value;
            }
        }

        public TreeContainer()
            : base("Draw Visualiser", "(Ctrl+F1 to toggle)")
        {
            AddInternal(waitingText = new SpriteText
            {
                Text = @"Waiting for target selection...",
                Font = FrameworkFont.Regular,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            AddButton(@"choose target", () => ChooseTarget?.Invoke());
            AddButton(@"up one parent", () => GoUpOneParent?.Invoke());
            AddButton(@"toggle inspector", () => ToggleInspector?.Invoke());

            MainHorizontalContent.Add(DrawableInspector = new DrawableInspector());

            ScrollContent.Child = searchContainer = new SearchContainer
            {
                AutoSizeAxes = Axes.Y,
                Width = WIDTH
            };
        }

        protected override Drawable WrapScrollContent(Drawable scrollContent)
            => new GridContainer
            {
                RelativeSizeAxes = Axes.Y,
                Width = WIDTH,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(GridSizeMode.Distributed)
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        queryTextBox = new BasicTextBox
                        {
                            Width = WIDTH,
                            Height = 30,
                            PlaceholderText = "Search..."
                        }
                    },
                    new[]
                    {
                        scrollContent
                    }
                }
            };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            queryTextBox.Current.BindValueChanged(term => searchContainer.SearchTerm = term.NewValue, true);
        }

        protected override void Update()
        {
            waitingText.Alpha = visualiser.Searching ? 1 : 0;
            base.Update();
        }

        protected override bool OnClick(ClickEvent e) => true;
    }
}
