// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Configuration
{
    public class BindableCollection<T> : IBindableCollection<T>
    {
        // A list allows to use methods like AddRange.
        private readonly List<T> collection = new List<T>();

        protected WeakList<BindableCollection<T>> Bindings;

        private WeakReference<BindableCollection<T>> weakReference { get; }

        public event Action<IEnumerable<T>> ItemsAdded;
        public event Action<IEnumerable<T>> ItemsRemoved;
        public event Action<bool> DisabledChanged;

        public BindableCollection() : this(null)
        {
        }

        public BindableCollection(IEnumerable<T> items = null)
        {
            if (items != null)
                collection.AddRange(items);

            weakReference = new WeakReference<BindableCollection<T>>(this);
        }

        #region IEnumerable

        public IEnumerator<T> GetEnumerator()
            => collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        #endregion IEnumerable

        #region ICollection

        public void Add(T item)
            => add(item, null);

        private void add(T item, BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot add items while the {nameof(BindableCollection<T>)} is disabled.");

            collection.Add(item);

            Bindings?.ForEachAlive(b =>
            {
                // prevent re-adding the item back to the callee.
                // That would result in a <see cref="StackOverflowException"/>.
                if (b != caller)
                    b.add(item, this);
            });

            ItemsAdded?.Invoke(new[] { item });
        }

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

            Bindings?.ForEachAlive(b =>
            {
                // prevent re-adding the item back to the callee.
                // That would result in a <see cref="StackOverflowException"/>.
                if (b != caller)
                    b.clear(this);
            });

            ItemsRemoved?.Invoke(clearedItems);
        }

        public bool Contains(T item)
            => collection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
            => collection.CopyTo(array, arrayIndex);

        public bool Remove(T item)
            => remove(item, null);

        private bool remove(T item, BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot remove items while the {nameof(BindableCollection<T>)} is disabled.");

            bool removed = collection.Remove(item);

            if (removed)
            {
                Bindings?.ForEachAlive(b =>
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
        int ICollection.Count => collection.Count;
        int ICollection<T>.Count => collection.Count;
        int IReadOnlyCollection<T>.Count => collection.Count;
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

                TriggerDisabledChange();
            }
        }

        public void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false)
        {
            DisabledChanged += onChange;
            if (runOnceImmediately)
                onChange(Disabled);
        }

        protected void TriggerDisabledChange(bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            bool beforePropagation = disabled;
            if (propagateToBindings) Bindings?.ForEachAlive(b => b.Disabled = disabled);
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
            Bindings?.ForEachAlive(b => b.Unbind(this));
            Bindings?.Clear();
        }

        public void UnbindAll()
        {
            UnbindEvents();
            UnbindBindings();
        }

        protected void Unbind(BindableCollection<T> binding) => Bindings.Remove(binding.weakReference);

        #endregion IUnbindable

        #region IHasDescription

        public string Description { get; set; }

        #endregion IHasDescription

        #region IBindableCollection

        public void AddRange(IEnumerable<T> items)
        {
            if (Disabled)
                throw new InvalidOperationException("Can not add a range of items as bindable collection is disabled.");

            collection.AddRange(items);
            ItemsAdded?.Invoke(items);
        }

        void IBindableCollection<T>.BindTo(IBindableCollection<T> them)
        {
            if (!(them is BindableCollection<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        void IBindableCollection.BindTo(IBindableCollection them)
        {
            if (!(them is BindableCollection<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        public void BindTo(BindableCollection<T> them)
        {
            Parse(them);
            Disabled = them.Disabled;

            addWeakReference(them.weakReference);
            them.addWeakReference(weakReference);
        }

        private void addWeakReference(WeakReference<BindableCollection<T>> weakReference)
        {
            if (Bindings == null)
                Bindings = new WeakList<BindableCollection<T>>();

            Bindings.Add(weakReference);
        }

        IBindableCollection IBindableCollection.GetBoundCopy()
            => GetBoundCopy();

        IBindableCollection<T> IBindableCollection<T>.GetBoundCopy()
            => GetBoundCopy();

        public BindableCollection<T> GetBoundCopy()
        {
            var copy = (BindableCollection<T>)Activator.CreateInstance(GetType(), null);
            copy.BindTo(this);
            return copy;
        }

        #endregion IBindableCollection
    }
}
