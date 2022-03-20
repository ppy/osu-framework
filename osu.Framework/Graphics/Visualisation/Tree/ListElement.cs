// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// Represents a list/enumerable and its children in the graph.
    /// </summary>
    internal class ListElement : ObjectElement
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

        private ListElement(IEnumerable list, Type viewType)
            : base(list)
        {
            targetView = createListView(viewType);
        }

        private abstract class View
        {
            protected readonly ListElement ViewTarget;

            public abstract ViewKind Kind { get; }
            public abstract Type ElementType { get; }

            public View(ListElement target)
            {
                ViewTarget = target;
            }

            public abstract void UpdateChildren(object list);
        }

        private abstract class View<TList, TElem> : View where TList : class, IEnumerable
        {
            public View(ListElement target)
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
            public FrozenView(ListElement target)
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
            public RefreshableListView(ListElement target)
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
            public SubscribableListView(ListElement target)
                : base(target)
            {
            }

            public override ViewKind Kind => ViewKind.Observable;

            protected override void UpdateChildren(IBindableList<T> list)
            {
                list.BindCollectionChanged(updateChildrenHandler, true);
            }

            private int indexOrFirst(int index)
                => index != -1 ? index : 0;

            private int ensureValidIndex(int index)
                => index != -1 ? index : throw new ArgumentOutOfRangeException(nameof(index), "Index must be set");

            private void updateChildrenHandler(object? sender, NotifyCollectionChangedEventArgs args)
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    {
                        addChildren(indexOrFirst(args.NewStartingIndex), args.NewItems!);
                        break;
                    }
                    case NotifyCollectionChangedAction.Remove:
                    {
                        removeChildren(ensureValidIndex(args.OldStartingIndex), args.OldItems!);
                        break;
                    }
                    case NotifyCollectionChangedAction.Replace:
                    {
                        using (var handle = ChildAnchor(ensureValidIndex(args.NewStartingIndex)))
                            handle.Replace(args.NewItems![0]);
                        break;
                    }
                    case NotifyCollectionChangedAction.Move:
                    {
                        TreeNode item;
                        using (var handle = ChildAnchor(ensureValidIndex(args.OldStartingIndex)))
                            item = handle.Detach();
                        using (var handle = ChildAnchor(ensureValidIndex(args.NewStartingIndex)))
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

        public static ListElement Create(IEnumerable list)
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
            return new ListElement(list, viewKind.MakeGenericType(elemType));
        }

        private View createListView(Type viewType)
            => (View)Activator.CreateInstance(viewType, this)!;

        public static ListElement CreateFrozenView(IEnumerable list)
        {
            Type elemType = typeof(object);
            Type baseType = list.GetType();
            foreach (var intType in baseType.FindInterfaces((t, bas) => t.IsGenericType && t.GetGenericTypeDefinition() == (Type)bas!, typeof(IEnumerable<>)))
            {
                elemType = intType.GetGenericArguments()[0];
                break;
            }
            return new ListElement(list, typeof(FrozenView<>).MakeGenericType(elemType));
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
}
