// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;
using System.Linq;

namespace osu.Framework.Graphics.Cursor
{
    public class TooltipContainer : Container
    {
        private readonly CursorContainer cursor;
        private readonly Tooltip tooltip;

        private ScheduledDelegate findTooltipTask;
        private UserInputManager inputManager;

        protected virtual int AppearDelay => 220;

        private IHasTooltip currentlyDisplayed;

        protected virtual Tooltip CreateTooltip() => new Tooltip();

        public TooltipContainer(CursorContainer cursor)
        {
            this.cursor = cursor;
            AlwaysPresent = true;
            RelativeSizeAxes = Axes.Both;
            Add(tooltip = CreateTooltip());
        }

        [BackgroundDependencyLoader]
        private void load(UserInputManager input)
        {
            inputManager = input;
        }

        protected override void Update()
        {
            if (tooltip.IsPresent)
            {
                if (currentlyDisplayed != null)
                    tooltip.TooltipText = currentlyDisplayed.TooltipText;

                //update the position of the displayed tooltip.
                tooltip.Position = ToLocalSpace(cursor.ActiveCursor.ScreenSpaceDrawQuad.Centre) + new Vector2(10);
            }
        }

        protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
        {
            updateTooltipState(state);
            return base.OnMouseUp(state, args);
        }

        protected override bool OnMouseMove(InputState state)
        {
            updateTooltipState(state);
            return base.OnMouseMove(state);
        }

        private void updateTooltipState(InputState state)
        {
            if (currentlyDisplayed?.Hovering != true)
            {
                if (currentlyDisplayed != null && !state.Mouse.HasMainButtonPressed)
                {
                    tooltip.Hide();
                    currentlyDisplayed = null;
                }

                findTooltipTask?.Cancel();
                findTooltipTask = Scheduler.AddDelayed(delegate
                {
                    var tooltipTarget = inputManager.HoveredDrawables.OfType<IHasTooltip>().FirstOrDefault();

                    if (tooltipTarget == null) return;

                    tooltip.TooltipText = tooltipTarget.TooltipText;
                    tooltip.Show();

                    currentlyDisplayed = tooltipTarget;
                }, (1 - tooltip.Alpha) * AppearDelay);
            }
        }

        public class Tooltip : OverlayContainer
        {
            private readonly Box background;
            private readonly SpriteText text;

            public virtual string TooltipText
            {
                set
                {
                    if (value == text.Text) return;

                    text.Text = value;
                }
            }

            public override bool HandleInput => false;

            private const float text_size = 16;

            public Tooltip()
            {
                Alpha = 0;
                AutoSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Gray,
                    },
                    text = new SpriteText
                    {
                        TextSize = text_size,
                        Padding = new MarginPadding(5),
                    }
                };
            }

            protected override void PopIn() => FadeIn();

            protected override void PopOut() => FadeOut();
        }
    }
}
