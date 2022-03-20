// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;

#nullable enable

namespace osu.Framework.Graphics.Visualisation
{
    [Cached]
    internal class TreeTabContainer : EmptyToolWindow
    {
        private readonly TabControl<TreeTab> tabControl;

        public readonly DrawableTreeContainer DrawableTreeContainer;

        public readonly Container<TreeContainer> TreeContainers;

        public TreeTabContainer(DrawableTreeContainer drawableTreeContainer)
            : base("Draw Visualiser", "(Ctrl+F1 to toggle)")
        {
            BodyContent.Add(new GridContainer
            {
                RelativeSizeAxes = Axes.Y,
                AutoSizeAxes = Axes.X,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension()
                },
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize, minSize: WIDTH)
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        tabControl = new TreeTabControl
                        {
                            RelativeSizeAxes = Axes.X,
                        },
                        TreeContainers = new Container<TreeContainer>
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                        },
                    }
                }.Invert()
            });

            tabControl.Current.BindValueChanged(showTab, true);

            DrawableTreeContainer = drawableTreeContainer;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            TreeContainers.Add(DrawableTreeContainer);
            AddTab(current = new SearchingTab(DrawableTreeContainer));
        }

        private void showTab(ValueChangedEvent<TreeTab> tabChanged)
        {
            Current = tabChanged.NewValue;
        }

        protected void AddTab(TreeTab tab)
        {
            tabControl.AddItem(tab);
            tabControl.Current.Value = tab;
        }

        private TreeTab current = null!;

        public TreeTab Current
        {
            get => current;
            set
            {
                if (current == value)
                    return;

                current.Tree.Hide();

                current = value;
                tabControl.Current.Value = value;

                current.Tree.Show();
                SetCurrentToolbar(current.Tree.Toolbar);
                current.SetTarget();
            }
        }

        public ObjectTreeContainer SpawnObjectVisualiser(object initTarget)
        {
            ObjectTreeContainer visualiser = new ObjectTreeContainer();
            TreeContainers.Add(visualiser);
            AddTab(new ObjectTreeTab(visualiser, initTarget));
            return visualiser;
        }

        public void SpawnDrawableVisualiser(Drawable initTarget)
        {
            AddTab(new DrawableTreeTab(DrawableTreeContainer, initTarget));
        }

        public void SelectTarget()
        {
            if (current == null)
                return;
            Current = tabControl.Items[0];
        }

        public new void Clear()
        {
            SelectTarget();
            tabControl.Items = new[] { current };
        }

        private class TreeTabControl : TabControl<TreeTab>
        {
            public TreeTabControl()
            {
                AutoSizeAxes = Axes.Y;
                TabContainer.AllowMultiline = true;
                TabContainer.RelativeSizeAxes = Axes.X;
                TabContainer.AutoSizeAxes = Axes.Y;
            }

            protected override Dropdown<TreeTab> CreateDropdown()
                => new BasicTabControl<TreeTab>.BasicTabControlDropdown();

            protected override TabItem<TreeTab> CreateTabItem(TreeTab value)
                => new TreeTabItem(value)
                {
                    RequestClose = closeTab
                };

            private void closeTab(TreeTabItem tab)
            {
                RemoveTabItem(tab);
            }
        }

        private class TreeTabItem : TabItem<TreeTab>
        {
            private Box background;
            private SpriteText text;

            public Action<TreeTabItem> RequestClose = null!;

            public TreeTabItem(TreeTab value)
                : base(value)
            {
                AutoSizeAxes = Axes.X;
                Height = 20f;
                AddRangeInternal(new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = FrameworkColour.YellowGreenDark,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.X,
                        RelativeSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Children = new Drawable[]
                        {
                            text = new SpriteText
                            {
                                Colour = Colour4.White,
                                Font = FrameworkFont.Regular.With(size: 18),
                            },
                            new TabCloseButton
                            {
                                Action = () => RequestClose(this),
                                Alpha = value is SearchingTab ? 0f : 1f,
                            },
                        }
                    }
                });
            }

            protected override void Update()
            {
                base.Update();

                text.Text = Value.ToString();
            }

            protected override void OnActivated()
                => background.Colour = FrameworkColour.YellowGreen;

            protected override void OnDeactivated()
                => background.Colour = FrameworkColour.YellowGreenDark;

            private class TabCloseButton : ClickableContainer
            {
                private SpriteIcon icon;

                public TabCloseButton()
                {
                    Margin = new MarginPadding { Left = 10f, Right = 2f };
                    Size = new osuTK.Vector2(20f);
                    InternalChild = icon = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Scale = new osuTK.Vector2(0.65f),
                        Icon = FontAwesome.Solid.Times,
                        Colour = Colour4.OrangeRed,
                    };
                }
            }
        }
    }

    internal abstract class TreeTab
    {
        public abstract object Target { get; }
        public abstract TreeContainer Tree { get; }

        public override string ToString()
        {
            return Target.ToString() ?? "";
        }

        public abstract void SetTarget();
    }

    internal sealed class DrawableTreeTab : TreeTab
    {
        public Drawable TargetDrawable;
        public readonly DrawableTreeContainer DrawableTree;

        public override object Target => TargetDrawable;
        public override TreeContainer Tree => DrawableTree;

        public DrawableTreeTab(DrawableTreeContainer tree, Drawable target)
        {
            DrawableTree = tree;
            TargetDrawable = target;
        }

        public override void SetTarget()
        {
            DrawableTree.Target = TargetDrawable;
        }
    }

    internal sealed class SearchingTab : TreeTab
    {
        public readonly DrawableTreeContainer DrawableTree;

        public override object Target => null!;
        public override TreeContainer Tree => DrawableTree;

        public SearchingTab(DrawableTreeContainer tree)
        {
            DrawableTree = tree;
        }

        public override void SetTarget()
        {
            DrawableTree.Target = null;
        }

        public override string ToString() => "Select drawable...";
    }

    internal sealed class ObjectTreeTab : TreeTab
    {
        public readonly ObjectTreeContainer ObjectTree;

        public override object Target { get; }
        public override TreeContainer Tree => ObjectTree;

        public ObjectTreeTab(ObjectTreeContainer tree, object target)
        {
            ObjectTree = tree;
            Target = target;
            tree.Target = target;
        }

        public override void SetTarget()
        {
        }
    }

    [Cached]
    internal abstract class TreeContainer : VisibilityContainer, IContainVisualisedElements
    {
        public VisualisedElement GetVisualiserFor(object? target) => GetVisualiserForImpl(target);

        protected abstract VisualisedElement GetVisualiserForImpl(object? target);
        public abstract void RequestTarget(object? target);
        public abstract void HighlightTarget(object target);

        public abstract void AddVisualiser(VisualiserTreeNode visualiser);
        public abstract void RemoveVisualiser(VisualiserTreeNode visualiser);

        [Resolved]
        public DrawVisualiser Visualiser { get; private set; } = null!;

        [Resolved]
        public TreeTabContainer TabContainer { get; private set; } = null!;

        public FillFlowContainer Toolbar { get; private set; } = null!;
        protected TreeTabContainer Window { get; private set; } = null!;
        protected readonly ScrollContainer<Drawable> ScrollContent;

        private FillFlowContainer content;
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
        private void load(TreeTabContainer window)
        {
            Window = window;
            Toolbar = Window.AddToolbar();
        }

        protected override void PopIn() => this.FadeIn(100);

        protected override void PopOut() => this.FadeOut(100);

        protected void AddButton(string text, Action action)
        {
            Debug.Assert(Window.ToolbarContent == Toolbar);

            Window.AddButton(text, action);
        }
    }

    internal abstract class TreeContainer<NodeType, TargetType> : TreeContainer
        where NodeType : VisualisedElement
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

        public override void AddVisualiser(VisualiserTreeNode _visualiser)
        {
            if (!(_visualiser is NodeType visualiser))
                throw new InvalidOperationException("Top node must be a visualiser");
            AddVisualiser(visualiser);
        }

        public override void RemoveVisualiser(VisualiserTreeNode _visualiser)
        {
            if (!(_visualiser is NodeType visualiser))
                throw new InvalidOperationException("Top node must be a visualiser");
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
                TabContainer.SpawnObjectVisualiser(target);
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
                    ScrollContent.Clear(false);
                else
                    ScrollContent.Child = value;
            }
        }

        private NodeType? targetVisualiser;
        private TargetType? target;

        protected void SetTarget(TargetType? target) => Target = target;

        protected sealed override VisualisedElement GetVisualiserForImpl(object? target) => GetVisualiserFor((TargetType?)target);

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

                if (Visualiser == null)
                    Schedule(() => Visualiser!.SetTarget(value));
                else
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
                ((DrawableTreeTab)TabContainer.Current).TargetDrawable = parent;
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
            if (Target != null)
                TabContainer.SpawnObjectVisualiser(Target);
        }

        public override VisualisedDrawable GetVisualiserFor(Drawable? drawable)
        {
            return Visualiser.GetVisualiserFor(drawable!);
        }
    }

    internal class ObjectTreeContainer : TreeContainer<VisualisedObject, object>
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddButton(@"inspect root", inspectRoot);
            AddButton(@"inspect as drawable", inspectAsDrawable);
        }


        [Resolved]
        private Game game { get; set; } = null!;

        private void inspectRoot()
        {
            TabContainer.SpawnObjectVisualiser(game);
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
