// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;

#nullable enable

namespace osu.Framework.Graphics.Visualisation
{
    [Cached]
    internal interface ITreeContainer : IDrawable, IContainVisualisedElements
    {
        VisualisedElement GetVisualiserFor(object? target);
        void RequestTarget(object? target);
        void HighlightTarget(object target);
    }

    internal abstract class TreeContainer<NodeType, TargetType> : ToolWindow, ITreeContainer
        where NodeType : VisualisedElement
        where TargetType : class
    {
        protected internal DrawableInspector DrawableInspector { get; protected set; }

        [Resolved]
        protected DrawVisualiser Visualiser { get; private set; } = null!;

        protected TreeContainer(string title, string keyHelpText)
            : base(title, keyHelpText)
        {
            MainHorizontalContent.Add(DrawableInspector = new DrawableInspector());

            DrawableInspector.State.ValueChanged += v =>
            {
                switch (v.NewValue)
                {
                    case Visibility.Hidden:
                        // Dehighlight everything automatically if property display is closed
                        SetHighlight(null);
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
                return;

            DrawableInspector.ToggleVisibility();

            if (DrawableInspector.State.Value == Visibility.Visible)
                SetHighlight(TargetVisualiser);
        }

        public virtual TargetType? Target
        {
            set
            {
                if (target == value)
                    return;

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

        protected override bool OnClick(ClickEvent e) => true;

        void IContainVisualisedElements.AddVisualiser(VisualiserTreeNode _visualiser)
        {
            if (!(_visualiser is NodeType visualiser))
                throw new InvalidOperationException("Top node must be a visualiser");
            AddVisualiser(visualiser);
        }

        void IContainVisualisedElements.RemoveVisualiser(VisualiserTreeNode _visualiser)
        {
            if (!(_visualiser is NodeType visualiser))
                throw new InvalidOperationException("Top node must be a visualiser");
            RemoveVisualiser(visualiser);
        }

        public void RequestTarget(object? target)
        {
            if (target is Drawable d)
            {
                Visualiser.Target = d;
                TargetVisualiser!.ExpandAll();
            }
            else
            {
                Visualiser.SpawnVisualiser<ObjectTreeContainer, VisualisedObject, object>(target);
            }
        }

        public void HighlightTarget(object target)
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
                    ScrollContent.Clear(false);
                else
                    ScrollContent.Child = value;
            }
        }

        private NodeType? targetVisualiser;
        private TargetType? target;

        protected void SetTarget(TargetType? target) => Target = target;

        VisualisedElement ITreeContainer.GetVisualiserFor(object? target) => GetVisualiserFor((TargetType?)target);

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

    internal class DrawableTreeContainer : TreeContainer<VisualisedDrawable, Drawable>
    {
        private readonly SpriteText waitingText;

        public Action? ChooseTarget;

        public DrawableTreeContainer()
            : base("Draw Visualiser", "(Ctrl+F1 to toggle)")
        {
            AddInternal(waitingText = new SpriteText
            {
                Text = @"Waiting for target selection...",
                Font = FrameworkFont.Regular,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddButton(@"choose target", () => ChooseTarget?.Invoke());
            AddButton(@"up one parent", goUpOneParent);
            AddButton(@"inspect as object", inspectAsObject);
        }

        public override Drawable? Target
        {
            set
            {
                base.Target = value;

                Visualiser.SetTarget(value);
            }
        }

        protected override void Update()
        {
            waitingText.Alpha = Visualiser.Searching ? 1 : 0;
            base.Update();
        }

        private void goUpOneParent()
        {
            Drawable? lastHighlight = HighlightedTarget?.TargetDrawable;

            var parent = Target?.Parent;

            if (parent != null)
            {
                var lastVisualiser = TargetVisualiser!;

                Target = parent;
                lastVisualiser.SetContainer(TargetVisualiser);

                TargetVisualiser!.Expand();
            }

            // Rehighlight the last highlight
            if (lastHighlight != null)
            {
                VisualisedDrawable? visualised = TargetVisualiser!.FindVisualisedDrawable(lastHighlight);

                if (visualised != null)
                {
                    DrawableInspector.Show();
                    SetHighlight(visualised);
                }
            }
        }

        private void inspectAsObject()
        {
            Visualiser.SpawnVisualiser<ObjectTreeContainer, VisualisedObject, object>(Target);
        }

        public override VisualisedDrawable GetVisualiserFor(Drawable? drawable)
        {
            return Visualiser.GetVisualiserFor(drawable!);
        }
    }

    internal class ObjectTreeContainer : TreeContainer<VisualisedObject, object>
    {
        public ObjectTreeContainer()
            : base("Tree visualiser", "(Esc to close)")
        {
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddButton(@"inspect root", inspectRoot);
            AddButton(@"inspect as drawable", inspectAsDrawable);
        }


        [Resolved]
        private Game game { get; set; } = null!;

        protected sealed override bool StartHidden => false;
        protected override void PopOut()
        {
            this.RemoveAndDisposeImmediately();
        }

        private void inspectRoot()
        {
            Target = game;
        }

        private void inspectAsDrawable()
        {
            VisualisedElement? element = HighlightedTarget ?? TargetVisualiser;
            if (element?.Target is Drawable d)
                Visualiser.Target = d;
        }

        public override VisualisedObject GetVisualiserFor(object? element)
        {
            return VisualisedObject.CreateFor(element);
        }
    }
}
