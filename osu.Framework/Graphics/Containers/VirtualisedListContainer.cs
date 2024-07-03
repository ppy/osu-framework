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
            pool = new DrawablePool<TDrawable>(initialPoolSize);

            InternalChildren = new Drawable[]
            {
                pool,
                Scroll = CreateScrollContainer().With(s =>
                {
                    s.RelativeSizeAxes = Axes.Both;
                    s.Child = Items = new ItemFlow(pool, rowHeight)
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
                            Items.Move(items[i], i + e.NewItems.Count);
                    }

                    for (int i = 0; i < e.NewItems.Count; ++i)
                        Items.Insert((TData)e.NewItems[i]!, Math.Max(e.NewStartingIndex, 0) + i);

                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    Debug.Assert(e.OldItems != null);

                    for (int i = 0; i < e.OldItems.Count; ++i)
                        Items.Remove(items[e.OldStartingIndex + i]);

                    for (int i = e.OldStartingIndex + e.OldItems.Count; i < items.Length; ++i)
                        Items.Move(items[i], i - e.OldItems.Count);

                    break;
                }

                case NotifyCollectionChangedAction.Replace:
                {
                    Items[e.OldStartingIndex].Row = (TData)e.NewItems![0]!;
                    break;
                }

                case NotifyCollectionChangedAction.Move:
                {
                    var allMoves = new List<(ItemRow, int)>();

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
                        Items.Move(item, newPosition);

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
            for (int i = 0; i < Items.Count; ++i)
            {
                TData expectedItem = RowData[i];
                TData actualItem = Items[i].Row;
                Debug.Assert(ReferenceEquals(expectedItem, actualItem), $"Data item mismatch at index {i} when handling changes in VirtualisedListContainer");

                float expectedY = i * rowHeight;
                float actualY = Items[i].Y;
                Debug.Assert(expectedY == actualY, $"Y mismatch at index {i} when handling changes in VirtualisedListContainer: expected {expectedY} actual {actualY}");
            }
        }

        protected override void Update()
        {
            base.Update();

            var currentVisibleRange = ((int)(Scroll.Current / rowHeight), (int)((Scroll.Current + Scroll.DrawHeight) / rowHeight) + 1);

            if (currentVisibleRange != visibleRange)
            {
                visibilityCache.Invalidate();
                visibleRange = currentVisibleRange;
            }

            if (!visibilityCache.IsValid)
            {
                foreach (var row in Items)
                {
                    if (!row.IsPresent)
                        continue;

                    float rowTop = row.Y;
                    float rowBottom = rowTop + rowHeight;

                    if (rowTop < visibleRange.min * rowHeight || rowBottom > visibleRange.max * rowHeight)
                    {
                        if (row.Visible)
                            row.Unload();
                    }
                    else
                    {
                        if (!row.Visible)
                            row.Load();
                    }
                }

                visibilityCache.Validate();
                assertCorrectOrder();
            }
        }

        protected partial class ItemFlow : Container<ItemRow>
        {
            private readonly DrawablePool<TDrawable> pool;
            private readonly float rowHeight;

            public ItemFlow(DrawablePool<TDrawable> pool, float rowHeight)
            {
                this.pool = pool;
                this.rowHeight = rowHeight;
            }

            public IEnumerable<ItemRow> FlowingChildren => Children.Where(d => d.IsPresent);

            public void Insert(TData row, int index)
            {
                Add(new ItemRow(row, pool)
                {
                    Height = rowHeight,
                    LifetimeEnd = double.NegativeInfinity,
                    // the depth management is mostly done so that enumeration order of `Children` matches expectations.
                    // in edge cases it could also ensure correct Z-ordering of children, but it's a secondary consideration.
                    Depth = -index,
                    Y = index * rowHeight
                });
            }

            public void Remove(ItemRow itemRow)
            {
                itemRow.Unload();
                Remove(itemRow, true);
            }

            public void Move(ItemRow itemRow, int newIndex)
            {
                itemRow.Y = newIndex * rowHeight;
                ChangeChildDepth(itemRow, -newIndex);
            }
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
