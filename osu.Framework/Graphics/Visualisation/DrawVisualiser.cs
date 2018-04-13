// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input;

namespace osu.Framework.Graphics.Visualisation
{
    public class DrawVisualiser : OverlayContainer
    {
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
                    ChooseTarget = chooseTarget,
                    GoUpOneParent = delegate
                    {
                        Drawable lastHighlight = highlightedTarget?.Target;

                        var parent = Target?.Parent;
                        if (parent?.Parent != null)
                            Target = Target?.Parent;

                        // Rehighlight the last highlight
                        if (lastHighlight != null)
                        {
                            VisualisedDrawable visualised = targetDrawable.FindVisualisedDrawable(lastHighlight);
                            if (visualised != null)
                            {
                                propertyDisplay.State = Visibility.Visible;
                                setHighlight(visualised);
                            }
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
            if (Target == null)
                chooseTarget();
            else
                createRootVisualisedDrawable();
        }

        protected override void PopOut()
        {
            this.FadeOut(100);

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

        private VisualisedDrawable targetDrawable;

        private void removeRootVisualisedDrawable(bool hideProperties = true)
        {
            if (hideProperties)
                propertyDisplay.State = Visibility.Hidden;

            if (targetDrawable != null)
            {
                if (targetDrawable.Parent != null)
                {
                    // targetDrawable may have gotten purged from the TreeContainer
                    treeContainer.Remove(targetDrawable);
                    targetDrawable.Dispose();
                }
                targetDrawable = null;
            }
        }

        private void createRootVisualisedDrawable()
        {
            removeRootVisualisedDrawable(target == null);

            if (target == null)
                return;

            targetDrawable = new VisualisedDrawable(target, treeContainer)
            {
                RequestTarget = d => Target = d,
                HighlightTarget = d =>
                {
                    propertyDisplay.State = Visibility.Visible;

                    // Either highlight or dehighlight the target, depending on whether
                    // it is currently highlighted
                    setHighlight(d);

                }
            };

            treeContainer.Add(targetDrawable);
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

        protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
        {
            return targetSearching;
        }

        private Drawable findTarget(InputState state)
        {
            return findTargetIn(Parent?.Parent, state);
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
            overlay.Target = targetSearching ? findTarget(state) : inputManager.HoveredDrawables.OfType<VisualisedDrawable>().FirstOrDefault()?.Target;
            return base.OnMouseMove(state);
        }
    }
}
