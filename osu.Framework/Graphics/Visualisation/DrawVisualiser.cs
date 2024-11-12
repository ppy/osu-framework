// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osuTK;

namespace osu.Framework.Graphics.Visualisation
{
    [Cached]
    // Implementing IRequireHighFrequencyMousePosition is necessary to gain the ability to block high frequency mouse position updates.
    internal partial class DrawVisualiser : OverlayContainer, IContainVisualisedDrawables, IRequireHighFrequencyMousePosition
    {
        public Vector2 ToolPosition
        {
            get => treeContainer.Position;
            set => treeContainer.Position = value;
        }

        [Cached]
        private readonly TreeContainer treeContainer;

        private VisualisedDrawable highlightedTarget;
        private readonly DrawableInspector drawableInspector;
        private readonly InfoOverlay overlay;
        private InputManager inputManager;

        protected override bool BlockPositionalInput => Searching;

        public DrawVisualiser()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                overlay = new InfoOverlay(),
                treeContainer = new TreeContainer
                {
                    State = { BindTarget = State },
                    ChooseTarget = () =>
                    {
                        Searching = true;
                        Target = null;
                    },
                    GoUpOneParent = goUpOneParent,
                    ToggleInspector = toggleInspector,
                },
                new CursorContainer()
            };

            drawableInspector = treeContainer.DrawableInspector;

            drawableInspector.State.ValueChanged += v =>
            {
                switch (v.NewValue)
                {
                    case Visibility.Hidden:
                        // Dehighlight everything automatically if property display is closed
                        setHighlight(null);
                        break;
                }
            };
        }

        private void goUpOneParent()
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
                    drawableInspector.Show();
                    setHighlight(visualised);
                }
            }
        }

        private void toggleInspector()
        {
            if (targetVisualiser == null)
                return;

            drawableInspector.ToggleVisibility();

            if (drawableInspector.State.Value == Visibility.Visible)
                setHighlight(targetVisualiser);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            inputManager = GetContainingInputManager();
        }

        protected override bool Handle(UIEvent e) => Searching;

        protected override void PopIn()
        {
            this.FadeIn(100);
            Searching = Target == null;
        }

        protected override void PopOut()
        {
            this.FadeOut(100);

            setHighlight(null);
            drawableInspector.Hide();

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
                drawableInspector.Show();

                // Either highlight or dehighlight the target, depending on whether
                // it is currently highlighted
                setHighlight(d);
            };

            visualiser.Depth = 0;

            treeContainer.Target = targetVisualiser = visualiser;
            targetVisualiser.TopLevel = true;
        }

        void IContainVisualisedDrawables.RemoveVisualiser(VisualisedDrawable visualiser)
        {
            target = null;

            targetVisualiser.TopLevel = false;
            targetVisualiser = null;

            treeContainer.Target = null;

            if (Target == null)
                drawableInspector.Hide();
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
            overlay.Target = Searching ? cursorTarget : inputManager.HoveredDrawables.OfType<VisualisedDrawable>().FirstOrDefault()?.Target;
        }

        private static readonly Dictionary<Type, bool> is_type_valid_target_cache = new Dictionary<Type, bool>();

        private void updateCursorTarget()
        {
            Drawable drawableTarget = null;
            CompositeDrawable compositeTarget = null;
            Quad? maskingQuad = null;

            findTarget(inputManager);

            cursorTarget = drawableTarget ?? compositeTarget;

            // Finds the targeted drawable and composite drawable. The search stops if a drawable is targeted.
            void findTarget(Drawable drawable)
            {
                // Ignore proxied drawables (they may be at a different visual layer).
                if (drawable.HasProxy)
                    return;

                // When a proxy is encountered, restore the original drawable for target testing.
                while (drawable.IsProxy)
                    drawable = drawable.Original;

                if (drawable == this || drawable is Component)
                    return;

                if (!drawable.IsPresent)
                    return;

                if (drawable.AlwaysPresent && Precision.AlmostEquals(drawable.Alpha, 0f))
                    return;

                if (drawable is CompositeDrawable composite)
                {
                    Quad? oldMaskingQuad = maskingQuad;

                    // BufferedContainers implicitly mask via their frame buffer
                    if (composite.Masking || composite is BufferedContainer)
                        maskingQuad = composite.ScreenSpaceDrawQuad;

                    for (int i = composite.AliveInternalChildren.Count - 1; i >= 0; i--)
                    {
                        findTarget(composite.AliveInternalChildren[i]);

                        if (drawableTarget != null)
                            return;
                    }

                    maskingQuad = oldMaskingQuad;

                    if (!validForTarget(composite))
                        return;

                    compositeTarget ??= composite;

                    // Allow targeting composites that don't have any content but display a border/glow

                    if (!composite.Masking)
                        return;

                    if ((composite.BorderThickness > 0 && composite.BorderColour.MaxAlpha > 0)
                        || (composite.EdgeEffect.Type != EdgeEffectType.None && composite.EdgeEffect.Radius > 0 && composite.EdgeEffect.Colour.Alpha > 0))
                    {
                        drawableTarget = composite;
                    }
                }
                else
                {
                    if (!validForTarget(drawable))
                        return;

                    drawableTarget = drawable;
                }
            }

            // Valid if the drawable contains the mouse position and the position wouldn't be masked by the parent
            bool validForTarget(Drawable drawable)
            {
                if (!drawable.ScreenSpaceDrawQuad.Contains(inputManager.CurrentState.Mouse.Position)
                    || maskingQuad?.Contains(inputManager.CurrentState.Mouse.Position) == false)
                {
                    return false;
                }

                Type type = drawable.GetType();

                if (is_type_valid_target_cache.TryGetValue(type, out bool valid))
                    return valid;

                // Exclude "overlay" objects (Component/etc) that don't draw anything and don't override CreateDrawNode().
                valid = type.GetMethod(nameof(CreateDrawNode), BindingFlags.Instance | BindingFlags.NonPublic)?.DeclaringType != typeof(Drawable);

                // Exclude objects that specify they should be hidden anyway.
                valid &= !type.GetCustomAttributes<DrawVisualiserHiddenAttribute>(true).Any();

                return is_type_valid_target_cache[type] = valid;
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
                drawableInspector.InspectedDrawable.Value = null;
                return;
            }

            // Only update when property display is visible
            if (drawableInspector.State.Value == Visibility.Visible)
            {
                highlightedTarget = newHighlight;
                newHighlight.IsHighlighted = true;

                drawableInspector.InspectedDrawable.Value = newHighlight.Target;
            }
        }

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
            treeContainer.Target = null;

            // We don't really know where the visualised drawables are, so we have to dispose them manually
            // This is done as an optimisation so that events aren't handled while the visualiser is hidden
            var visualisers = visCache.Values.ToList();
            foreach (var v in visualisers)
                v.Dispose();

            target = null;
            targetVisualiser = null;
        }
    }
}
