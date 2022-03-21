// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Input.Events;

using osuTK;
using osuTK.Graphics;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// A node in the visualisation graph. Can contain nested nodes.
    /// </summary>
    internal abstract class TreeNode : Container, INodeContainer, IEnumerable<TreeNode>
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

        public virtual void AddVisualiser(TreeNode visualiser)
        {
            Flow.EnsureFlowMutationAllowed();
            Flow.Add(visualiser);
        }

        public virtual void RemoveVisualiser(TreeNode visualiser)
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

        protected INodeContainer? CurrentContainer;

        /// <summary>
        /// Moves this <see cref="ElementNode"/> to be contained by another target.
        /// </summary>
        /// <remarks>
        /// The <see cref="ElementNode"/> is first removed from its current container via <see cref="INodeContainer.RemoveVisualiser"/>,
        /// prior to being added to the new container via <see cref="INodeContainer.AddVisualiser"/>.
        /// </remarks>
        /// <param name="container">The target which should contain this <see cref="ElementNode"/>.</param>
        public void SetContainer(INodeContainer? container)
        {
            CurrentContainer?.RemoveVisualiser(this);

            // The visualised may have previously been within a container (e.g. flow), which repositioned it
            // We should make sure that the position is reset before it's added to another container
            Y = 0;

            container?.AddVisualiser(this);

            CurrentContainer = container;
        }

        protected void AddChild(object? element) => AddChildVisualiser(element);
        protected internal virtual ElementNode? AddChildVisualiser(object? element)
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
            ((IEnumerable<TreeNode>)this).ForEach(f => f.Expand());
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

        IEnumerator<TreeNode> IEnumerable<TreeNode>.GetEnumerator() => GetEnumerator();

        public new IEnumerator<TreeNode> GetEnumerator()
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

        protected internal class VisualisedDrawableFlow : FillFlowContainer<TreeNode>
        {
            public override IEnumerable<Drawable> FlowingChildren
                => from d in AliveInternalChildren
                where d.IsPresent
                orderby -d.Depth, (d as VisualisedDrawable)?.TargetDrawable?.ChildID ?? 0
                select d;

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

    /// <summary>
    /// A handle to a <see cref="TreeNode"/>'s children.
    /// Used for dynamic manipulation of the tree.
    /// </summary>
    /// <remarks>
    /// This class is mostly used by nodes' internal use, so it's not
    /// recommended to explicitly modify another node's children.
    /// </remarks>
    internal class TreeNodeHandle : IDisposable
    {
        public TreeNode Parent { get; private set; }
        public int ChildIndex { get; private set; }

        private TreeNode.VisualisedDrawableFlow flow;

        public bool IsAtEnd => ChildIndex >= flow.Count;
        public TreeNode Node
        {
            get
            {
                ensureNode();
                return flow[ChildIndex];
            }
        }
        public ElementNode? Visualiser => Node as ElementNode;

        public TreeNodeHandle(TreeNode parent, int index = 0)
        {
            flow = parent.SetHandle(this);
            Parent = parent;
            ChildIndex = index;
        }

        public void Add(object? obj)
        {
            using (mutate())
            {
                var vis = Parent.AddChildVisualiser(obj);
                if (vis == null)
                    return;

                insert(vis);
            }
        }

        public void Remove()
        {
            using (mutate())
            {
                Node.SetContainer(null);
            }
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

        public TreeNode Detach()
        {
            using (mutate())
            {
                var node = Node;
                node.SetContainer(null);
                return node;
            }
        }

        public void Attach(TreeNode node)
        {
            using (mutate())
            {
                node.SetContainer(Parent);
                insert(node);
            }
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

        private void ensureNode()
        {
            if (IsAtEnd)
                throw new InvalidOperationException("Handle does not point at an item");
        }

        private void insert(TreeNode vis)
        {
            //Logger.Log($"Current depth: {vis.Depth}", level: LogLevel.Debug);
            //Logger.Log($"Target depth: {-ChildIndex}", level: LogLevel.Debug);
            flow.ChangeChildDepth(vis, 1);
            if (ChildIndex != 0)
            {
                for (int it = 1, end = ChildIndex; it != end; ++it)
                    flow.ChangeChildDepth(flow[it], 1-it);
                for (int it = ChildIndex, end = flow.Count; it != end; ++it)
                    flow.ChangeChildDepth(flow[it], -it);
            }
            flow.ChangeChildDepth(vis, -ChildIndex);
            //Logger.Log($"Depth after: {vis.Depth}", level: LogLevel.Debug);
            ++ChildIndex;
        }

        private void mutateStart()
        {
            flow.SetHandle(null);
        }

        private void mutateStop()
        {
            flow.SetHandle(this);
        }

        private MutateHandle mutate() => new MutateHandle(this);

        private ref struct MutateHandle
        {
            private TreeNodeHandle handle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public MutateHandle(TreeNodeHandle _handle)
            {
                handle = _handle;
                handle.mutateStart();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                handle.mutateStop();
            }
        }
    }
}
