// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;
using System;

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// Displays Tooltips for all its children that inherit from the <see cref="IHasTooltip"/> or <see cref="IHasCustomTooltip"/> interfaces.
    /// </summary>
    public class TooltipContainer : CursorEffectContainer<TooltipContainer, IHasTooltip>
    {
        private readonly CursorContainer cursorContainer;
        private readonly ITooltip defaultTooltip;

        private ITooltip currentTooltip;

        private ScheduledDelegate findTooltipTask;
        private UserInputManager inputManager;

        /// <summary>
        /// Duration in milliseconds of still hovering until tooltips appear.
        /// </summary>
        protected virtual int AppearDelay => 220;

        private IHasTooltip currentlyDisplayed;

        /// <summary>
        /// Creates a new tooltip. Can be overridden to supply custom subclass of <see cref="Tooltip"/>.
        /// </summary>
        protected virtual ITooltip CreateTooltip() => new Tooltip();

        private readonly Container content;
        protected override Container<Drawable> Content => content;

        /// <summary>
        /// Creates a tooltip container where the tooltip is positioned at the bottom-right of
        /// the <see cref="CursorContainer.ActiveCursor"/> of the given <see cref="CursorContainer"/>.
        /// </summary>
        /// <param name="cursorContainer">The <see cref="CursorContainer"/> of which the <see cref="CursorContainer.ActiveCursor"/>
        /// shall be used for positioning. If null is provided, then a small offset from the current mouse position is used.</param>
        public TooltipContainer(CursorContainer cursorContainer = null)
        {
            this.cursorContainer = cursorContainer;
            AddInternal(content = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });
            AddInternal((Drawable)(currentTooltip = CreateTooltip()));
            defaultTooltip = currentTooltip;
        }

        protected override void OnSizingChanged()
        {
            base.OnSizingChanged();

            if (content != null)
            {
                // reset to none to prevent exceptions
                content.RelativeSizeAxes = Axes.None;
                content.AutoSizeAxes = Axes.None;

                // in addition to using this.RelativeSizeAxes, sets RelativeSizeAxes on every axis that is neither relative size nor auto size
                content.RelativeSizeAxes = Axes.Both & ~AutoSizeAxes;
                content.AutoSizeAxes = AutoSizeAxes;
            }
        }

        [BackgroundDependencyLoader]
        private void load(UserInputManager input)
        {
            inputManager = input;
        }

        private Vector2 computeTooltipPosition()
        {
            // Update the position of the displayed tooltip.
            // Our goal is to find the bounding circle of the cursor in screen-space, and to
            // position the top-left corner of the tooltip at the circle's southeast position.
            float boundingRadius;
            Vector2 cursorCentre;

            if (cursorContainer == null)
            {
                cursorCentre = ToLocalSpace(inputManager.CurrentState.Mouse.Position);
                boundingRadius = 14f;
            }
            else
            {
                Quad cursorQuad = cursorContainer.ActiveCursor.ToSpaceOfOtherDrawable(cursorContainer.ActiveCursor.DrawRectangle, this);
                cursorCentre = cursorQuad.Centre;
                // We only need to check 2 of the 4 vertices, because we only allow affine transformations
                // and the quad is therefore symmetric around the centre.
                boundingRadius = Math.Max(
                    (cursorQuad.TopLeft - cursorCentre).Length,
                    (cursorQuad.TopRight - cursorCentre).Length);
            }

            Vector2 southEast = new Vector2(1).Normalized();
            Vector2 tooltipPos = cursorCentre + southEast * boundingRadius;

            // Clamp position to tooltip container
            tooltipPos.X = Math.Min(tooltipPos.X, DrawWidth - currentTooltip.DrawSize.X - 5);
            float dX = Math.Max(0, tooltipPos.X - cursorCentre.X);
            float dY = (float)Math.Sqrt(boundingRadius * boundingRadius - dX * dX);

            if (tooltipPos.Y > DrawHeight - currentTooltip.DrawSize.Y - 5)
                tooltipPos.Y = cursorCentre.Y - dY - currentTooltip.DrawSize.Y;
            else
                tooltipPos.Y = cursorCentre.Y + dY;

            return tooltipPos;
        }

        /// <summary>
        /// Refreshes the displayed tooltip. By default, this <see cref="ITooltip.Move(Vector2)"/>s the tooltip to the cursor position, updates its <see cref="ITooltip.TooltipText"/> and calls its <see cref="ITooltip.Refresh"/> method.
        /// </summary>
        /// <param name="tooltip">The tooltip that is refreshed.</param>
        /// <param name="tooltipTarget">The target of the tooltip.</param>
        protected virtual void RefreshTooltip(ITooltip tooltip, IHasTooltip tooltipTarget)
        {
            if (tooltipTarget != null)
            {
                tooltip.TooltipText = tooltipTarget.TooltipText;
                tooltip.Refresh();
            }

            tooltip.Move(computeTooltipPosition());
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            RefreshTooltip(currentTooltip, currentlyDisplayed);

            if (currentlyDisplayed != null && ShallHideTooltip(currentlyDisplayed))
                hideTooltip();

        }

        protected override bool OnMouseMove(InputState state)
        {
            updateTooltipVisibility();
            return base.OnMouseMove(state);
        }

        private void hideTooltip()
        {
            currentTooltip.Hide();
            currentlyDisplayed = null;
        }

        /// <summary>
        /// Returns true if the currently visible tooltip should be hidden, false otherwise. By default, returns true if the target of the tooltip is neither hovered nor dragged.
        /// </summary>
        /// <param name="tooltipTarget">The target of the tooltip.</param>
        /// <returns>True if the currently visible tooltip should be hidden, false otherwise.</returns>
        protected virtual bool ShallHideTooltip(IHasTooltip tooltipTarget) => !tooltipTarget.IsHovered && !tooltipTarget.IsDragged;

        private void updateTooltipVisibility()
        {
            findTooltipTask?.Cancel();
            findTooltipTask = Scheduler.AddDelayed(delegate
            {
                IHasTooltip target = FindTarget();
                if (target != null)
                {
                    currentlyDisplayed = target;

                    RemoveInternal((Drawable)currentTooltip);
                    currentTooltip = getTooltip(target);
                    AddInternal((Drawable)currentTooltip);

                    currentTooltip.Show();
                }

            }, (1 - currentTooltip.Alpha) * AppearDelay);
        }

        private ITooltip getTooltip(IHasTooltip target) => (target as IHasCustomTooltip)?.GetCustomTooltip() ?? defaultTooltip;

        /// <summary>
        /// The default tooltip. Simply displays its text on a gray background and performs no easing.
        /// </summary>
        public class Tooltip : OverlayContainer, ITooltip
        {
            private readonly SpriteText text;

            /// <summary>
            /// The text to be displayed by this tooltip. This property is assigned to whenever the tooltip text changes.
            /// </summary>
            public virtual string TooltipText
            {
                set
                {
                    text.Text = value;
                }
            }

            public override bool HandleInput => false;

            private const float text_size = 16;

            /// <summary>
            /// Constructs a new tooltip that starts out invisible.
            /// </summary>
            public Tooltip()
            {
                Alpha = 0;
                AutoSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    new Box
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

            public virtual void Refresh() { }

            /// <summary>
            /// Called whenever the tooltip appears. When overriding do not forget to fade in.
            /// </summary>
            protected override void PopIn() => FadeIn();

            /// <summary>
            /// Called whenever the tooltip disappears. When overriding do not forget to fade out.
            /// </summary>
            protected override void PopOut() => FadeOut();

            /// <summary>
            /// Called whenever the position of the tooltip changes. Can be overridden to customize
            /// easing.
            /// </summary>
            /// <param name="pos">The new position of the tooltip.</param>
            public virtual void Move(Vector2 pos) => Position = pos;
        }
    }
}
