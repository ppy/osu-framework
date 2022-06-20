// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

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
        public Action ToggleInspector;

        internal DrawableInspector DrawableInspector { get; private set; }

        [Resolved]
        private DrawVisualiser visualiser { get; set; }

        public VisualisedDrawable Target
        {
            set
            {
                if (value == null)
                    SearchContainer.Clear(false);
                else
                    SearchContainer.Child = value;
            }
        }

        public TreeContainer()
            : base("Draw Visualiser", "(Ctrl+F1 to toggle)", true)
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
        }

        protected override void Update()
        {
            waitingText.Alpha = visualiser.Searching ? 1 : 0;
            base.Update();
        }

        protected override bool OnClick(ClickEvent e) => true;
    }
}
