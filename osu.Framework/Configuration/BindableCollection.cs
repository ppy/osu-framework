// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Lists;

namespace osu.Framework.Configuration
{
    public class BindableCollection<T> : IBindableCollection<T>, ICollection<T>, ICollection, IParseable, IHasDescription
    {
        // list allows to use methods like AddRange.
        private readonly List<T> collection = new List<T>();

        private readonly WeakReference<BindableCollection<T>> weakReference;

        private WeakList<BindableCollection<T>> bindings;

        /// <summary>
        /// An event which is raised when any items are added to this <see cref="BindableCollection{T}"/>.
        /// </summary>
        public event Action<IEnumerable<T>> ItemsAdded;

        /// <summary>
        /// An event which is raised when any items are removed from this <see cref="BindableCollection{T}"/>.
        /// </summary>
        public event Action<IEnumerable<T>> ItemsRemoved;

        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed (or manually via <see cref="triggerDisabledChange(bool)"/>).
        /// </summary>
        public event Action<bool> DisabledChanged;

        /// <summary>
        /// Creates a new <see cref="BindableCollection{T}"/>, optionally adding the items of the given collection.
        /// </summary>
        /// <param name="items">The items that are going to be contained in the newly created <see cref="BindableCollection{T}"/>.</param>
        public BindableCollection(IEnumerable<T> items = null)
        {
            if (items != null)
                collection.AddRange(items);

            // we can not initialize this directly at the property due to the this capture.
            weakReference = new WeakReference<BindableCollection<T>>(this);
        }

        /// <summary>
        /// Gets or sets the item at an index in this <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <exception cref="InvalidOperationException">Thrown when setting a value while this <see cref="BindableCollection{T}"/> is <see cref="Disabled"/>.</exception>
        public T this[int index]
        {
            get => collection[index];
            set => setIndex(index, value, null);
        }

        private void setIndex(int index, T item, BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot change items while the {nameof(BindableCollection<T>)} is disabled.");

            T lastItem = collection[index];

            collection[index] = item;

            bindings?.ForEachAlive(b =>
            {
                // prevent re-adding the item back to the callee.
                // That would result in a <see cref="StackOverflowException"/>.
                if (b != caller)
                    b.setIndex(index, item, this);
            });

            ItemsRemoved?.Invoke(new[] { lastItem });
            ItemsAdded?.Invoke(new[] { item });
        }

        /// <summary>
        /// Adds a single item to this <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item to be added.</param>
        /// <exception cref="InvalidOperationException">Thrown when this <see cref="BindableCollection{T}"/> is <see cref="Disabled"/>.</exception>
        public void Add(T item)
            => add(item, null);

        private void add(T item, BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot add items while the {nameof(BindableCollection<T>)} is disabled.");

            collection.Add(item);

            bindings?.ForEachAlive(b =>
            {
                // prevent re-adding the item back to the callee.
                // That would result in a <see cref="StackOverflowException"/>.
                if (b != caller)
                    b.add(item, this);
            });

            ItemsAdded?.Invoke(new[] { item });
        }

        /// <summary>
        /// Retrieves the index of an item in this <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item to retrieve the index of.</param>
        /// <returns>The index of the item, or -1 if the item isn't in this <see cref="BindableCollection{T}"/>.</returns>
        public int IndexOf(T item) => collection.IndexOf(item);

        /// <summary>
        /// Inserts an item at the specified index in this <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="index">The index to insert at.</param>
        /// <param name="item">The item to insert.</param>
        /// <exception cref="InvalidOperationException">Thrown when this <see cref="BindableCollection{T}"/> is <see cref="Disabled"/>.</exception>
        public void Insert(int index, T item)
            => insert(index, item, null);

        private void insert(int index, T item, BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot insert items while the {nameof(BindableCollection<T>)} is disabled.");

            collection.Insert(index, item);

            bindings?.ForEachAlive(b =>
            {
                // prevent re-adding the item back to the callee.
                // That would result in a <see cref="StackOverflowException"/>.
                if (b != caller)
                    b.insert(index, item, this);
            });

            ItemsAdded?.Invoke(new[] { item });
        }

        /// <summary>
        /// Clears the contents of this <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when this <see cref="BindableCollection{T}"/> is <see cref="Disabled"/>.</exception>
        public void Clear()
            => clear(null);

        private void clear(BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot clear items while the {nameof(BindableCollection<T>)} is disabled.");

            if (collection.Count <= 0)
                return;

            // Preserve items for subscribers
            var clearedItems = collection.ToList();

            collection.Clear();

            bindings?.ForEachAlive(b =>
            {
                // prevent re-adding the item back to the callee.
                // That would result in a <see cref="StackOverflowException"/>.
                if (b != caller)
                    b.clear(this);
            });

            ItemsRemoved?.Invoke(clearedItems);
        }

        /// <summary>
        /// Determines if an item is in this <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item to locate in this <see cref="BindableCollection{T}"/>.</param>
        /// <returns><code>true</code> if this <see cref="BindableCollection{T}"/> contains the given item.</returns>
        public bool Contains(T item)
            => collection.Contains(item);

        /// <summary>
        /// Removes an item from this <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="item">The item to remove from this <see cref="BindableCollection{T}"/>.</param>
        /// <returns><code>true</code> if the removal was successful.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="BindableCollection{T}"/> is <see cref="Disabled"/>.</exception>
        public bool Remove(T item)
            => remove(item, null);

        private bool remove(T item, BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot remove items while the {nameof(BindableCollection<T>)} is disabled.");

            bool removed = collection.Remove(item);

            if (removed)
            {
                bindings?.ForEachAlive(b =>
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.remove(item, this);
                });

                ItemsRemoved?.Invoke(new[] { item });
            }

            return removed;
        }

        /// <summary>
        /// Removes an item at the specified index from this <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="index">The index of the item to remove.</param>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="BindableCollection{T}"/> is <see cref="Disabled"/>.</exception>
        public void RemoveAt(int index)
            => removeAt(index, null);

        private void removeAt(int index, BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot remove items while the {nameof(BindableCollection<T>)} is disabled.");

            T item = collection[index];

            collection.RemoveAt(index);

            bindings?.ForEachAlive(b =>
            {
                // prevent re-adding the item back to the callee.
                // That would result in a <see cref="StackOverflowException"/>.
                if (b != caller)
                    b.removeAt(index, this);
            });

            ItemsRemoved?.Invoke(new[] { item });
        }

        /// <summary>
        /// Copies the contents of this <see cref="BindableCollection{T}"/> to the given array, starting at the given index.
        /// </summary>
        /// <param name="array">The array that is the destination of the items copied from this <see cref="BindableCollection{T}"/>.</param>
        /// <param name="arrayIndex">The index at which the copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
            => collection.CopyTo(array, arrayIndex);

        /// <summary>
        /// Copies the contents of this <see cref="BindableCollection{T}"/> to the given array, starting at the given index.
        /// </summary>
        /// <param name="array">The array that is the destination of the items copied from this <see cref="BindableCollection{T}"/>.</param>
        /// <param name="index">The index at which the copying begins.</param>
        public void CopyTo(Array array, int index)
            => ((ICollection)collection).CopyTo(array, index);

        public int Count => collection.Count;
        public bool IsSynchronized => ((ICollection)collection).IsSynchronized;
        public object SyncRoot => ((ICollection)collection).SyncRoot;
        public bool IsReadOnly => Disabled;

        #region IParseable

        /// <summary>
        /// Parse an object into this instance.
        /// A collection holding items of type <typeparamref name="T"/> can be parsed. Null results in an empty <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="BindableCollection{T}"/> is <see cref="Disabled"/>.</exception>
        public void Parse(object input)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot parse object while the {nameof(BindableCollection<T>)} is disabled.");

            switch (input)
            {
                case null:
                    Clear();
                    break;
                case IEnumerable<T> enumerable:
                    Clear();
                    AddRange(enumerable);
                    break;
                default:
                    throw new ArgumentException($@"Could not parse provided {input.GetType()} ({input}) to {typeof(T)}.");
            }
        }

        #endregion

        #region ICanBeDisabled

        private bool disabled;

        /// <summary>
        /// Whether this <see cref="BindableCollection{T}"/> has been disabled. When disabled, attempting to change the contents of this <see cref="BindableCollection{T}"/> will result in an <see cref="InvalidOperationException"/>.
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

            if (propagateToBindings)
                bindings?.ForEachAlive(b => b.Disabled = disabled);

            if (beforePropagation == disabled)
                DisabledChanged?.Invoke(disabled);
        }

        #endregion ICanBeDisabled

        #region IUnbindable

        public void UnbindEvents()
        {
            ItemsAdded = null;
            ItemsRemoved = null;
            DisabledChanged = null;
        }

        public void UnbindBindings()
        {
            bindings?.ForEachAlive(b => b.unbind(this));
            bindings?.Clear();
        }

        public void UnbindAll()
        {
            UnbindEvents();
            UnbindBindings();
        }

        public void UnbindFrom(IUnbindable them)
        {
            if (!(them is BindableCollection<T> tThem))
                throw new InvalidCastException($"Can't unbind a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            removeWeakReference(tThem.weakReference);
            tThem.removeWeakReference(weakReference);
        }

        private void unbind(BindableCollection<T> binding)
            => bindings.Remove(binding.weakReference);

        #endregion IUnbindable

        #region IHasDescription

        public string Description { get; set; }

        #endregion IHasDescription

        #region IBindableCollection

        /// <summary>
        /// Adds a collection of items to this <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="items">The collection whose items should be added to this collection.</param>
        /// <exception cref="InvalidOperationException">is beeing thrown if this collection is <see cref="Disabled"/></exception>
        public void AddRange(IEnumerable<T> items)
            => addRange(items, null);

        private void addRange(IEnumerable<T> items, BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException("Can not add a range of items as bindable collection is disabled.");

            collection.AddRange(items);

            bindings?.ForEachAlive(b =>
            {
                // prevent re-adding the item back to the callee.
                // That would result in a <see cref="StackOverflowException"/>.
                if (b != caller)
                    b.addRange(items, this);
            });

            ItemsAdded?.Invoke(items);
        }

        void IBindableCollection<T>.BindTo(IBindableCollection<T> them)
        {
            if (!(them is BindableCollection<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        /// <summary>
        /// Binds this <see cref="BindableCollection{T}"/> to another.
        /// </summary>
        /// <param name="them">The <see cref="BindableCollection{T}"/> to be bound to.</param>
        public void BindTo(BindableCollection<T> them)
        {
            if (them == null)
                throw new ArgumentNullException(nameof(them));
            if (bindings?.Contains(weakReference) ?? false)
                throw new ArgumentException("An already bound collection can not be bound again.");
            if (them == this)
                throw new ArgumentException("A collection can not be bound to itself");

            // copy state and content over
            Parse(them);
            Disabled = them.Disabled;

            addWeakReference(them.weakReference);
            them.addWeakReference(weakReference);
        }

        private void addWeakReference(WeakReference<BindableCollection<T>> weakReference)
        {
            if (bindings == null)
                bindings = new WeakList<BindableCollection<T>>();

            bindings.Add(weakReference);
        }

        private void removeWeakReference(WeakReference<BindableCollection<T>> weakReference) => bindings?.Remove(weakReference);

        IBindableCollection<T> IBindableCollection<T>.GetBoundCopy()
            => GetBoundCopy();

        /// <summary>
        /// Create a new instance of <see cref="BindableCollection{T}"/> and binds it to this instance.
        /// </summary>
        /// <returns>The created instace.</returns>
        public BindableCollection<T> GetBoundCopy()
        {
            var copy = (BindableCollection<T>)Activator.CreateInstance(GetType(), new object[] { null });
            copy.BindTo(this);
            return copy;
        }

        #endregion IBindableCollection

        #region IEnumerable

        public IEnumerator<T> GetEnumerator()
            => collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        #endregion IEnumerable
    }
}
