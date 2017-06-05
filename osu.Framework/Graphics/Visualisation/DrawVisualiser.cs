// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;
using osu.Framework.Lists;
using System;
using System.Reflection;
using System.Collections.Generic;
using OpenTK.Graphics;
using OpenTK;

namespace osu.Framework.Graphics.Visualisation
{
    public class DrawVisualiser : OverlayContainer
    {
        internal readonly TreeContainer treeContainer;
        internal readonly PropertyDisplay propertyDisplay;

        internal VisualisedDrawable highlighted;

        private readonly InfoOverlay overlay;
        private ScheduledDelegate task;

        private readonly SortedList<VisualisedDrawable> hoveredDrawables =
            new SortedList<VisualisedDrawable>(VisualisedDrawable.Comparer);

        public DrawVisualiser()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                overlay = new InfoOverlay()
                {
                    Depth = float.MaxValue
                },
                propertyDisplay = new PropertyDisplay
                {
                    Depth = float.MinValue
                },
                treeContainer = new TreeContainer
                {
                    Depth = 0f,                         // Below property display
                    ChooseTarget = chooseTarget,
                    GoUpOneParent = delegate
                    {
                        Drawable lastHighlight = highlighted?.Target;

                        var parent = Target?.Parent;
                        if (parent?.Parent != null)
                            Target = (Drawable)Target?.Parent;

                        // Rehighlight the last highlight
                        if (lastHighlight != null)
                        {
                            VisualisedDrawable visualised = findVisualised(lastHighlight, targetDrawable);
                            if (visualised != null)
                                setHighlight(visualised);
                        }
                    },
                    ToggleProperties = delegate
                    {
                        if (targetDrawable == null)
                            return;

                        propertyDisplay.ToggleVisibility();

                        if (propertyDisplay.State == Visibility.Visible)
                            setHighlight(targetDrawable);
                    },
                },
                new CursorContainer()
            };

            propertyDisplay.StateChanged += (display, visibility) =>
            {
                switch (visibility)
                {
                    case Visibility.Hidden:
                        setHighlight(null);
                        break;
                }
            };
        }

        private VisualisedDrawable findVisualised(Drawable d, VisualisedDrawable root)
        {
            foreach (VisualisedDrawable child in root.Flow.InternalChildren)
            {
                if (child.Target == d)
                    return child;

                VisualisedDrawable found = findVisualised(d, child);
                if (found != null)
                    return found;
            }

            return null;
        }

        protected override bool BlockPassThroughMouse => false;

        protected override void PopIn()
        {
            FadeIn(100);
            if (Target == null)
                chooseTarget();
            else
                createRootVisualisedDrawable();

            task?.Cancel();
            task = Scheduler.AddDelayed(runUpdate, 200, true);
        }

        protected override void PopOut()
        {
            task?.Cancel();
            FadeOut(100);

            // Don't keep resources for visualizing the target
            // allocated; unbind callback events.
            removeRootVisualisedDrawable();
        }

        private bool targetSearching;

        private void chooseTarget()
        {
            setHighlight(null);
            propertyDisplay.State = Visibility.Hidden;

            Target = null;
            targetSearching = true;
        }

        private Drawable findTargetIn(Drawable d, InputState state)
        {
            if (d is DrawVisualiser) return null;
            if (d is CursorContainer) return null;
            if (d is PropertyDisplay) return null;

            if (!d.IsPresent) return null;

            var dAsContainer = d as IContainerEnumerable<Drawable>;

            Drawable containedTarget = null;

            if (dAsContainer != null)
            {
                if (!dAsContainer.InternalChildren.Any())
                    return null;

                foreach (var c in dAsContainer.InternalChildren)
                {
                    if (!c.IsAlive)
                        continue;

                    var contained = findTargetIn(c, state);
                    if (contained != null)
                    {
                        if (containedTarget == null ||
                            containedTarget.DrawWidth * containedTarget.DrawHeight > contained.DrawWidth * contained.DrawHeight)
                        {
                            containedTarget = contained;
                        }
                    }
                }
            }

            return containedTarget ?? (d.ScreenSpaceDrawQuad.Contains(state.Mouse.NativeState.Position) ? d : null);
        }

        private VisualisedDrawable targetDrawable;

        private void removeRootVisualisedDrawable()
        {
            if (targetDrawable != null)
            {
                treeContainer.Remove(targetDrawable);
                targetDrawable.Dispose();
                targetDrawable = null;
            }
        }

        private void createRootVisualisedDrawable()
        {
            removeRootVisualisedDrawable();
            setHighlight(null);

            if (target != null)
            {
                targetDrawable = createVisualisedDrawable(null, target as Drawable);
                treeContainer.Add(targetDrawable);

                runUpdate(); // run an initial update to immediately show the selected hierarchy.

                // Set highlight and update
                setHighlight(targetDrawable);
            }
        }

        private Drawable target;

        public Drawable Target
        {
            get { return target; }
            set
            {
                target = value;
                createRootVisualisedDrawable();
            }
        }

        private void runUpdate()
        {
            if (Target == null) return;

            visualise(Target, targetDrawable);
        }
        private void updatePropertyDisplay(Drawable d)
        {
            propertyDisplay.Clear(true);

            if (d == null)
                return;
            
            Type type = d.GetType();

            propertyDisplay.Add(
                ((IEnumerable<MemberInfo>)type.GetProperties(BindingFlags.Instance | BindingFlags.Public))      // Get all properties
                .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.Public))                            // And all fields
                .OrderBy(member => member.Name)
                .Select(member => new PropertyItem(member, d)));
        }

        private void updateHoveredDrawable()
        {
            overlay.Target = hoveredDrawables.Count > 0 ? hoveredDrawables.Last().Target : null;
        }

        private VisualisedDrawable createVisualisedDrawable(VisualisedDrawable parent, Drawable target)
        {
            var vis = new VisualisedDrawable(parent, target, this)
            {
                RequestTarget = delegate { Target = target; },
                HighlightTarget = setHighlight
            };

            vis.HoverGained = delegate
            {
                hoveredDrawables.Add(vis);
                updateHoveredDrawable();
            };

            vis.HoverLost = delegate
            {
                hoveredDrawables.Remove(vis);
                updateHoveredDrawable();
            };

            return vis;
        }
        private void setHighlight(VisualisedDrawable newHighlight)
        {
            highlighted?.highlightBackground.FadeOut();

            if (newHighlight == null)
            {
                updatePropertyDisplay(null);
                highlighted = null;
                return;
            }

            updatePropertyDisplay(newHighlight.Target);
            highlighted = newHighlight;

            if (propertyDisplay.State == Visibility.Visible)
            {
                newHighlight.highlightBackground.FadeIn();
                newHighlight.Expand();
            }
        }

        private void visualise(IDrawable d, VisualisedDrawable vis)
        {
            if (d == this) return;

            vis.CheckExpiry();

            foreach (var dd in vis.Flow.Children)
                if (!dd.CheckExpiry())
                    visualise(dd.Target, dd);

            var dContainer = d as IContainerEnumerable<Drawable>;

            if (d is SpriteText) return;

            if (dContainer == null) return;

            foreach (var c in dContainer.InternalChildren)
            {
                var dr = vis.Flow.Children.FirstOrDefault(x => x.Target == c);

                if (dr == null)
                {
                    var cLocal = c;
                    dr = createVisualisedDrawable(vis, cLocal);
                    vis.Flow.Add(dr);
                }

                visualise(c, dr);
            }
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            return targetSearching;
        }

        private Drawable findTarget(InputState state)
        {
            return findTargetIn(Parent?.Parent as Drawable, state);
        }

        protected override bool OnClick(InputState state)
        {
            if (targetSearching)
            {
                Target = (Drawable)findTarget(state)?.Parent;

                if (Target != null)
                {
                    targetSearching = false;
                    overlay.Target = null;
                    return true;
                }
            }

            return base.OnClick(state);
        }

        protected override bool OnMouseMove(InputState state)
        {
            if (targetSearching)
                overlay.Target = findTarget(state);

            return base.OnMouseMove(state);
        }
    }
}
