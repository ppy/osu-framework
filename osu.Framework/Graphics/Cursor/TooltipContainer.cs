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
using System.Collections.Generic;
using System.Linq;

namespace osu.Framework.Graphics.Cursor
{
    public class TooltipContainer : Container
    {
        private readonly CursorContainer cursorContainer;
        private readonly Tooltip tooltip;

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
        protected virtual Tooltip CreateTooltip() => new Tooltip();

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
            AddInternal(tooltip = CreateTooltip());
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
            tooltipPos.X = Math.Min(tooltipPos.X, DrawWidth - tooltip.DrawWidth - 5);
            float dX = Math.Max(0, tooltipPos.X - cursorCentre.X);
            float dY = (float)Math.Sqrt(boundingRadius * boundingRadius - dX * dX);

            if (tooltipPos.Y > DrawHeight - tooltip.DrawHeight - 5)
                tooltipPos.Y = cursorCentre.Y - dY - tooltip.DrawHeight;
            else
                tooltipPos.Y = cursorCentre.Y + dY;

            return tooltipPos;
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!tooltip.IsPresent)
                return;

            if (currentlyDisplayed != null)
                tooltip.TooltipText = currentlyDisplayed.TooltipText;

            tooltip.Move(computeTooltipPosition());
        }

        protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
        {
            updateTooltipVisibility(state);
            return base.OnMouseUp(state, args);
        }

        protected override bool OnMouseMove(InputState state)
        {
            updateTooltipVisibility(state);
            return base.OnMouseMove(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            if (!state.Mouse.HasMainButtonPressed)
                hideTooltip();
            base.OnHoverLost(state);
        }

        private void hideTooltip()
        {
            tooltip.Hide();
            currentlyDisplayed = null;
        }

        private readonly HashSet<IDrawable> childDrawables = new HashSet<IDrawable>();
        private readonly HashSet<IDrawable> nestedTtcChildDrawables = new HashSet<IDrawable>();
        private readonly List<IDrawable> newChildDrawables = new List<IDrawable>();
        private readonly List<IHasTooltip> tooltippedChildren = new List<IHasTooltip>();

        private void findTooltippedChildren()
        {
            // Skip all drawables in the hierarchy prior to (and including) ourself.
            var targetCandidates = inputManager.MouseInputQueue.Reverse().SkipWhile(d => d != this).Skip(1);

            // keep track of all hovered drawables below this and nested tooltip containers
            // so we can decide which are valid candidates for displaying a tooltip and so
            // we know when we can abort our search.
            childDrawables.Clear();
            childDrawables.Add(this);
            nestedTtcChildDrawables.Clear();
            tooltippedChildren.Clear();

            foreach (var candidate in targetCandidates)
            {
                // Children of drawables we are responsible for transitively also fall into our subtree,
                // and therefore we need to handle them. If they are not children of any drawables we handle,
                // it means that we iterated beyond our subtree and may terminate.
                IDrawable parent = candidate.Parent;

                // We keep track of all drawables we found while traversing the parent chain upwards.
                newChildDrawables.Clear();
                newChildDrawables.Add(candidate);
                // When we encounter a drawable we already encountered before, then there is no need
                // to keep going upward, since we already recorded it previously. At that point we know
                // the drawables we found are in fact children of ours.
                while (!childDrawables.Contains(parent))
                {
                    // If we reach to the root node (i.e. parent == null), then we found a drawable
                    // which is no longer a child of ours and we may terminate.
                    if (parent == null)
                        return;

                    newChildDrawables.Add(parent);
                    parent = parent.Parent;
                }

                // Assuming we did _not_ end up terminating, then all found drawables are children of ours
                // and need to be added.
                childDrawables.UnionWith(newChildDrawables);

                // Keep track of child drawables whose tooltips are managed by a nested tooltip container.
                // Note, that nested tooltip containers themselves could implement IHasTooltip and
                // are still our own responsibility to handle.
                nestedTtcChildDrawables.UnionWith(
                    ((IEnumerable<IDrawable>)newChildDrawables).Reverse()
                    .SkipWhile(d => d.Parent == this || !(d.Parent is TooltipContainer) && !nestedTtcChildDrawables.Contains(d.Parent)));

                // Ignore drawables whose tooltips are managed by a nested tooltip container.
                if (nestedTtcChildDrawables.Contains(candidate))
                    continue;

                IHasTooltip tooltipTarget = candidate as IHasTooltip;
                if (tooltipTarget != null && tooltipTarget.Hovering)
                    // We found a valid candidate; keep track of it
                    tooltippedChildren.Add(tooltipTarget);
            }
        }

        private void updateTooltipVisibility(InputState state)
        {
            // Nothing to do if we're still hovering a tooltipped drawable
            if (currentlyDisplayed?.Hovering == true)
                return;

            // Hide if we stopped hovering and do not have any button pressed.
            if (currentlyDisplayed != null && !state.Mouse.HasMainButtonPressed)
                hideTooltip();

            findTooltipTask?.Cancel();
            findTooltipTask = Scheduler.AddDelayed(delegate
            {
                findTooltippedChildren();

                // If we found any children with valid tooltips, pick the _last_ one as it
                // represents the front-most drawn one.
                if (tooltippedChildren.Count > 0)
                {
                    currentlyDisplayed = tooltippedChildren.Last();
                    tooltip.Show();
                }

            }, (1 - tooltip.Alpha) * AppearDelay);
        }

        public class Tooltip : OverlayContainer
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
