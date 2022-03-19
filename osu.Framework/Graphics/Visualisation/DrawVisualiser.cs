// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

#nullable enable

namespace osu.Framework.Graphics.Visualisation
{
    [Cached]
    // Implementing IRequireHighFrequencyMousePosition is necessary to gain the ability to block high frequency mouse position updates.
    internal class DrawVisualiser : OverlayContainer, IRequireHighFrequencyMousePosition
    {
        public Vector2 ToolPosition
        {
            get => TabContainer.Position;
            set => TabContainer.Position = value;
        }

        private readonly DrawableTreeContainer drawableTreeContainer;
        public readonly TreeTabContainer TabContainer;

        private DrawableInspector drawableInspector => drawableTreeContainer.DrawableInspector;
        private readonly InfoOverlay overlay;
        private InputManager inputManager = null!;

        public DrawVisualiser()
        {
            RelativeSizeAxes = Axes.Both;
            Children = new Drawable[]
            {
                overlay = new InfoOverlay(),
                TabContainer = new TreeTabContainer(
                    drawableTreeContainer = new DrawableTreeContainer
                    {
                        ChooseTarget = () =>
                        {
                            Target = null;
                        },
                    })
                    {
                        State = { BindTarget = State }
                    },
                new CursorContainer()
            };
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

            drawableTreeContainer.SetHighlight(null);
            drawableInspector.Hide();

            recycleVisualisers();
        }


        private VisualisedDrawable? targetVisualiser => drawableTreeContainer.TargetVisualiser;

        internal void SetTarget(Drawable? target)
        {
            Searching = target == null;
        }

        public Drawable? Target
        {
            get => drawableTreeContainer.Target;
            set
            {
                if (value == null)
                    TabContainer.SelectTarget();
                else
                    TabContainer.SpawnDrawableVisualiser(value);
            }
        }

        private Drawable? cursorTarget;

        protected override void Update()
        {
            base.Update();

            updateCursorTarget();
            overlay.Target = Searching ? cursorTarget : inputManager.HoveredDrawables.OfType<VisualisedDrawable>().FirstOrDefault()?.TargetDrawable;
        }

        private void updateCursorTarget()
        {
            Drawable? drawableTarget = null;
            CompositeDrawable? compositeTarget = null;
            Quad? maskingQuad = null;

            findTarget(inputManager);

            cursorTarget = drawableTarget ?? compositeTarget;

            // Finds the targeted drawable and composite drawable. The search stops if a drawable is targeted.
            void findTarget(Drawable drawable)
            {
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

                    if (composite.BorderThickness > 0 && composite.BorderColour.MaxAlpha > 0
                        || composite.EdgeEffect.Type != EdgeEffectType.None && composite.EdgeEffect.Radius > 0 && composite.EdgeEffect.Colour.Linear.A > 0)
                    {
                        drawableTarget = composite;
                    }
                }
                else
                {
                    if (!validForTarget(drawable))
                        return;

                    // Special case for full-screen overlays that act as input receptors, but don't display anything
                    if (!hasCustomDrawNode(drawable))
                        return;

                    drawableTarget = drawable;
                }
            }

            // Valid if the drawable contains the mouse position and the position wouldn't be masked by the parent
            bool validForTarget(Drawable drawable)
                => drawable.ScreenSpaceDrawQuad.Contains(inputManager.CurrentState.Mouse.Position)
                   && maskingQuad?.Contains(inputManager.CurrentState.Mouse.Position) != false;
        }

        private static readonly Dictionary<Type, bool> has_custom_drawnode_cache = new Dictionary<Type, bool>();

        private bool hasCustomDrawNode(Drawable drawable)
        {
            var type = drawable.GetType();

            if (has_custom_drawnode_cache.TryGetValue(type, out bool existing))
                return existing;

            return has_custom_drawnode_cache[type] = type.GetMethod(nameof(CreateDrawNode), BindingFlags.Instance | BindingFlags.NonPublic)?.DeclaringType != typeof(Drawable);
        }

        public bool Searching { get; private set; }

        protected override bool OnMouseDown(MouseDownEvent e) => Searching;

        protected override bool OnClick(ClickEvent e)
        {
            if (Searching)
            {
                Target = cursorTarget?.Parent;

                if (Target != null)
                {
                    overlay.Target = null;
                    targetVisualiser!.ExpandAll();

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
            vis.OnDispose += () => visCache.Remove(vis.TargetDrawable);

            return visCache[drawable] = vis;
        }

        private void recycleVisualisers()
        {
            Target = null;

            // We don't really know where the visualised drawables are, so we have to dispose them manually
            // This is done as an optimisation so that events aren't handled while the visualiser is hidden
            var visualisers = visCache.Values.ToList();
            foreach (var v in visualisers)
                v.Dispose();
        }

        public override string ToString() => "Draw Visualiser (inspecting will crash!)";
    }
}
