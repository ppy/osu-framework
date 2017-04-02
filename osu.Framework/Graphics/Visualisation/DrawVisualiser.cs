// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Visualisation
{
    public class DrawVisualiser : OverlayContainer
    {
        private readonly TreeContainer treeContainer;

        private readonly InfoOverlay overlay;
        private ScheduledDelegate task;

        private readonly SortedList<VisualisedDrawable> hoveredDrawables =
            new SortedList<VisualisedDrawable>(VisualisedDrawable.Comparer);

        public DrawVisualiser()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                overlay = new InfoOverlay(),
                treeContainer = new TreeContainer
                {
                    Depth = float.MinValue,
                    ChooseTarget = chooseTarget,
                    GoUpOneParent = delegate
                    {
                        var parent = Target?.Parent;
                        if (parent?.Parent != null)
                            Target = Target?.Parent;
                    }
                },
                new CursorContainer()
            };
        }

        protected override bool BlockPassThroughInput => false;

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
            Target = null;
            targetSearching = true;
        }

        private Drawable findTargetIn(Drawable d, InputState state)
        {
            if (d is DrawVisualiser) return null;
            if (d is CursorContainer) return null;

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
            if (target != null)
            {
                targetDrawable = createVisualisedDrawable(null, target as Drawable);
                treeContainer.Add(targetDrawable);

                runUpdate(); // run an initial update to immediately show the selected hierarchy.
            }
        }

        private IDrawable target;

        public IDrawable Target
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

        private void updateHoveredDrawable()
        {
            overlay.Target = hoveredDrawables.Count > 0 ? hoveredDrawables.Last().Target : null;
        }

        private VisualisedDrawable createVisualisedDrawable(VisualisedDrawable parent, Drawable target)
        {
            var vis = new VisualisedDrawable(parent, target, treeContainer)
            {
                RequestTarget = delegate { Target = target; }
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
                Target = findTarget(state)?.Parent;

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
