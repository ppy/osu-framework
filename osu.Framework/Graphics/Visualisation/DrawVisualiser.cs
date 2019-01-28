// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input;
using osu.Framework.Input.Events;

namespace osu.Framework.Graphics.Visualisation
{
    [Cached]
    internal class DrawVisualiser : OverlayContainer, IContainVisualisedDrawables
    {
        [Cached]
        private readonly TreeContainer treeContainer;

        private VisualisedDrawable highlightedTarget;

        private readonly PropertyDisplay propertyDisplay;

        private readonly InfoOverlay overlay;

        private InputManager inputManager;

        public DrawVisualiser()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                overlay = new InfoOverlay(),
                treeContainer = new TreeContainer
                {
                    ChooseTarget = () =>
                    {
                        Searching = true;
                        Target = null;
                    },
                    GoUpOneParent = delegate
                    {
                        Drawable lastHighlight = highlightedTarget?.Target;

                        var parent = Target?.Parent;
                        if (parent != null)
                        {
                            var lastVisualiser = targetVisualiser;

                            Target = parent;
                            lastVisualiser.SetContainer(targetVisualiser);

                            targetVisualiser.Expand();
                        }

                        // Rehighlight the last highlight
                        if (lastHighlight != null)
                        {
                            VisualisedDrawable visualised = targetVisualiser.FindVisualisedDrawable(lastHighlight);
                            if (visualised != null)
                            {
                                propertyDisplay.State = Visibility.Visible;
                                setHighlight(visualised);
                            }
                        }
                    },
                    ToggleProperties = delegate
                    {
                        if (targetVisualiser == null)
                            return;

                        propertyDisplay.ToggleVisibility();

                        if (propertyDisplay.State == Visibility.Visible)
                            setHighlight(targetVisualiser);
                    },
                },
                new CursorContainer()
            };

            propertyDisplay = treeContainer.PropertyDisplay;

            propertyDisplay.StateChanged += visibility =>
            {
                switch (visibility)
                {
                    case Visibility.Hidden:
                        // Dehighlight everything automatically if property display is closed
                        setHighlight(null);
                        break;
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            inputManager = GetContainingInputManager();
        }

        protected override bool BlockPositionalInput => false;

        protected override void PopIn()
        {
            this.FadeIn(100);
            Searching = Target == null;
        }

        protected override void PopOut()
        {
            this.FadeOut(100);

            recycleVisualisers();
        }

        void IContainVisualisedDrawables.AddVisualiser(VisualisedDrawable visualiser)
        {
            visualiser.RequestTarget = d =>
            {
                Target = d;
                targetVisualiser.ExpandAll();
            };

            visualiser.HighlightTarget = d =>
            {
                propertyDisplay.State = Visibility.Visible;

                // Either highlight or dehighlight the target, depending on whether
                // it is currently highlighted
                setHighlight(d);
            };

            visualiser.Depth = 0;

            treeContainer.Child = targetVisualiser = visualiser;
        }

        void IContainVisualisedDrawables.RemoveVisualiser(VisualisedDrawable visualiser)
        {
            target = null;
            targetVisualiser = null;
            treeContainer.Remove(visualiser);

            if (Target == null)
                propertyDisplay.State = Visibility.Hidden;
        }

        private VisualisedDrawable targetVisualiser;
        private Drawable target;
        public Drawable Target
        {
            get => target;
            set
            {
                if (target != null)
                {
                    GetVisualiserFor(target).SetContainer(null);
                    targetVisualiser = null;
                }

                target = value;

                if (target != null)
                {
                    targetVisualiser = GetVisualiserFor(target);
                    targetVisualiser.SetContainer(this);
                }
            }
        }

        private Drawable cursorTarget;

        protected override void Update()
        {
            base.Update();

            updateCursorTarget();
        }

        private void updateCursorTarget()
        {
            Drawable drawableTarget = null;
            CompositeDrawable compositeTarget = null;

            findTarget(inputManager);

            cursorTarget = drawableTarget ?? compositeTarget;

            // Finds the targeted drawable and composite drawable. The search stops if a drawable is targeted.
            void findTarget(Drawable drawable)
            {
                if (!isValidTarget(drawable))
                    return;

                if (drawable is CompositeDrawable composite)
                {
                    for (int i = composite.AliveInternalChildren.Count - 1; i >= 0; i--)
                    {
                        findTarget(composite.AliveInternalChildren[i]);

                        if (drawableTarget != null)
                            return;
                    }

                    if (compositeTarget == null)
                        compositeTarget = composite;
                }
                else if (!(drawable is Component))
                    drawableTarget = drawable;
            }

            bool isValidTarget(Drawable drawable)
            {
                if (drawable is DrawVisualiser || drawable is CursorContainer || drawable is PropertyDisplay)
                    return false;

                if (!drawable.IsPresent)
                    return false;

                bool containsCursor = drawable.ScreenSpaceDrawQuad.Contains(inputManager.CurrentState.Mouse.Position);
                // This is an optimization: We don't need to consider drawables which we don't hover, and which do not
                // forward input further to children (via d.ReceivePositionalInputAt). If they do forward input to children, then there
                // is a good chance they have children poking out of their bounds, which we need to catch.
                if (!containsCursor && !drawable.ReceivePositionalInputAt(inputManager.CurrentState.Mouse.Position))
                    return false;

                return true;
            }
        }

        public bool Searching { get; private set; }

        private void setHighlight(VisualisedDrawable newHighlight)
        {
            if (highlightedTarget != null)
            {
                // Dehighlight the lastly highlighted target
                highlightedTarget.IsHighlighted = false;
                highlightedTarget = null;
            }

            if (newHighlight == null)
            {
                propertyDisplay.UpdateFrom(null);
                return;
            }

            // Only update when property display is visible
            if (propertyDisplay.State == Visibility.Visible)
            {
                highlightedTarget = newHighlight;
                newHighlight.IsHighlighted = true;

                propertyDisplay.UpdateFrom(newHighlight.Target);
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e) => Searching;

        protected override bool OnClick(ClickEvent e)
        {
            if (Searching)
            {
                Target = cursorTarget?.Parent;

                if (Target != null)
                {
                    overlay.Target = null;
                    targetVisualiser.ExpandAll();

                    Searching = false;
                    return true;
                }
            }

            return base.OnClick(e);
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            overlay.Target = Searching ? cursorTarget : inputManager.HoveredDrawables.OfType<VisualisedDrawable>().FirstOrDefault()?.Target;
            return overlay.Target != null;
        }

        private readonly Dictionary<Drawable, VisualisedDrawable> visCache = new Dictionary<Drawable, VisualisedDrawable>();

        public VisualisedDrawable GetVisualiserFor(Drawable drawable)
        {
            if (visCache.TryGetValue(drawable, out var existing))
                return existing;

            var vis = new VisualisedDrawable(drawable);
            vis.OnDispose += () => visCache.Remove(vis.Target);

            return visCache[drawable] = vis;
        }

        private void recycleVisualisers()
        {
            // May come from the disposal thread, in which case they won't ever be reused anyway
            Schedule(() => treeContainer.Clear());

            // We don't really know where the visualised drawables are, so we have to dispose them manually
            // This is done as an optimisation so that events aren't handled while the visualiser is hidden
            var visualisers = visCache.Values.ToList();
            foreach (var v in visualisers)
                v.Dispose();

            target = null;
            targetVisualiser = null;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            recycleVisualisers();
        }
    }
}
