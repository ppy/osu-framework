using System;
using System.Collections;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Configuration
{
    public class BindableCollection<T> : IBindableCollection<T>, IBindableCollection
    {
        // We use a list, that allows us to use methods like AddRange.
        private readonly List<T> collection = new List<T>();

        protected WeakList<BindableCollection<T>> Bindings;

        private WeakReference<BindableCollection<T>> weakReference { get; }

        public BindableCollection(IEnumerable<T> items = null)
        {
            if (items != null)
                collection = new List<T>(collection);

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
            => Add(item, null);

        protected void Add(T item, BindableCollection<T> caller)
        {
            if (Disabled)
                throw new InvalidOperationException($"Can not add item as bindable collection is disabled.");

            collection.Add(item);
            TriggerItemAdded(item, true, caller);
        }

        public void Clear()
        {
            if (Disabled)
                throw new InvalidOperationException($"Can not clear items as bindable collection is disabled.");

            if (collection.Count <= 0) return;

            var clearedItems = new T[collection.Count];

            collection.CopyTo(clearedItems);
            collection.Clear();

            ItemsCleared?.Invoke(clearedItems);
        }

        public bool Contains(T item)
            => collection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
            => collection.CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            if (Disabled)
                throw new InvalidOperationException($"Can not remove item as bindable collection is disabled.");

            bool removed = collection.Remove(item);

            if (removed)
                ItemRemoved?.Invoke(item);

            return removed;
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public int Count => collection.Count;
        public bool IsSynchronized { get; }
        public object SyncRoot { get; }
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
                throw new InvalidOperationException($"Can not parse object as bindable collection is disabled.");

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

        public bool Disabled { get; private set; }

        public event Action<bool> DisabledChanged;

        public void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false)
        {
            throw new NotImplementedException();
        }

        #endregion ICanBeDisabled

        #region IUnbindable

        public void UnbindEvents()
        {
            ItemAdded = null;
            ItemRangeAdded = null;
            ItemRemoved = null;
            ItemsCleared = null;
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

        public string Description { get; }

        #endregion IHasDescription

        #region IBindableCollection

        public event Action<T> ItemAdded;
        public event Action<IEnumerable<T>> ItemRangeAdded;
        public event Action<T> ItemRemoved;
        public event Action<IEnumerable<T>> ItemsCleared;

        public void AddRange(IEnumerable<T> collection)
        {
            if (Disabled)
                throw new InvalidOperationException($"Can not add a range of items as bindable collection is disabled.");

            this.collection.AddRange(collection);
            ItemRangeAdded?.Invoke(collection);
        }

        public void BindTo(IBindableCollection<T> them)
        {
            if (!(them is BindableCollection<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        public void BindTo(BindableCollection<T> them)
        {
            Parse(them);
            Disabled = them.Disabled;

            AddWeakReference(them.weakReference);
            them.AddWeakReference(weakReference);
        }

        protected void AddWeakReference(WeakReference<BindableCollection<T>> weakReference)
        {
            if (Bindings == null)
                Bindings = new WeakList<BindableCollection<T>>();

            Bindings.Add(weakReference);
        }

        public void BindTo(IBindableCollection them)
        {
            throw new NotImplementedException();
        }

        IBindableCollection IBindableCollection.GetBoundCopy()
        {
            throw new NotImplementedException();
        }

        public IBindableCollection<T> GetBoundCopy()
        {
            var copy = (BindableCollection<T>) Activator.CreateInstance(GetType());
            copy.BindTo(this);
            return copy;
        }

        #endregion IBindableCollection

        /// <summary>
        /// Notifies the subscribers and bindings about the item add.
        /// </summary>
        /// <param name="addedItem">The item that got added to the collection</param>
        /// <param name="propagateToBindings">If the bindings should be notified that an item got added. Default: true</param>
        /// <param name="caller">The bindable collection that calls this method. Can be null if the caller is this. Default: null</param>
        protected void TriggerItemAdded(T addedItem, bool propagateToBindings = true, BindableCollection<T> caller = null)
        {
            if (propagateToBindings)
                Bindings?.ForEachAlive(b =>
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.Add(addedItem, this);
                });

            ItemAdded?.Invoke(addedItem);
        }
    }
}
