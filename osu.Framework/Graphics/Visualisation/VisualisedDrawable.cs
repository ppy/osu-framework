// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;

using System.Collections;
using osu.Framework.Bindables;
using System.Collections.Specialized;
using osu.Framework.Graphics.UserInterface;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using osu.Framework.Extensions.TypeExtensions;

#nullable enable

namespace osu.Framework.Graphics.Visualisation
{
    internal abstract class VisualiserTreeNode : Container, IContainVisualisedElements, IEnumerable<VisualiserTreeNode>
    {
        protected const int LINE_HEIGHT = 12;

        protected Box Background = null!;
        protected VisualisedDrawableFlow Flow = null!;
        protected Container ConnectionContainer = null!;

        private const float row_width = 10;
        private const float row_height = 20;

        private Container<Drawable> content = null!;
        protected override Container<Drawable> Content => content;

        private bool isHighlighted;

        public bool IsHighlighted
        {
            get => isHighlighted;
            set
            {
                isHighlighted = value;

                UpdateColours();
                if (value)
                    Expand();
            }
        }

        protected virtual void UpdateColours()
        {
            if (IsHighlighted)
            {
                Background.Colour = FrameworkColour.YellowGreen;
            }
            else if (IsHovered)
            {
                Background.Colour = FrameworkColour.BlueGreen;
            }
            else
            {
                Background.Colour = Color4.Transparent;
            }
        }

        [Resolved]
        protected TreeContainer Tree { get; private set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            AddRangeInternal(new Drawable[]
            {
                Flow = new VisualisedDrawableFlow
                {
                    Direction = FillDirection.Vertical,
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Position = new Vector2(row_width, row_height)
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Position = new Vector2(row_width, 0),
                    Size = new Vector2(1, row_height),
                    Children = new Drawable[]
                    {
                        Background = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Colour = Color4.Transparent,
                        },
                        content = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Y,
                            AutoSizeAxes = Axes.X,
                            Direction = FillDirection.Horizontal,
                        }
                    }
                }
            });

            const float connection_width = 1;

            AddInternal(ConnectionContainer = new Container
            {
                Colour = FrameworkColour.Green,
                RelativeSizeAxes = Axes.Y,
                Width = connection_width,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        EdgeSmoothness = new Vector2(0.5f),
                    },
                    new Box
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.CentreLeft,
                        Y = row_height / 2,
                        Width = row_width / 2,
                        EdgeSmoothness = new Vector2(0.5f),
                    }
                }
            });
        }

        public virtual void AddVisualiser(VisualiserTreeNode visualiser)
        {
            Flow.EnsureFlowMutationAllowed();
            Flow.Add(visualiser);
        }

        public virtual void RemoveVisualiser(VisualiserTreeNode visualiser)
        {
            Flow.EnsureFlowMutationAllowed();
            Flow.Remove(visualiser);
        }

        public VisualisedDrawable? FindVisualisedDrawable(Drawable drawable)
        {
            if (this is VisualisedDrawable visualisedDrawable && drawable == visualisedDrawable.TargetDrawable)
                return visualisedDrawable;

            foreach (var child in Flow)
            {
                var vis = child.FindVisualisedDrawable(drawable);
                if (vis != null)
                    return vis;
            }

            return null;
        }

        protected IContainVisualisedElements? CurrentContainer;

        /// <summary>
        /// Moves this <see cref="VisualisedElement"/> to be contained by another target.
        /// </summary>
        /// <remarks>
        /// The <see cref="VisualisedElement"/> is first removed from its current container via <see cref="IContainVisualisedElements.RemoveVisualiser"/>,
        /// prior to being added to the new container via <see cref="IContainVisualisedElements.AddVisualiser"/>.
        /// </remarks>
        /// <param name="container">The target which should contain this <see cref="VisualisedElement"/>.</param>
        public void SetContainer(IContainVisualisedElements? container)
        {
            CurrentContainer?.RemoveVisualiser(this);

            // The visualised may have previously been within a container (e.g. flow), which repositioned it
            // We should make sure that the position is reset before it's added to another container
            Y = 0;

            container?.AddVisualiser(this);

            CurrentContainer = container;
        }

        protected void AddChild(object? element) => AddChildVisualiser(element);
        protected internal virtual VisualisedElement? AddChildVisualiser(object? element)
        {
            var vis = Tree.GetVisualiserFor(element);
            vis.SetContainer(this);
            return vis;
        }

        protected virtual void RemoveChild(Drawable drawable)
        {
            var vis = Tree.GetVisualiserFor(drawable);
            if (vis.CurrentContainer == this)
                vis.SetContainer(null);
        }

        public void Expand()
        {
            Flow.FadeIn();

            IsExpanded = true;
        }

        public void ExpandAll()
        {
            Expand();
            ((IEnumerable<VisualiserTreeNode>)this).ForEach(f => f.Expand());
        }

        public void Collapse()
        {
            Flow.FadeOut();
            UpdateSpecifics();

            IsExpanded = false;
        }

        protected bool IsExpanded = true;

        protected virtual void UpdateSpecifics()
        {
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (IsExpanded)
                Collapse();
            else
                Expand();
            return true;
        }

        IEnumerator<VisualiserTreeNode> IEnumerable<VisualiserTreeNode>.GetEnumerator() => GetEnumerator();

        public new IEnumerator<VisualiserTreeNode> GetEnumerator()
        {
            foreach (var child in Flow)
            {
                /*if (child is VisualisedElement el)
                    yield return el;
                else if (child is VisualiserSpacer sp)
                    foreach (var item in sp)
                        yield return item;*/
                yield return child;
            }
        }

        [return: NotNullIfNotNull(@"handle")]
        internal VisualisedDrawableFlow? SetHandle(TreeNodeHandle? handle) => Flow.SetHandle(handle);

        protected internal class VisualisedDrawableFlow : FillFlowContainer<VisualiserTreeNode>
        {
            public override IEnumerable<Drawable> FlowingChildren => AliveInternalChildren.Where(d => d.IsPresent).OrderBy(d => -d.Depth).ThenBy(d => (d as VisualisedDrawable)?.TargetDrawable?.ChildID ?? 0f);

            private TreeNodeHandle? currentHandle;

            public void EnsureFlowMutationAllowed()
            {
                if (currentHandle != null)
                    throw new InvalidOperationException("Mutation of the flow is not allowed");
            }

            [return: NotNullIfNotNull(@"handle")]
            internal VisualisedDrawableFlow? SetHandle(TreeNodeHandle? handle)
            {
                currentHandle = handle;
                return handle != null ? this : null;
            }
        }
    }

    internal class TreeNodeHandle : IDisposable
    {
        public VisualiserTreeNode Parent { get; private set; }
        public int ChildIndex { get; private set; }

        private VisualiserTreeNode.VisualisedDrawableFlow flow;

        public bool IsAtEnd => ChildIndex >= flow.Count;
        public VisualiserTreeNode Node
        {
            get
            {
                ensureNode();
                return flow[ChildIndex];
            }
        }
        public VisualisedElement? Visualiser => Node as VisualisedElement;

        private void ensureNode()
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Handle does not point at an item");
        }

        public TreeNodeHandle(VisualiserTreeNode parent, int index = 0)
        {
            flow = parent.SetHandle(this);
            Parent = parent;
            ChildIndex = index;
        }

        public void Add(object? obj)
        {
            mutateStart();
            var vis = Parent.AddChildVisualiser(obj);
            flow.SetLayoutPosition(vis, ChildIndex);
            ++ChildIndex;
            mutateStop();
        }

        public void Remove()
        {
            mutateStart();
            Node.SetContainer(null);
            mutateStop();
        }

        public void Replace(object? obj)
        {
            ensureNode();
            if (Visualiser != null && Visualiser.Target == obj)
                return;

            Remove();
            Add(obj);
        }

        public void RemoveAll()
        {
            while (!IsAtEnd)
                Remove();
        }

        public VisualiserTreeNode Detach()
        {
            mutateStart();
            var node = Node;
            node.SetContainer(null);
            mutateStop();
            return node;
        }

        public void Attach(VisualiserTreeNode node)
        {
            mutateStart();
            node.SetContainer(Parent);
            flow.SetLayoutPosition(node, ChildIndex);
            ++ChildIndex;
            mutateStop();
        }

        public void Advance(int diff = 1)
        {
            int index = ChildIndex + diff;
            if (index < 0 || index >= flow.Count)
                throw new ArgumentOutOfRangeException(nameof(diff));
            ChildIndex = index;
        }

        public void SetIndex(int index)
        {
            if (index < 0 || index >= flow.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            ChildIndex = index;
        }

        ~TreeNodeHandle() => Dispose();

        public void Dispose()
        {
            Parent?.SetHandle(null);
            Parent = null!;
        }

        private void mutateStart()
        {
            flow.SetHandle(null);
        }

        private void mutateStop()
        {
            flow.SetHandle(this);
        }
    }

    internal abstract class VisualisedElement : VisualiserTreeNode
    {
        public abstract object? Target { get; protected set; }

        protected SpriteText Text = null!;
        protected SpriteText Text2 = null!;
        protected Drawable PreviewBox = null!;
        protected Drawable? ActivityInvalidate;
        protected Drawable? ActivityAutosize;
        protected Drawable? ActivityLayout;

        protected Action<VisualisedElement>? OnAddVisualiser;
        protected Action<VisualisedElement>? OnRemoveVisualiser;

        public VisualisedElement(object? d)
        {
            Target = d;
        }

        protected virtual void LoadFrontDecorations()
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var spriteTarget = Target as Sprite;

            LoadFrontDecorations();

            AddRange(new Drawable[]
            {
                PreviewBox = spriteTarget?.Texture == null
                    ? new Box
                    {
                        Colour = Color4.White,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    }
                    : new Sprite
                    {
                        // It's fine to only bypass the ref count, because this sprite will dispose along with the original sprite
                        Texture = new Texture(spriteTarget.Texture.TextureGL),
                        Scale = new Vector2(spriteTarget.Texture.DisplayWidth / spriteTarget.Texture.DisplayHeight, 1),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(5),
                    Margin = new MarginPadding { Left = 2f },
                    Children = new Drawable[]
                    {
                        Text = new SpriteText { Font = FrameworkFont.Regular },
                        Text2 = new SpriteText { Font = FrameworkFont.Regular },
                    }
                },
            });

            PreviewBox.Size = new Vector2(LINE_HEIGHT, LINE_HEIGHT);

            UpdateSpecifics();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            UpdateChildren();
            UpdateColours();
        }

        public bool TopLevel
        {
            set => ConnectionContainer.Alpha = value ? 0 : 1;
        }

        protected abstract void UpdateChildren();

        public override void AddVisualiser(VisualiserTreeNode visualiser)
        {
            if (visualiser is VisualisedElement element)
            {
                OnAddVisualiser?.Invoke(element);
            }

            base.AddVisualiser(visualiser);
        }

        public override void RemoveVisualiser(VisualiserTreeNode visualiser)
        {
            if (visualiser is VisualisedElement element)
            {
                OnRemoveVisualiser?.Invoke(element);
            }

            base.RemoveVisualiser(visualiser);
        }

        protected override bool OnHover(HoverEvent e)
        {
            UpdateColours();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            UpdateColours();
            base.OnHoverLost(e);
        }

        protected override void UpdateColours()
        {
            base.UpdateColours();

            if (IsHighlighted)
            {
                Text.Colour = FrameworkColour.Blue;
                Text2.Colour = FrameworkColour.Blue;
            }
            else if (IsHovered)
            {
                Text.Colour = Color4.White;
                Text2.Colour = FrameworkColour.YellowGreen;
            }
            else
            {
                Text.Colour = Color4.White;
                Text2.Colour = FrameworkColour.YellowGreen;
            }
        }

        protected override void UpdateSpecifics()
        {
            Vector2 posInTree = ToSpaceOfOtherDrawable(Vector2.Zero, Tree);

            if (posInTree.Y < -PreviewBox.DrawHeight || posInTree.Y > Tree.DrawHeight)
            {
                Text.Text = string.Empty;
                Text2.Text = string.Empty;
                return;
            }

            UpdateContent();
        }

        protected abstract void UpdateContent();

        protected override void Update()
        {
            UpdateSpecifics();
            base.Update();
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == MouseButton.Right)
            {
                Tree.HighlightTarget(this);
                return true;
            }

            return false;
        }

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            Tree.RequestTarget(Target);
            return true;
        }
    }

    internal class VisualiserSpacer : VisualiserTreeNode
    {
        protected SpriteText Text = null!;

        public string SpacerText
        {
            get => Text?.Text.ToString() ?? string.Empty;
            set
            {
                if (Text != null)
                    Text.Text = value;
                else
                    Schedule(() => Text!.Text = value);
            }
        }

        public new VisualisedElement? Child
        {
            get => Flow.Child as VisualisedElement;
            set
            {
                Flow.EnsureFlowMutationAllowed();
                foreach (var child in Flow)
                    child.SetContainer(null);
                value?.SetContainer(this);
            }
        }

        public void Add(VisualisedElement element)
        {
            element.SetContainer(this);
        }

        public void Remove(VisualisedElement element)
        {
            if (!Flow.Contains(element))
                throw new InvalidOperationException("Target element is not in this container");
            element.SetContainer(null);
        }

        protected override bool OnDoubleClick(DoubleClickEvent e) => true;

        public VisualiserSpacer()
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    Text = new SpriteText {
                        Font = FrameworkFont.Regular,
                        Colour = FrameworkColour.Yellow,
                    },
                }
            });
        }
    }

    internal class VisualisedDrawable : VisualisedElement
    {
        public Drawable TargetDrawable { get; private set; } = null!;

        public override object? Target
        {
            get => TargetDrawable;
            protected set => TargetDrawable = (Drawable?)value ?? throw new ArgumentNullException(nameof(value));
        }

        public VisualisedDrawable(Drawable d)
            : base(d)
        {
            OnAddVisualiser += visualiser => visualiser.Depth = (visualiser as VisualisedDrawable)?.TargetDrawable?.Depth ?? 0;
        }

        protected override void LoadFrontDecorations()
        {
            Add(new FillFlowContainer
            {
                AutoSizeAxes = Axes.X,
                RelativeSizeAxes = Axes.Y,
                Spacing = new Vector2(1),
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    ActivityAutosize = new Box
                    {
                        Colour = Color4.Red,
                        Size = new Vector2(2, LINE_HEIGHT),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AlwaysPresent = true,
                        Alpha = 0
                    },
                    ActivityLayout = new Box
                    {
                        Colour = Color4.Orange,
                        Size = new Vector2(2, LINE_HEIGHT),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AlwaysPresent = true,
                        Alpha = 0
                    },
                    ActivityInvalidate = new Box
                    {
                        Colour = Color4.Yellow,
                        Size = new Vector2(2, LINE_HEIGHT),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AlwaysPresent = true,
                        Alpha = 0
                    },
                }
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            attachEvents();
        }


        private void attachEvents()
        {
            TargetDrawable.Invalidated += onInvalidated;
            TargetDrawable.OnDispose += onDispose;

            if (TargetDrawable is CompositeDrawable da)
            {
                da.OnAutoSize += onAutoSize;
                da.ChildBecameAlive += AddChild;
                da.ChildDied += RemoveChild;
                da.ChildDepthChanged += depthChanged;
            }

            if (TargetDrawable is FlowContainer<Drawable> df) df.OnLayout += onLayout;
        }

        private void detachEvents()
        {
            if (TargetDrawable != null)
            {
                TargetDrawable.Invalidated -= onInvalidated;
                TargetDrawable.OnDispose -= onDispose;
            }

            if (TargetDrawable is CompositeDrawable da)
            {
                da.OnAutoSize -= onAutoSize;
                da.ChildBecameAlive -= AddChild;
                da.ChildDied -= RemoveChild;
                da.ChildDepthChanged -= depthChanged;
            }

            if (TargetDrawable is FlowContainer<Drawable> df) df.OnLayout -= onLayout;
        }


        private void onAutoSize() => ActivityAutosize.FadeOutFromOne(1);

        private void onLayout() => ActivityLayout.FadeOutFromOne(1);

        private void onInvalidated(Drawable d) => ActivityInvalidate.FadeOutFromOne(1);

        private void depthChanged(Drawable drawable)
        {
            var vis = (VisualisedDrawable)Tree.GetVisualiserFor(drawable);

            vis.CurrentContainer?.RemoveVisualiser(vis);
            vis.CurrentContainer?.AddVisualiser(vis);
        }

        private void onDispose()
        {
            // May come from the disposal thread, in which case they won't ever be reused and the container doesn't need to be reset
            Schedule(() => SetContainer(null));
        }

        protected override void Dispose(bool isDisposing)
        {
            detachEvents();
            base.Dispose(isDisposing);
        }

        protected override void UpdateContent()
        {
            PreviewBox.Alpha = Math.Max(0.2f, TargetDrawable.Alpha);
            PreviewBox.Colour = TargetDrawable.Colour;

            int childCount = (TargetDrawable as CompositeDrawable)?.InternalChildren.Count ?? 0;

            Text.Text = TargetDrawable.ToString();
            Text2.Text = $"({TargetDrawable.DrawPosition.X:#,0},{TargetDrawable.DrawPosition.Y:#,0}) {TargetDrawable.DrawSize.X:#,0}x{TargetDrawable.DrawSize.Y:#,0}"
                            + (!IsExpanded && childCount > 0 ? $@" ({childCount} children)" : string.Empty);

            Alpha = (TargetDrawable?.IsPresent ?? true) ? 1f : 0.3f;
        }

        protected override void UpdateChildren()
        {
            if (TargetDrawable is CompositeDrawable compositeTarget)
                compositeTarget.AliveInternalChildren.ForEach(AddChild);
        }

        protected internal override VisualisedElement? AddChildVisualiser(object? element)
        {
            // Make sure to never add the DrawVisualiser (recursive scenario)
            // The user can still bypass it through inspecting properties
            // but there's not much we can do here.
            if (element == Tree.Visualiser) return null;

            // Don't add individual characters of SpriteText
            if (Target is SpriteText) return null;

            return base.AddChildVisualiser(element);
        }
    }

    internal class VisualisedObject : VisualisedElement
    {
        public override object? Target { get; protected set; }
        protected virtual Colour4 PreviewColour =>
            Target is Drawable
            ? Color4.White
            : Target is ValueType
            ? Color4.DarkViolet
            : Color4.Blue;

        public VisualisedObject(object? obj)
            : base(obj)
        {
        }

        protected override void UpdateContent()
        {
            PreviewBox.Alpha = 1;
            PreviewBox.Colour = PreviewColour;

            Text.Text = Target!.ToString()!;
            if (Target is IList arr)
            {
                Text2.Text = !IsExpanded ? $"({arr.Count} elements)" : string.Empty;
            }
            else
            {
                Text2.Text = string.Empty;
            }

            Alpha = 1;
        }

        protected override void UpdateChildren()
        {
        }

        public static VisualisedObject CreateFor(object? element)
        {
            if (element is null)
                return NullVisualiser.Instance;
            else if (element is IEnumerable en && !(element is string) && !(element is IDrawable))
                return VisualisedList.Create(en);
            else if (element is IBindable b)
                return new VisualisedBindable(b);
            else
                return new VisualisedObject(element);
        }

        private class NullVisualiser : VisualisedObject
        {
            private NullVisualiser()
                : base(null)
            {
            }

            public static NullVisualiser Instance => new NullVisualiser();

            protected override Colour4 PreviewColour => Colour4.Gray;
            protected override void UpdateContent()
            {
                PreviewBox.Alpha = 1;
                PreviewBox.Colour = PreviewColour;

                Text.Text = "<<NULL>>";
                Text2.Text = string.Empty;
            }

            protected override bool OnDoubleClick(DoubleClickEvent e) => true;
        }
    }

    internal class VisualisedList : VisualisedObject
    {
        public IEnumerable TargetList { get; private set; } = null!;

        private View targetView;

        public enum ViewKind
        {
            Frozen,
            Refreshable,
            Observable,
        }

        [NotNull]
        public override object? Target
        {
            get => TargetList;
            protected set => TargetList = (IEnumerable?)value ?? throw new ArgumentNullException(nameof(value));
        }

        public ViewKind Kind => targetView.Kind;
        public Type ElementType => targetView.ElementType;

        private VisualisedList(IEnumerable list, Type viewType)
            : base(list)
        {
            targetView = createListView(viewType);
        }

        private abstract class View
        {
            protected readonly VisualisedList ViewTarget;

            public abstract ViewKind Kind { get; }
            public abstract Type ElementType { get; }

            public View(VisualisedList target)
            {
                ViewTarget = target;
            }

            public abstract void UpdateChildren(object list);
        }

        private abstract class View<TList, TElem> : View where TList : class, IEnumerable
        {
            public View(VisualisedList target)
                : base(target)
            {
            }

            public override Type ElementType => typeof(TElem);

            public override void UpdateChildren(object list)
            {
                UpdateChildren((TList)(list));
            }

            protected abstract void UpdateChildren(TList list);

            protected TreeNodeHandle ChildAnchor(int startIndex = 0) => ViewTarget.createView(startIndex);

            protected void RemoveAll()
            {
                using (var a = ChildAnchor())
                    a.RemoveAll();
            }
        }

        private class FrozenView<T> : View<IEnumerable<T>, T>
        {
            public FrozenView(VisualisedList target)
                : base(target)
            {
            }

            public override ViewKind Kind => ViewKind.Frozen;

            protected override void UpdateChildren(IEnumerable<T> list)
            {
                using (var a = ChildAnchor())
                {
                    Debug.Assert(a.IsAtEnd);
                    foreach (var o in list)
                        a.Add(o);
                }
            }
        }

        private class RefreshableListView<T> : View<IReadOnlyList<T>, T>
        {
            public RefreshableListView(VisualisedList target)
                : base(target)
            {
            }

            public override ViewKind Kind => ViewKind.Refreshable;

            protected override void UpdateChildren(IReadOnlyList<T> list)
            {
                // TODO: do it better
                using (var a = ChildAnchor())
                {
                    a.RemoveAll();
                    foreach (var item in list)
                        a.Add(item);
                }
            }
        }

        private class SubscribableListView<T> : View<IBindableList<T>, T>
        {
            public SubscribableListView(VisualisedList target)
                : base(target)
            {
            }

            public override ViewKind Kind => ViewKind.Observable;

            protected override void UpdateChildren(IBindableList<T> list)
            {
                list.BindCollectionChanged(updateChildrenHandler, true);
            }

            private void updateChildrenHandler(object? sender, NotifyCollectionChangedEventArgs args)
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    {
                        addChildren(args.NewStartingIndex, args.NewItems!);
                        break;
                    }
                    case NotifyCollectionChangedAction.Remove:
                    {
                        removeChildren(args.OldStartingIndex, args.OldItems!);
                        break;
                    }
                    case NotifyCollectionChangedAction.Replace:
                    {
                        using (var handle = ChildAnchor(args.NewStartingIndex))
                            handle.Replace(args.NewItems![0]);
                        break;
                    }
                    case NotifyCollectionChangedAction.Move:
                    {
                        VisualiserTreeNode item;
                        using (var handle = ChildAnchor(args.OldStartingIndex))
                            item = handle.Detach();
                        using (var handle = ChildAnchor(args.NewStartingIndex))
                            handle.Attach(item);
                        break;
                    }
                    case NotifyCollectionChangedAction.Reset:
                    {
                        RemoveAll();
                        break;
                    }
                }
            }

            private void addChildren(int index, IList list)
            {
                using (var a = ChildAnchor(index))
                    foreach (object o in list)
                        a.Add(o);
            }

            private void removeChildren(int index, IList oldList)
            {
                if (index != -1)
                {
                    using (var a = ChildAnchor(index))
                        for (int idx = oldList.Count; idx != 0; --idx)
                            a.Remove();
                }
                else
                {
                    checkObjectType();
                    using (var a = ChildAnchor())
                        foreach (object old in oldList)
                        {
                            if (old is null)
                                throw new InvalidOperationException("Only non-null element removals may be observed");
                                // continue;
                            if (a.IsAtEnd)
                                throw new InvalidOperationException("Removed element not found");
                            while (a.Visualiser?.Target != old)
                            {
                                a.Advance();
                                if (a.IsAtEnd)
                                    throw new InvalidOperationException("Removed element not found");
                            }
                            a.Remove();
                        }
                }
            }

            private void checkObjectType()
            {
                if (default(T) != null)
                    throw new InvalidOperationException("List parameter type must be an object type");
            }
        }

        protected override void UpdateChildren() => targetView.UpdateChildren(TargetList);

        private TreeNodeHandle createView(int startIndex) => new TreeNodeHandle(this, startIndex);

        public static VisualisedList Create(IEnumerable list)
        {
            // TODO: This is dumb
            Type elemType = typeof(object);
            Type baseType = list.GetType();
            Type viewKind = typeof(FrozenView<>);
            foreach (var intType in baseType.FindInterfaces(
                (t, bas) => t.IsGenericType && ((Type[])bas!).Contains(t.GetGenericTypeDefinition()),
                new[] { typeof(IBindableList<>), typeof(IList<>), typeof(IReadOnlyList<>), typeof(IEnumerable<>), }))
            {
                elemType = intType.GetGenericArguments()[0];
                if (intType.GetGenericTypeDefinition() == typeof(IBindableList<>))
                {
                    viewKind = typeof(SubscribableListView<>);
                }
                else if (intType.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                {
                    viewKind = typeof(RefreshableListView<>);
                }
                break;
            }
            return new VisualisedList(list, viewKind.MakeGenericType(elemType));
        }

        private View createListView(Type viewType)
            => (View)Activator.CreateInstance(viewType, this)!;

        public static VisualisedList CreateFrozenView(IEnumerable list)
        {
            Type elemType = typeof(object);
            Type baseType = list.GetType();
            foreach (var intType in baseType.FindInterfaces((t, bas) => t.IsGenericType && t.GetGenericTypeDefinition() == (Type)bas!, typeof(IEnumerable<>)))
            {
                elemType = intType.GetGenericArguments()[0];
                break;
            }
            return new VisualisedList(list, typeof(FrozenView<>).MakeGenericType(elemType));
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (Kind == ViewKind.Refreshable)
            {
                Add(new BasicButton
                {
                    AutoSizeAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Text = "Refresh",
                    Action = UpdateChildren,
                });
            }
        }

        protected override Colour4 PreviewColour => Colour4.OrangeRed;

        protected override void UpdateContent()
        {
            base.UpdateContent();

            Text.Text = $"List of {targetView.ElementType}";
            Text2.Text = Kind.ToString();
        }
    }

    internal class VisualisedBindable : VisualisedObject
    {
        /// <summary>
        /// Prevents automatic unbinding from drawable unloader.
        /// </summary>
        public struct WrapBindable
        {
            public IBindable Value;
        }

        private WrapBindable targetBindable;
        public IBindable TargetBindable => targetBindable.Value;

        [NotNull]
        public override object? Target
        {
            get => TargetBindable;
            protected set => targetBindable.Value = (IBindable?)value ?? throw new ArgumentNullException(nameof(value));
        }

        private VisualiserSpacer valueContainer = null!;
        private VisualiserSpacer weakrefContainer = null!;
        public IBindableWrapper? LocalBindable;

        private bool inRootPosition => !(CurrentContainer is VisualisedElement);

        public VisualisedBindable(IBindable bindable)
            : base(bindable)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            (valueContainer = new VisualiserSpacer{ SpacerText = "Value" }).SetContainer(this);
            (weakrefContainer = new VisualiserSpacer{ SpacerText = "Bindings" }).SetContainer(this);
        }

        protected override void UpdateChildren()
        {
            if (LocalBindable == null)
            {
                var t = TargetBindable.GetType();
                while (t != null && (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(Bindable<>)))
                    t = t.BaseType;
                if (t == null)
                    throw new InvalidOperationException("Type does not inherit from Bindable");
                LocalBindable = (IBindableWrapper)Activator.CreateInstance(typeof(BindableWrapper<>).MakeGenericType(t.GetGenericArguments()[0]), new object[] { TargetBindable })!;
                valueContainer.Child = Tree.GetVisualiserFor(LocalBindable.Value);

                LocalBindable.BindValueChanged(newValue =>
                    valueContainer.Child = Tree.GetVisualiserFor(newValue));
            }
            if (inRootPosition)
            {
                foreach (var binding in LocalBindable.GetWeakReferences())
                    weakrefContainer.Add(new VisualisedWeakObject(binding));
            }
        }

        protected override Colour4 PreviewColour => Colour4.LimeGreen;

        protected override void UpdateContent()
        {
            base.UpdateContent();

            Text.Text = "Bound " + TargetBindable.GetType().ReadableName();
            Text2.Text = TargetBindable.ToString()!;
        }
        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (LocalBindable == null)
                return;

            foreach (var r in LocalBindable.GetWeakReferences())
            {
                bool isNew = true;
                foreach (var vis in weakrefContainer)
                {
                    if (vis is VisualisedWeakObject vr && vr.IsAlive && vr.Target == r)
                    {
                        isNew = false;
                        break;
                    }
                }
                if (isNew)
                    weakrefContainer.Add(new VisualisedWeakObject(r));
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            LocalBindable?.Unbind();
            LocalBindable = null!;
        }

        public interface IBindableWrapper
        {
            public IEnumerable<IBindable> GetWeakReferences();
            public void BindValueChanged(Action<object?> newValueAction);
            public void Unbind();
            public object? Value { get; }
        }

        private class BindableWrapper<T> : Bindable<T>, IBindableWrapper
        {
            private Bindable<T> originalBindable;
            private Lists.LockedWeakList<Bindable<T>>? targetBindings;

            public BindableWrapper(Bindable<T> bindable)
                : base()
            {
                originalBindable = bindable;
                BindTo(bindable);
            }

            public IEnumerable<IBindable> GetWeakReferences()
            {
                if (targetBindings == null)
                {
                    if (typeof(Bindable<T>).GetProperty("Bindings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is System.Reflection.PropertyInfo pBindings)
                    {
                        if (!(pBindings.GetValue(originalBindable) is Lists.LockedWeakList<Bindable<T>> l))
                        {
                            pBindings.SetValue(originalBindable, l = new Lists.LockedWeakList<Bindable<T>>());
                        }
                        targetBindings = l;
                    }
                }

                return targetBindings!.ToArray();
            }

            void IBindableWrapper.BindValueChanged(Action<object?> newValueAction)
            {
                BindValueChanged(v => newValueAction(v.NewValue));
            }

            public void Unbind() => UnbindAll();

            object? IBindableWrapper.Value => (object?)Value;
        }
    }

    internal class VisualisedWeakObject : VisualisedObject
    {
        public WeakReference TargetWeakRef { get; private set; } = null!;

        public new bool IsAlive => TargetWeakRef.IsAlive;

        [NotNull]
        public override object? Target
        {
            get => TargetWeakRef.Target ?? throw new InvalidOperationException("This visualiser holds a null reference");
            protected set => TargetWeakRef = new WeakReference(value ?? throw new ArgumentNullException(nameof(value)));
        }

        public VisualisedWeakObject(object obj)
            : base(obj)
        {}

        protected override void Update()
        {
            if (!IsAlive)
            {
                CurrentContainer?.RemoveVisualiser(this);
                return;
            }

            base.Update();
        }

        protected override Colour4 PreviewColour => Colour4.Brown;

        protected override void UpdateContent()
        {
            base.UpdateContent();

            Text.Text = "Weak " + Target.GetType().ReadableName();
            Text2.Text = Target.ToString()!;
        }
    }
}
