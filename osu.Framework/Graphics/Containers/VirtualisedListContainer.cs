// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.UserInterface;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// This class is an alternative to combined <see cref="ScrollContainer{T}"/> / <see cref="FlowContainer{T}"/> usage
    /// for large lists (think thousands of items).
    /// This class efficiently handles collection change events, as well as applies pooling to only allocate drawables for what is actually visible
    /// on screen.
    /// The trade-off is that every row has to have an estimated or fixed height so that layout can be estimated cheaply pre-emptively.
    /// </summary>
    public abstract partial class VirtualisedListContainer<TData, TDrawable> : CompositeDrawable
        where TData : notnull
        where TDrawable : PoolableDrawable, IHasCurrentValue<TData>, new()
    {
        public BindableList<TData> RowData { get; } = new BindableList<TData>();

        protected ScrollContainer<Drawable> Scroll { get; private set; } = null!;
        protected ItemFlow Items { get; private set; } = null!;

        private readonly float rowHeight;
        private readonly int initialPoolSize;

        private (int min, int max) visibleRange;
        private readonly Cached visibilityCache = new Cached();

        private DrawablePool<TDrawable> pool = null!;

        protected VirtualisedListContainer(float rowHeight, int initialPoolSize)
        {
            this.rowHeight = rowHeight;
            this.initialPoolSize = initialPoolSize;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                pool = new DrawablePool<TDrawable>(initialPoolSize),
                Scroll = CreateScrollContainer().With(s =>
                {
                    s.RelativeSizeAxes = Axes.Both;
                    s.Child = Items = new ItemFlow
                    {
                        RelativeSizeAxes = Axes.X
                    };
                })
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            RowData.BindCollectionChanged(itemsChanged, true);
        }

        private void itemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            visibilityCache.Invalidate();
            Items.Height = rowHeight * RowData.Count;

            var items = Items.FlowingChildren.ToArray();

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    Debug.Assert(e.NewItems != null);

                    if (e.NewStartingIndex >= 0)
                    {
                        for (int i = e.NewStartingIndex; i < items.Length; ++i)
                        {
                            var itemToMove = Items[Items.IndexOf(items[i])];
                            itemToMove.Y = (i + e.NewItems.Count) * rowHeight;
                            Items.ChangeChildDepth(itemToMove, -(i + e.NewItems.Count));
                        }
                    }

                    for (int i = 0; i < e.NewItems.Count; ++i)
                    {
                        Items.Add(new ItemRow((TData)e.NewItems[i]!, pool)
                        {
                            Height = rowHeight,
                            LifetimeEnd = double.NegativeInfinity,
                            Depth = -(Math.Max(e.NewStartingIndex, 0) + i),
                            Y = (Math.Max(e.NewStartingIndex, 0) + i) * rowHeight
                        });
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    Debug.Assert(e.OldItems != null);

                    for (int i = 0; i < e.OldItems.Count; ++i)
                    {
                        var itemToRemove = Items[Items.IndexOf(items[e.OldStartingIndex + i])];
                        itemToRemove.Unload();
                        Items.Remove(itemToRemove, true);
                    }

                    for (int i = e.OldStartingIndex + e.OldItems.Count; i < items.Length; ++i)
                    {
                        var itemToMove = Items[Items.IndexOf(items[i])];
                        itemToMove.Y = (i - e.OldItems.Count) * rowHeight;
                        Items.ChangeChildDepth(itemToMove, -(i - e.OldItems.Count));
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Replace:
                {
                    Items[Items.IndexOf(items[e.OldStartingIndex])].Row = (TData)e.NewItems![0]!;
                    break;
                }

                case NotifyCollectionChangedAction.Move:
                {
                    var allMoves = new List<(ItemRow, float)>();

                    for (int i = Math.Min(e.OldStartingIndex, e.NewStartingIndex); i <= Math.Max(e.OldStartingIndex, e.NewStartingIndex); ++i)
                    {
                        if (i == e.OldStartingIndex)
                            allMoves.Add((items[i], e.NewStartingIndex));
                        else if (e.NewStartingIndex < e.OldStartingIndex)
                            allMoves.Add((items[i], i + 1));
                        else
                            allMoves.Add((items[i], i - 1));
                    }

                    foreach (var (item, newPosition) in allMoves)
                    {
                        item.Y = newPosition * rowHeight;
                        Items.ChangeChildDepth(item, -newPosition);
                    }

                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                {
                    Items.Clear();
                    break;
                }
            }

            assertCorrectOrder();
        }

        [Conditional("DEBUG")]
        private void assertCorrectOrder()
        {
            var children = Items.FlowingChildren.ToArray();

            for (int i = 0; i < children.Length; ++i)
            {
                float expected = i;
                float? actual = -children[i].Depth;
                Debug.Assert(expected == actual, $"Index mismatch when handling collection change callback in VirtualisedListContainer: expected {expected} actual {actual}");
            }
        }

        protected override void Update()
        {
            base.Update();

            var currentVisibleRange = ((int)(Scroll.Current / rowHeight) + 1, (int)((Scroll.Current + Scroll.DrawHeight) / rowHeight));

            if (currentVisibleRange != visibleRange)
            {
                visibilityCache.Invalidate();
                visibleRange = currentVisibleRange;
            }

            if (!visibilityCache.IsValid)
            {
                for (int i = 0; i < Items.Count; ++i)
                {
                    var row = Items[i];

                    if (!row.IsPresent)
                        continue;

                    float minY = row.IsLoaded ? row.Y : -row.Depth * rowHeight;
                    float maxY = minY + rowHeight;

                    if ((maxY < visibleRange.min * rowHeight || minY > visibleRange.max * rowHeight) && row.Visible)
                        row.Unload();

                    if ((maxY >= visibleRange.min * rowHeight && minY <= visibleRange.max * rowHeight) && !row.Visible)
                        row.Load();
                }

                visibilityCache.Validate();
                assertCorrectOrder();
            }
        }

        protected partial class ItemFlow : Container<ItemRow>
        {
            public IEnumerable<ItemRow> FlowingChildren => Children.Where(d => d.IsPresent);
        }

        protected partial class ItemRow : CompositeDrawable
        {
            public override bool RemoveWhenNotAlive => false;
            public override bool DisposeOnDeathRemoval => false;

            private TData row;

            public TData Row
            {
                get => row;
                [MemberNotNull(nameof(row))]
                set
                {
                    row = value;
                    if (InternalChildren.SingleOrDefault() is TDrawable rowDrawable)
                        rowDrawable.Current.Value = value;
                }
            }

            public bool Visible { get; private set; }

            private readonly DrawablePool<TDrawable> pool;

            public ItemRow(TData row, DrawablePool<TDrawable> pool)
            {
                Row = row;
                this.pool = pool;
                RelativeSizeAxes = Axes.X;

                // to avoid overheads from input handling (or, to be more specific, from constructing input queues),
                // the row manages its own lifetime.
                // if the row is not alive, it is not in `AliveInternalChildren` of its parent,
                // which means that it is omitted from consideration in `Build(Non)PositionalInputQueue()` et al.
                LifetimeEnd = double.NegativeInfinity;
            }

            public void Load()
            {
                InternalChild = pool.Get(d => d.Current.Value = Row);
                Visible = true;
                LifetimeEnd = double.PositiveInfinity;
            }

            public void Unload()
            {
                ClearInternal(false);
                Visible = false;
                LifetimeEnd = double.NegativeInfinity;
            }
        }

        protected abstract ScrollContainer<Drawable> CreateScrollContainer();
    }
}
