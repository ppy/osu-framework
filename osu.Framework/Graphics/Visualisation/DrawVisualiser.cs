// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Visualisation
{
    public class DrawVisualiser : OverlayContainer
    {
        private TreeContainer treeContainer;

        private InfoOverlay overlay;
        private ScheduledDelegate task;

        private List<Drawable> hoveredDrawables = new List<Drawable>();

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
                    GoUpOneParent = delegate { Target = Target?.Parent ?? Target; }
                },
                new CursorContainer()
            };
        }

        protected override void PopIn()
        {
            task = Scheduler.AddDelayed(runUpdate, 200, true);

            FadeIn(100);
            if (Target == null)
                chooseTarget();
        }

        protected override void PopOut()
        {
            task?.Cancel();
            FadeOut(100);
        }

        bool targetSearching;

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

        private IDrawable target;
        public IDrawable Target
        {
            get { return target; }
            set
            {
                if (targetDrawable != null)
                {
                    treeContainer.Remove(targetDrawable);
                    targetDrawable.Dispose();
                    targetDrawable = null;
                }

                target = value;

                if (target != null)
                {
                    targetDrawable = new VisualisedDrawable(target as Drawable);
                    treeContainer.Add(targetDrawable);
                }
            }
        }

        private void runUpdate()
        {
            if (Target == null) return;

            visualise(Target, targetDrawable);
        }

        private void visualise(IDrawable d, VisualisedDrawable vis)
        {
            if (d == this) return;

            vis.CheckExpiry();

            var drawables = vis.Flow.Children.Cast<VisualisedDrawable>();
            foreach (var dd in drawables)
            {
                if (!dd.CheckExpiry())
                    visualise(dd.Target, dd);
            }

            var dContainer = d as IContainerEnumerable<Drawable>;

            if (d is SpriteText) return;

            if (dContainer == null) return;

            foreach (var c in dContainer.InternalChildren)
            {
                var dr = drawables.FirstOrDefault(x => x.Target == c);

                if (dr == null)
                {
                    var cLocal = c;
                    dr = new VisualisedDrawable(cLocal)
                    {
                        HoverGained = delegate {
                            hoveredDrawables.Add(cLocal);
                            overlay.Target = cLocal;
                        },
                        HoverLost = delegate
                        {
                            hoveredDrawables.Remove(cLocal);
                            overlay.Target = (hoveredDrawables.Count > 0 ? hoveredDrawables.Last() : null);
                        },
                        RequestTarget = delegate { Target = cLocal; }
                    };
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
            return findTargetIn(Parent?.Parent?.Parent as Drawable, state);
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

        protected override void OnHoverLost(InputState state)
        {
            overlay.Target = null;
            base.OnHoverLost(state);
        }
    }
}
