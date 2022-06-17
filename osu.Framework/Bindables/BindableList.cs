// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Lists;

namespace osu.Framework.Bindables
{
    public class BindableList<T> : IBindableList<T>, IBindable, IParseable, IList<T>, IList
    {
        /// <summary>
        /// An event which is raised when this <see cref="BindableList{T}"/> changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed (or manually via <see cref="triggerDisabledChange(bool)"/>).
        /// </summary>
        public event Action<bool> DisabledChanged;

        private readonly List<T> collection = new List<T>();

        private readonly Cached<WeakReference<BindableList<T>>> weakReferenceCache = new Cached<WeakReference<BindableList<T>>>();

        private WeakReference<BindableList<T>> weakReference => weakReferenceCache.IsValid ? weakReferenceCache.Value : weakReferenceCache.Value = new WeakReference<BindableList<T>>(this);

        private LockedWeakList<BindableList<T>> bindings;

        /// <summary>
        /// Creates a new <see cref="BindableList{T}"/>, optionally adding the items of the given collection.
        /// </summary>
        /// <param name="items">The items that are going to be contained in the newly created <see cref="BindableList{T}"/>.</param>
        public BindableList(IEnumerable<T> items = null)
        {
            if (items != null)
                collection.AddRange(items);
        }

        #region IList<T>

        /// <summary>
        /// Gets or sets the item at an index in this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <exception cref="InvalidOperationException">Thrown when setting a value while this <see cref="BindableList{T}"/> is <see cref="Disabled"/>.</exception>
        public T this[int index]
        {
            get => collection[index];
            set => setIndex(index, value, null);
        }

        private void setIndex(int index, T item, BindableList<T> caller)
        {
            ensureMutationAllowed();

            T lastItem = collection[index];

            collection[index] = item;

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.setIndex(index, item, this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, lastItem, index));
        }

        /// <summary>
        /// Adds a single item to this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <exception cref="InvalidOperationException">Thrown when this <see cref="BindableList{T}"/> is <see cref="Disabled"/>.</exception>
        public void Add(T item)
            => add(item, null);

        private void add(T item, BindableList<T> caller)
        {
            ensureMutationAllowed();

            collection.Add(item);

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.add(item, this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, collection.Count - 1));
        }

        /// <summary>
        /// Retrieves the index of an item in this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="item">The item to retrieve the index of.</param>
        /// <returns>The index of the item, or -1 if the item isn't in this <see cref="BindableList{T}"/>.</returns>
        public int IndexOf(T item) => collection.IndexOf(item);

        /// <summary>
        /// Inserts an item at the specified index in this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="item">The item to insert.</param>
        /// <exception cref="InvalidOperationException">Thrown when this <see cref="BindableList{T}"/> is <see cref="Disabled"/>.</exception>
        public void Insert(int index, T item)
            => insert(index, item, null);

        private void insert(int index, T item, BindableList<T> caller)
        {
            ensureMutationAllowed();

            collection.Insert(index, item);

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.insert(index, item, this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        /// <summary>
        /// Clears the contents of this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when this <see cref="BindableList{T}"/> is <see cref="Disabled"/>.</exception>
        public void Clear()
            => clear(null);

        private void clear(BindableList<T> caller)
        {
            ensureMutationAllowed();

            if (collection.Count <= 0)
                return;

            // Preserve items for subscribers
            var clearedItems = collection.ToList();

            collection.Clear();

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.clear(this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, clearedItems, 0));
        }

        /// <summary>
        /// Determines if an item is in this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="item">The item to locate in this <see cref="BindableList{T}"/>.</param>
        /// <returns><code>true</code> if this <see cref="BindableList{T}"/> contains the given item.</returns>
        public bool Contains(T item)
            => collection.Contains(item);

        /// <summary>
        /// Removes an item from this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="item">The item to remove from this <see cref="BindableList{T}"/>.</param>
        /// <returns><code>true</code> if the removal was successful.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="BindableList{T}"/> is <see cref="Disabled"/>.</exception>
        public bool Remove(T item)
            => remove(item, null);

        private bool remove(T item, BindableList<T> caller)
        {
            ensureMutationAllowed();

            int index = collection.IndexOf(item);

            if (index < 0)
                return false;

            // Removal may have come from an equality comparison.
            // Always return the original reference from the list to other bindings and events.
            var listItem = collection[index];

            collection.RemoveAt(index);

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-removing from the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.remove(listItem, this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, listItem, index));

            return true;
        }

        /// <summary>
        /// Removes <paramref name="count"/> items starting from <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index to start removing from.</param>
        /// <param name="count">The count of items to be removed.</param>
        public void RemoveRange(int index, int count)
        {
            removeRange(index, count, null);
        }

        private void removeRange(int index, int count, BindableList<T> caller)
        {
            ensureMutationAllowed();

            var removedItems = collection.GetRange(index, count);

            collection.RemoveRange(index, count);

            if (removedItems.Count == 0)
                return;

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // Prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.removeRange(index, count, this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, index));
        }

        /// <summary>
        /// Removes an item at the specified index from this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="BindableList{T}"/> is <see cref="Disabled"/>.</exception>
        public void RemoveAt(int index)
            => removeAt(index, null);

        private void removeAt(int index, BindableList<T> caller)
        {
            ensureMutationAllowed();

            T item = collection[index];

            collection.RemoveAt(index);

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.removeAt(index, this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        /// <summary>
        /// Removes all items from this <see cref="BindableList{T}"/> that match a predicate.
        /// </summary>
        /// <param name="match">The predicate.</param>
        public int RemoveAll(Predicate<T> match)
            => removeAll(match, null);

        private int removeAll(Predicate<T> match, BindableList<T> caller)
        {
            ensureMutationAllowed();

            var removed = collection.FindAll(match);

            if (removed.Count == 0) return removed.Count;

            // RemoveAll is internally optimised
            collection.RemoveAll(match);

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.removeAll(match, this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed));

            return removed.Count;
        }

        /// <summary>
        /// Copies the contents of this <see cref="BindableList{T}"/> to the given array, starting at the given index.
        /// </summary>
        /// <param name="array">The array that is the destination of the items copied from this <see cref="BindableList{T}"/>.</param>
        /// <param name="arrayIndex">The index at which the copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
            => collection.CopyTo(array, arrayIndex);

        /// <summary>
        /// Copies the contents of this <see cref="BindableList{T}"/> to the given array, starting at the given index.
        /// </summary>
        /// <param name="array">The array that is the destination of the items copied from this <see cref="BindableList{T}"/>.</param>
        /// <param name="index">The index at which the copying begins.</param>
        public void CopyTo(Array array, int index)
            => ((ICollection)collection).CopyTo(array, index);

        public int BinarySearch(T item) => collection.BinarySearch(item);

        public int Count => collection.Count;
        public bool IsSynchronized => ((ICollection)collection).IsSynchronized;
        public object SyncRoot => ((ICollection)collection).SyncRoot;
        public bool IsReadOnly => Disabled;

        #endregion

        #region IList

        object IList.this[int index]
        {
            get => this[index];
            set => this[index] = (T)value;
        }

        int IList.Add(object value)
        {
            Add((T)value);
            return Count - 1;
        }

        bool IList.Contains(object value) => Contains((T)value);

        int IList.IndexOf(object value) => IndexOf((T)value);

        void IList.Insert(int index, object value) => Insert(index, (T)value);

        void IList.Remove(object value) => Remove((T)value);

        bool IList.IsFixedSize => false;

        #endregion

        #region IParseable

        /// <summary>
        /// Parse an object into this instance.
        /// A collection holding items of type <typeparamref name="T"/> can be parsed. Null results in an empty <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="BindableList{T}"/> is <see cref="Disabled"/>.</exception>
        public void Parse(object input)
        {
            ensureMutationAllowed();

            switch (input)
            {
                case null:
                    Clear();
                    break;

                case IEnumerable<T> enumerable:
                    // enumerate once locally before proceeding.
                    var newItems = enumerable.ToList();

                    if (this.SequenceEqual(newItems))
                        return;

                    Clear();
                    AddRange(newItems);
                    break;

                default:
                    throw new ArgumentException($@"Could not parse provided {input.GetType()} ({input}) to {typeof(T)}.");
            }
        }

        #endregion

        #region ICanBeDisabled

        private bool disabled;

        /// <summary>
        /// Whether this <see cref="BindableList{T}"/> has been disabled. When disabled, attempting to change the contents of this <see cref="BindableList{T}"/> will result in an <see cref="InvalidOperationException"/>.
        /// </summary>
        public bool Disabled
        {
            get => disabled;
            set
            {
                if (value == disabled)
                    return;

                disabled = value;

                triggerDisabledChange();
            }
        }

        public void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false)
        {
            DisabledChanged += onChange;
            if (runOnceImmediately)
                onChange(Disabled);
        }

        private void triggerDisabledChange(bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            bool beforePropagation = disabled;

            if (propagateToBindings && bindings != null)
            {
                foreach (var b in bindings)
                    b.Disabled = disabled;
            }

            if (beforePropagation == disabled)
                DisabledChanged?.Invoke(disabled);
        }

        #endregion ICanBeDisabled

        #region IUnbindable

        public virtual void UnbindEvents()
        {
            CollectionChanged = null;
            DisabledChanged = null;
        }

        public void UnbindBindings()
        {
            if (bindings == null)
                return;

            foreach (var b in bindings)
                UnbindFrom(b);
        }

        public void UnbindAll()
        {
            UnbindEvents();
            UnbindBindings();
        }

        public virtual void UnbindFrom(IUnbindable them)
        {
            if (!(them is BindableList<T> tThem))
                throw new InvalidCastException($"Can't unbind a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            removeWeakReference(tThem.weakReference);
            tThem.removeWeakReference(weakReference);
        }

        #endregion IUnbindable

        #region IHasDescription

        public string Description { get; set; }

        #endregion IHasDescription

        #region IBindableCollection

        /// <summary>
        /// Adds a collection of items to this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="items">The collection whose items should be added to this collection.</param>
        /// <exception cref="InvalidOperationException">Thrown if this collection is <see cref="Disabled"/></exception>
        public void AddRange(IEnumerable<T> items)
            => addRange(items as IList ?? items.ToArray(), null);

        private void addRange(IList items, BindableList<T> caller)
        {
            ensureMutationAllowed();

            collection.AddRange(items.Cast<T>());

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.addRange(items, this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items, collection.Count - items.Count));
        }

        /// <summary>
        /// Moves an item in this collection.
        /// </summary>
        /// <param name="oldIndex">The index of the item to move.</param>
        /// <param name="newIndex">The index specifying the new location of the item.</param>
        public void Move(int oldIndex, int newIndex)
            => move(oldIndex, newIndex, null);

        private void move(int oldIndex, int newIndex, BindableList<T> caller)
        {
            ensureMutationAllowed();

            T item = collection[oldIndex];

            collection.RemoveAt(oldIndex);
            collection.Insert(newIndex, item);

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.move(oldIndex, newIndex, this);
                }
            }

            notifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, newIndex, oldIndex));
        }

        void IBindable.BindTo(IBindable them)
        {
            if (!(them is BindableList<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        void IBindableList<T>.BindTo(IBindableList<T> them)
        {
            if (!(them is BindableList<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        public IBindableList<T> BindTarget
        {
            set => ((IBindableList<T>)this).BindTo(value);
        }

        /// <summary>
        /// Binds this <see cref="BindableList{T}"/> to another.
        /// </summary>
        /// <param name="them">The <see cref="BindableList{T}"/> to be bound to.</param>
        public void BindTo(BindableList<T> them)
        {
            if (them == null)
                throw new ArgumentNullException(nameof(them));
            if (bindings?.Contains(weakReference) == true)
                throw new ArgumentException("An already bound collection can not be bound again.");
            if (them == this)
                throw new ArgumentException("A collection can not be bound to itself");

            // copy state and content over
            Parse(them);
            Disabled = them.Disabled;

            addWeakReference(them.weakReference);
            them.addWeakReference(weakReference);
        }

        /// <summary>
        /// Bind an action to <see cref="CollectionChanged"/> with the option of running the bound action once immediately
        /// with an <see cref="NotifyCollectionChangedAction.Add"/> event for the entire contents of this <see cref="BindableList{T}"/>.
        /// </summary>
        /// <param name="onChange">The action to perform when this <see cref="BindableList{T}"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <paramref name="onChange"/> should be run once immediately.</param>
        public void BindCollectionChanged(NotifyCollectionChangedEventHandler onChange, bool runOnceImmediately = false)
        {
            CollectionChanged += onChange;
            if (runOnceImmediately)
                onChange(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection));
        }

        private void addWeakReference(WeakReference<BindableList<T>> weakReference)
        {
            bindings ??= new LockedWeakList<BindableList<T>>();
            bindings.Add(weakReference);
        }

        private void removeWeakReference(WeakReference<BindableList<T>> weakReference) => bindings?.Remove(weakReference);

        IBindable IBindable.CreateInstance() => CreateInstance();

        /// <inheritdoc cref="IBindable.CreateInstance"/>
        protected virtual BindableList<T> CreateInstance() => new BindableList<T>();

        IBindable IBindable.GetBoundCopy() => GetBoundCopy();

        IBindableList<T> IBindableList<T>.GetBoundCopy() => GetBoundCopy();

        /// <inheritdoc cref="IBindableList{T}.GetBoundCopy"/>
        public BindableList<T> GetBoundCopy() => IBindable.GetBoundCopyImplementation(this);

        #endregion IBindableCollection

        #region IEnumerable

        public List<T>.Enumerator GetEnumerator() => collection.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable

        private void notifyCollectionChanged(NotifyCollectionChangedEventArgs args) => CollectionChanged?.Invoke(this, args);

        private void ensureMutationAllowed()
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot mutate the {nameof(BindableList<T>)} while it is disabled.");
        }

        public bool IsDefault => Count == 0;
    }
}
