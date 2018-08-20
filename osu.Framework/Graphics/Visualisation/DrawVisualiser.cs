// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.States;

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

        protected override bool BlockPassThroughMouse => false;

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

        private Drawable findTargetIn(Drawable d, InputState state)
        {
            if (d is DrawVisualiser) return null;
            if (d is CursorContainer) return null;
            if (d is PropertyDisplay) return null;

            if (!d.IsPresent) return null;

            bool containsCursor = d.ScreenSpaceDrawQuad.Contains(state.Mouse.NativeState.Position);
            // This is an optimization: We don't need to consider drawables which we don't hover, and which do not
            // forward input further to children (via d.ReceiveMouseInputAt). If they do forward input to children, then there
            // is a good chance they have children poking out of their bounds, which we need to catch.
            if (!containsCursor && !d.ReceiveMouseInputAt(state.Mouse.NativeState.Position))
                return null;

            var dAsContainer = d as CompositeDrawable;

            Drawable containedTarget = null;

            if (dAsContainer != null)
            {
                if (!dAsContainer.InternalChildren.Any())
                    return null;

                foreach (var c in dAsContainer.AliveInternalChildren)
                {
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

            return containedTarget ?? (containsCursor ? d : null);
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

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args) => Searching;

        private Drawable findTarget(InputState state) => findTargetIn(Parent?.Parent, state);

        protected override bool OnClick(InputState state)
        {
            if (Searching)
            {
                Target = findTarget(state)?.Parent;

                if (Target != null)
                {
                    overlay.Target = null;
                    targetVisualiser.ExpandAll();

                    Searching = false;
                    return true;
                }
            }

            return base.OnClick(state);
        }

        protected override bool OnMouseMove(InputState state)
        {
            overlay.Target = Searching ? findTarget(state) : inputManager.HoveredDrawables.OfType<VisualisedDrawable>().FirstOrDefault()?.Target;
            return base.OnMouseMove(state);
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
            treeContainer.Clear();

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
