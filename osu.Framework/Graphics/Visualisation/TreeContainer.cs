// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
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
            AddInternal(waitingText = new SpriteText
            {
                Text = @"Waiting for target selection...",
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });

            AddButton(@"choose target", () => ChooseTarget?.Invoke());
            AddButton(@"up one parent", () => GoUpOneParent?.Invoke());
            AddButton(@"view properties", () => ToggleProperties?.Invoke());

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
