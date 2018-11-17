// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Configuration
{
    public class BindableCollection<T> : IBindableCollection<T>, ICollection, ICollection<T>, IParseable, IHasDescription
    {
        // list allows to use methods like AddRange.
        private readonly List<T> collection = new List<T>();

        private WeakReference<BindableCollection<T>> weakReference { get; }
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
        /// <see cref="DisabledChanged"/> is beeing called when <see cref="Disabled"/>s value changed.
        /// </summary>
        public event Action<bool> DisabledChanged;

        /// <summary>
        /// Created a new instance of <see cref="BindableCollection{T}"/> and adds the gives items to the created <see cref="BindableCollection{T}"/>.
        /// </summary>
        /// <param name="items">The items that are going to be contained in the newly created <see cref="BindableCollection{T}"/>.</param>
        public BindableCollection(IEnumerable<T> items = null)
        {
            if (items != null)
                collection.AddRange(items);

            // we can not initialize this directly at the property due to the this capture.
            weakReference = new WeakReference<BindableCollection<T>>(this);
        }

        #region ICollection

        /// <summary>
        /// Adds a single item to this collection.
        /// </summary>
        /// <param name="item">The item that is going to be added.</param>
        /// <exception cref="InvalidOperationException">Is being thrown when this collection is <see cref="Disabled"/></exception>
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
        /// Clears all contents of the collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is being thrown when this collection is <see cref="Disabled"/></exception>
        public void Clear()
            => clear(null);

        private void clear(BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot clear items while the {nameof(BindableCollection<T>)} is disabled.");

            if (collection.Count <= 0)
                return;

            // Preserve items for subscribers
            var clearedItems = new T[collection.Count];
            collection.CopyTo(clearedItems);

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
        /// Determines if an item is in this collection.
        /// </summary>
        /// <param name="item">The items to locate in this collection.</param>
        /// <returns><code>true</code> is this collection contains the given item.</returns>
        public bool Contains(T item)
            => collection.Contains(item);

        /// <summary>
        /// Copies the contents of this collection to the given array, starting at the given index.
        /// </summary>
        /// <param name="array">The array that is the destination of the items copied from this collection.</param>
        /// <param name="arrayIndex">The index at which the copying begins.</param>
        public void CopyTo(T[] array, int arrayIndex)
            => collection.CopyTo(array, arrayIndex);

        /// <summary>
        /// Removes an item from this collection.
        /// </summary>
        /// <param name="item">The item to remove from this collection.</param>
        /// <returns><code>true</code> or <code>false</code> depending if the removal was successfull</returns>
        /// <exception cref="InvalidOperationException">is beeing thrown if this collection is <see cref="Disabled"/></exception>
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

        public void CopyTo(Array array, int index)
            => ((ICollection) collection).CopyTo(array, index);

        public int Count => collection.Count;
        public bool IsSynchronized => ((ICollection)collection).IsSynchronized;
        public object SyncRoot => ((ICollection)collection).SyncRoot;
        public bool IsReadOnly => Disabled;

        #endregion ICollection

        #region IParseable

        /// <summary>
        /// Parse an object into this instance.
        /// An collection holding items deriving T can be parsed, or null that results into an empty collection.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        /// <exception cref="InvalidOperationException">is beeing thrown if this collection is <see cref="Disabled"/></exception>
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
        /// The collection can not be modified if the collection is disabled.
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

        private void bindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false)
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

        private void unbind(BindableCollection<T> binding)
            => bindings.Remove(binding.weakReference);

        #endregion IUnbindable

        #region IHasDescription

        public string Description { get; set; }

        #endregion IHasDescription

        #region IBindableCollection

        /// <summary>
        /// Adds a range if items of the specified collection to this collection.
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
        /// Binds this collection to <paramref name="them"/>.
        /// </summary>
        /// <param name="them">The collection to be bound to this collection</param>
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
