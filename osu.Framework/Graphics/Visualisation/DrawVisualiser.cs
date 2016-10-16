// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Drawables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Threading;
using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Graphics.Visualisation
{
    public class DrawVisualiser : Container
    {
        private TreeContainer treeContainer;

        public DrawVisualiser()
        {
            RelativeSizeAxes = Axes.Both;
            Alpha = 0;

            Children = new Drawable[]
            {
                treeContainer = new TreeContainer
                {
                    BeginRun = delegate { Scheduler.AddDelayed(runUpdate, 200, true); },
                    ChooseTarget = chooseTarget,
                    GoUpOneParent = delegate { Target = Target?.Parent ?? Target; }
                },
            };
        }

        bool targetSearching;

        private void chooseTarget()
        {
            Target = null;
            targetSearching = true;
        }

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            return targetSearching;
        }

        protected override bool OnClick(InputState state)
        {
            if (targetSearching)
            {
                Target = findTargetIn(Parent, state)?.Parent;

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
            {
                showOverlayFor(findTargetIn(Parent, state));
            }

            return base.OnMouseMove(state);
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

            return containedTarget ?? (d.ScreenSpaceDrawQuad.Contains(state.Mouse.Position) ? d : null);
        }

        private VisualisedDrawable targetVD;

        private Drawable target;
        public Drawable Target
        {
            get { return target; }
            set
            {
                if (targetVD != null)
                    treeContainer.Remove(targetVD);

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
                    dr = new VisualisedDrawable(c)
                    {
                        Selected = onSelect
                    };
                    vis.Flow.Add(dr);
                }

                visualise(c, dr);
            }
        }

        protected override void OnHoverLost(InputState state)
        {
            if (overlay != null)
                Remove(overlay);

            base.OnHoverLost(state);
        }

        private InfoOverlay overlay;

        private void onSelect(VisualisedDrawable obj)
        {
            showOverlayFor(obj.Target);
        }

        private void showOverlayFor(Drawable target)
        {
            if (overlay != null)
                Remove(overlay);

            if (target != null)
                Add(overlay = new InfoOverlay(target));
        }

        class FlashyBox : Box
        {
            public FlashyBox()
            {
                Size = new Vector2(4);
                Origin = Anchor.Centre;
                Colour = Color4.Red;
            }

            public override void Load(BaseGame game)
            {
                base.Load(game);

                FadeColour(Color4.White, 500);
                Delay(500);
                FadeColour(Color4.Red, 500);
                Delay(500);
                Loop();

                DelayReset();
                ScaleTo(3);
                ScaleTo(1, 200);
            }
        }

        class InfoOverlay : Container
        {
            private Drawable target;

            private Box tl, tr, bl, br;

            public InfoOverlay(Drawable target)
            {
                this.target = target;
                target.OnInvalidate += update;

                RelativeSizeAxes = Axes.Both;

                Children = new Drawable[]
                {
                    tl = new FlashyBox(),
                    tr = new FlashyBox(),
                    bl = new FlashyBox(),
                    br = new FlashyBox()
                };
            }

            public override void Load(BaseGame game)
            {
                base.Load(game);
                update();
            }

            private void update()
            {
                Quad q = target.ScreenSpaceDrawQuad * DrawInfo.MatrixInverse;

                tl.Position = q.TopLeft;
                tr.Position = q.TopRight;
                bl.Position = q.BottomLeft;
                br.Position = q.BottomRight;

                if (!target.IsAlive)
                    Expire();
            }
        }
    }
}
