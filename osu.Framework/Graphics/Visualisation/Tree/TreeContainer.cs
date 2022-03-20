// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;

using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// Presents a view into a node graph.
    /// </summary>
    [Cached]
    internal abstract class TreeContainer : VisibilityContainer, INodeContainer
    {
        public ElementNode GetVisualiserFor(object? target)
        {
            return GetVisualiserForImpl(target);
        }

        protected abstract ElementNode GetVisualiserForImpl(object? target);
        public abstract void RequestTarget(object? target);
        public abstract void HighlightTarget(object target);

        public abstract void AddVisualiser(TreeNode visualiser);
        public abstract void RemoveVisualiser(TreeNode visualiser);

        [Resolved]
        public DrawVisualiser Visualiser { get; private set; } = null!;

        [Resolved]
        public TabbedTreeWindow TabContainer { get; private set; } = null!;

        public FillFlowContainer Toolbar { get; private set; } = null!;
        protected TabbedTreeWindow Window { get; private set; } = null!;
        protected readonly ScrollContainer<Drawable> ScrollContent;

        private readonly FillFlowContainer content;
        protected override Container<Drawable> Content => content;

        public TreeContainer()
        {
            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;
            AddInternal(content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                Direction = FillDirection.Horizontal,
                Child = ScrollContent = new BasicScrollContainer<Drawable>
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = EmptyToolWindow.WIDTH,
                },
            });
        }

        [BackgroundDependencyLoader]
        private void load(TabbedTreeWindow window)
        {
            Window = window;
            Toolbar = Window.AddToolbar();
        }

        protected override void PopIn()
        {
            this.FadeIn(100);
        }

        protected override void PopOut()
        {
            this.FadeOut(100);
        }

        protected void AddButton(string text, Action action)
        {
            Debug.Assert(Window.ToolbarContent == Toolbar);

            Window.AddButton(text, action);
        }
    }

    internal abstract class TreeContainer<NodeType, TargetType> : TreeContainer
        where NodeType : ElementNode
        where TargetType : class
    {
        protected internal DrawableInspector DrawableInspector { get; protected set; }

        protected TreeContainer()
        {
            Add(DrawableInspector = new DrawableInspector());

            DrawableInspector.State.ValueChanged += v =>
            {
                switch (v.NewValue)
                {
                    case Visibility.Hidden:
                        // Dehighlight everything automatically if property display is closed
                        SetHighlight(null);
                        break;
                    case Visibility.Visible:
                        break;
                    default:
                        break;
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddButton(@"toggle inspector", toggleInspector);
        }

        private void toggleInspector()
        {
            if (TargetVisualiser == null)
            {
                return;
            }

            DrawableInspector.ToggleVisibility();

            if (DrawableInspector.State.Value == Visibility.Visible)
            {
                SetHighlight(TargetVisualiser);
            }
        }

        public virtual TargetType? Target
        {
            set
            {
                if (target == value)
                {
                    return;
                }

                if (target != null)
                {
                    GetVisualiserFor(target).SetContainer(null);
                }

                target = value;

                if (target != null)
                {
                    GetVisualiserFor(target).SetContainer(this);
                }
            }
            get => target;
        }

        protected override bool OnClick(ClickEvent e)
        {
            return true;
        }

        public override void AddVisualiser(TreeNode _visualiser)
        {
            if (!(_visualiser is NodeType visualiser))
            {
                throw new InvalidOperationException("Top node must be a visualiser");
            }

            AddVisualiser(visualiser);
        }

        public override void RemoveVisualiser(TreeNode _visualiser)
        {
            if (!(_visualiser is NodeType visualiser))
            {
                throw new InvalidOperationException("Top node must be a visualiser");
            }

            RemoveVisualiser(visualiser);
        }

        public override void RequestTarget(object? target)
        {
            if (target is Drawable d)
            {
                Visualiser.Target = d;
                TargetVisualiser!.ExpandAll();
            }
            else if (target != null)
            {
                _ = TabContainer.SpawnObjectVisualiser(target);
            }
        }

        public override void HighlightTarget(object target)
        {
            DrawableInspector.Show();

            // Either highlight or dehighlight the target, depending on whether
            // it is currently highlighted
            SetHighlight((NodeType)target);
        }

        protected void AddVisualiser(NodeType visualiser)
        {
            visualiser.Depth = 0;

            TargetVisualiser = visualiser;
            TargetVisualiser.TopLevel = true;
        }

        protected void RemoveVisualiser(NodeType visualiser)
        {
            target = null;

            TargetVisualiser!.TopLevel = false;
            TargetVisualiser = null;

            DrawableInspector.Hide();
        }

        public NodeType? TargetVisualiser
        {
            get => targetVisualiser;
            private set
            {
                targetVisualiser = value;

                if (value == null)
                {
                    ScrollContent.Clear(false);
                }
                else
                {
                    ScrollContent.Child = value;
                }
            }
        }

        private NodeType? targetVisualiser;
        private TargetType? target;

        protected void SetTarget(TargetType? target)
        {
            Target = target;
        }

        protected sealed override ElementNode GetVisualiserForImpl(object? target)
        {
            return GetVisualiserFor((TargetType?)target);
        }

        public abstract NodeType GetVisualiserFor(TargetType? target);

        protected NodeType? HighlightedTarget;
        public void SetHighlight(NodeType? newHighlight)
        {
            if (HighlightedTarget != null)
            {
                // Dehighlight the lastly highlighted target
                HighlightedTarget.IsHighlighted = false;
                HighlightedTarget = null;
            }

            if (newHighlight == null)
            {
                DrawableInspector.InspectedTarget.Value = null;
                return;
            }

            // Only update when property display is visible
            if (DrawableInspector.State.Value == Visibility.Visible)
            {
                HighlightedTarget = newHighlight;
                newHighlight.IsHighlighted = true;

                DrawableInspector.InspectedTarget.Value = newHighlight.Target;
            }
        }
    }
}
