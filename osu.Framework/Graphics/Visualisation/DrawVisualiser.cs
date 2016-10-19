// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;

namespace osu.Framework.Graphics.Visualisation
{
    public class DrawVisualiser : OverlayContainer
    {
        private TreeContainer treeContainer;

        private InfoOverlay overlay;
        private ScheduledDelegate task;

        public DrawVisualiser()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                treeContainer = new TreeContainer
                {
                    ChooseTarget = chooseTarget,
                    GoUpOneParent = delegate { Target = Target?.Parent ?? Target; }
                },
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

            if (!d.IsVisible) return null;

            var dAsContainer = d as Container;

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
                        containedTarget = contained;
                }
            }

            return containedTarget ?? (d.ScreenSpaceDrawQuad.Contains(state.Mouse.NativeState.Position) ? d : null);
        }

        private VisualisedDrawable targetVD;

        private Drawable target;
        public Drawable Target
        {
            get { return target; }
            set
            {
                if (targetVD != null)
                {
                    treeContainer.Remove(targetVD);
                    targetVD.Dispose();
                    targetVD = null;
                }

                target = value;

                if (target != null)
                {
                    targetVD = new VisualisedDrawable(target);
                    treeContainer.Add(targetVD);
                }
            }
        }

        private void runUpdate()
        {
            if (Target == null) return;

            visualise(Target, targetVD);
        }

        private void visualise(Drawable d, VisualisedDrawable vis)
        {
            if (d == this) return;

            vis.CheckExpiry();

            var drawables = vis.Flow.Children.Cast<VisualisedDrawable>();
            foreach (var dd in drawables)
            {
                if (!dd.CheckExpiry())
                    visualise(dd.Target, dd);
            }

            Container dContainer = d as Container;

            if (d is SpriteText) return;

            if (dContainer == null) return;

            foreach (var c in dContainer.Children)
            {
                var dr = drawables.FirstOrDefault(x => x.Target == c);

                if (dr == null)
                {
                    var cLocal = c;
                    dr = new VisualisedDrawable(cLocal)
                    {
                        Hovered = delegate { showOverlayFor(cLocal); },
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
            return findTargetIn(Parent?.Parent?.Parent, state);
        }

        protected override bool OnClick(InputState state)
        {
            if (targetSearching)
            {
                Target = findTarget(state)?.Parent;

                if (Target != null)
                {
                    targetSearching = false;
                    showOverlayFor(null);
                    return true;
                }
            }

            return base.OnClick(state);
        }

        protected override bool OnMouseMove(InputState state)
        {
            if (targetSearching)
                showOverlayFor(findTarget(state));

            return base.OnMouseMove(state);
        }

        protected override void OnHoverLost(InputState state)
        {
            showOverlayFor(null);
            base.OnHoverLost(state);
        }

        private void showOverlayFor(Drawable target)
        {
            if (target != null)
            {
                if (overlay == null)
                    Add(overlay = new InfoOverlay());

                overlay.Target = target;
            }
            else if (overlay != null)
            {
                Remove(overlay);
                overlay.Dispose();
                overlay = null;
            }
        }
    }
}
