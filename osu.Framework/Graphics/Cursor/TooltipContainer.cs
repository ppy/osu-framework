// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Cursor
{
    /// <summary>
    /// Displays Tooltips for all its children that inherit from the <see cref="IHasTooltip"/> or <see cref="IHasCustomTooltip"/> interfaces. Keep in mind that only children with <see cref="Drawable.HandleMouseInput"/> set to true will be checked for their tooltips.
    /// </summary>
    public class TooltipContainer : CursorEffectContainer<TooltipContainer, IHasTooltip>, IHandleGlobalInput
    {
        private readonly CursorContainer cursorContainer;
        private readonly ITooltip defaultTooltip;

        private ITooltip currentTooltip;

        private InputManager inputManager;

        /// <summary>
        /// Duration the cursor has to stay in a circular region of <see cref="AppearRadius"/>
        /// for the tooltip to appear.
        /// </summary>
        protected virtual double AppearDelay => 220;

        /// <summary>
        /// Radius of the circular region the cursor has to stay in for <see cref="AppearDelay"/>
        /// milliseconds for the tooltip to appear.
        /// </summary>
        protected virtual float AppearRadius => 20;

        private IHasTooltip currentlyDisplayed;

        /// <summary>
        /// Creates a new tooltip. Can be overridden to supply custom subclass of <see cref="Tooltip"/>.
        /// </summary>
        protected virtual ITooltip CreateTooltip() => new Tooltip();

        private bool hasValidTooltip(IHasTooltip target) => !string.IsNullOrEmpty(target?.TooltipText);

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

        protected override void LoadComplete()
        {
            base.LoadComplete();
            inputManager = GetContainingInputManager();
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

        private struct TimedPosition
        {
            public double Time;
            public Vector2 Position;
        }

        protected override void Update()
        {
            base.Update();

            IHasTooltip target = findTooltipTarget();
            if (target != null && target != currentlyDisplayed)
            {
                currentlyDisplayed = target;

                RemoveInternal((Drawable)currentTooltip);
                currentTooltip = getTooltip(target);
                AddInternal((Drawable)currentTooltip);

                currentTooltip.Show();
            }
        }

        private readonly List<TimedPosition> recentMousePositions = new List<TimedPosition>();
        private double lastRecordedPositionTime;

        private IHasTooltip lastCandidate;
        /// <summary>
        /// Determines which drawable should currently receive a tooltip, taking into account
        /// <see cref="AppearDelay"/> and <see cref="AppearRadius"/>. Returns null if no valid
        /// target is found.
        /// </summary>
        /// <returns>The tooltip target. null if no valid one is found.</returns>
        private IHasTooltip findTooltipTarget()
        {
            // While we are dragging a tooltipped drawable we should show a tooltip for it.
            if (inputManager.DraggedDrawable is IHasTooltip draggedTarget)
                return hasValidTooltip(draggedTarget) ? draggedTarget : null;

            IHasTooltip targetCandidate = FindTargets().FirstOrDefault(t => t.TooltipText != null);
            // check this first - if we find no target candidate we still want to clear the recorded positions and update the lastCandidate.
            if (targetCandidate != lastCandidate)
            {
                recentMousePositions.Clear();
                lastCandidate = targetCandidate;
            }
            if (targetCandidate == null)
                return null;

            double appearDelay = (targetCandidate as IHasAppearDelay)?.AppearDelay ?? AppearDelay;
            // Always keep 10 positions at equally-sized time intervals that add up to AppearDelay.
            double positionRecordInterval = appearDelay / 10;
            if (Time.Current - lastRecordedPositionTime >= positionRecordInterval)
            {
                lastRecordedPositionTime = Time.Current;
                recentMousePositions.Add(new TimedPosition
                {
                    Time = Time.Current,
                    Position = ToLocalSpace(inputManager.CurrentState.Mouse.Position)
                });
            }

            // check that we have recorded enough positions to make a judgement about whether or not the cursor has been standing still for the required amount of time.
            // we can skip this if the appear-delay is set to 0, since then tooltips can appear instantly and we don't need to wait to record enough positions.
            if (appearDelay > 0 && (recentMousePositions.Count == 0 || lastRecordedPositionTime - recentMousePositions[0].Time < appearDelay - positionRecordInterval))
                return null;
            recentMousePositions.RemoveAll(t => Time.Current - t.Time > appearDelay);

            // For determining whether to show a tooltip we first select only those positions
            // which happened within a shorter, alpha-adjusted appear delay.
            double alphaModifiedAppearDelay = (1 - currentTooltip.Alpha) * appearDelay;
            var relevantPositions = recentMousePositions.Where(t => Time.Current - t.Time <= alphaModifiedAppearDelay);

            // We then check whether all relevant positions fall within a radius of AppearRadius within the
            // first relevant position. If so, then the mouse has stayed within a small circular region of
            // AppearRadius for the duration of the modified appear delay, and we therefore want to display
            // the tooltip.
            Vector2 first = relevantPositions.FirstOrDefault().Position;
            float appearRadiusSq = AppearRadius * AppearRadius;

            if (relevantPositions.All(t => Vector2Extensions.DistanceSquared(t.Position, first) < appearRadiusSq))
                return targetCandidate;

            return null;
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
        protected virtual bool ShallHideTooltip(IHasTooltip tooltipTarget) => !hasValidTooltip(tooltipTarget) || !tooltipTarget.IsHovered && !tooltipTarget.IsDragged;

        private ITooltip getTooltip(IHasTooltip target) => (target as IHasCustomTooltip)?.GetCustomTooltip() ?? defaultTooltip;

        /// <summary>
        /// The default tooltip. Simply displays its text on a gray background and performs no easing.
        /// </summary>
        public class Tooltip : VisibilityContainer, ITooltip
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

            public override bool HandleKeyboardInput => false;
            public override bool HandleMouseInput => false;

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
            protected override void PopIn() => this.FadeIn();

            /// <summary>
            /// Called whenever the tooltip disappears. When overriding do not forget to fade out.
            /// </summary>
            protected override void PopOut() => this.FadeOut();

            /// <summary>
            /// Called whenever the position of the tooltip changes. Can be overridden to customize
            /// easing.
            /// </summary>
            /// <param name="pos">The new position of the tooltip.</param>
            public virtual void Move(Vector2 pos) => Position = pos;
        }
    }
}
