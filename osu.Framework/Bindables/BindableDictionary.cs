// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using osu.Framework.Caching;
using osu.Framework.Lists;

namespace osu.Framework.Bindables
{
    public class BindableDictionary<TKey, TValue> : IBindableDictionary<TKey, TValue>, IBindable, IParseable, IDictionary<TKey, TValue>, IDictionary
        where TKey : notnull
    {
        public event NotifyDictionaryChangedEventHandler<TKey, TValue>? CollectionChanged;

        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed (or manually via <see cref="triggerDisabledChange(bool)"/>).
        /// </summary>
        public event Action<bool>? DisabledChanged;

        private readonly Dictionary<TKey, TValue> collection;

        private readonly Cached<WeakReference<BindableDictionary<TKey, TValue>>> weakReferenceCache = new Cached<WeakReference<BindableDictionary<TKey, TValue>>>();

        private WeakReference<BindableDictionary<TKey, TValue>> weakReference
            => weakReferenceCache.IsValid ? weakReferenceCache.Value : weakReferenceCache.Value = new WeakReference<BindableDictionary<TKey, TValue>>(this);

        private LockedWeakList<BindableDictionary<TKey, TValue>>? bindings;

        /// <inheritdoc cref="Dictionary{TKey,TValue}(IEqualityComparer{TKey})" />
        public BindableDictionary(IEqualityComparer<TKey>? comparer = null)
            : this(0, comparer)
        {
        }

        /// <inheritdoc cref="Dictionary{TKey,TValue}(IDictionary{TKey,TValue},IEqualityComparer{TKey})" />
        public BindableDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey>? comparer = null)
            : this((IEnumerable<KeyValuePair<TKey, TValue>>)dictionary, comparer)
        {
        }

        /// <inheritdoc cref="Dictionary{TKey,TValue}(int,IEqualityComparer{TKey})" />
        public BindableDictionary(int capacity, IEqualityComparer<TKey>? comparer = null)
        {
            collection = new Dictionary<TKey, TValue>(capacity, comparer);
        }

        /// <inheritdoc cref="Dictionary{TKey,TValue}(IEnumerable{KeyValuePair{TKey,TValue}},IEqualityComparer{TKey})" />
        public BindableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey>? comparer = null)
        {
            this.collection = new Dictionary<TKey, TValue>(collection, comparer);
        }

        #region IDictionary<TKey, Value>

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown when this <see cref="BindableDictionary{TKey, TValue}"/> is <see cref="Disabled"/>.</exception>
        public void Add(TKey key, TValue value)
            => add(key, value, null);

        private void add(TKey key, TValue value, BindableDictionary<TKey, TValue>? caller)
        {
            ensureMutationAllowed();

            collection.Add(key, value);

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.add(key, value, this);
                }
            }

            notifyDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
        }

        public bool ContainsKey(TKey key) => collection.ContainsKey(key);

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="BindableDictionary{TKey, TValue}"/> is <see cref="Disabled"/>.</exception>
        public bool Remove(TKey key)
            => remove(key, out _, null);

        /// <inheritdoc cref="IDictionary.Remove" />
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="BindableDictionary{TKey, TValue}"/> is <see cref="Disabled"/>.</exception>
        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
            => remove(key, out value, null);

        private bool remove(TKey key, [MaybeNullWhen(false)] out TValue value, BindableDictionary<TKey, TValue>? caller)
        {
            ensureMutationAllowed();

            if (!collection.Remove(key, out value))
                return false;

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-removing from the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.remove(key, out _, this);
                }
            }

            notifyDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, new KeyValuePair<TKey, TValue>(key, value)));

            return true;
        }

#if NETSTANDARD
        public bool TryGetValue(TKey key, out TValue value) => collection.TryGetValue(key, out value);
#else
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => collection.TryGetValue(key, out value);
#endif

        /// <inheritdoc cref="IDictionary{TKey,TValue}.this" />
        /// <exception cref="InvalidOperationException">Thrown when setting an item while this <see cref="BindableDictionary{TKey, TValue}"/> is <see cref="Disabled"/>.</exception>
        public TValue this[TKey key]
        {
            get => collection[key];
            set => setKey(key, value, null);
        }

        private void setKey(TKey key, TValue value, BindableDictionary<TKey, TValue>? caller)
        {
            ensureMutationAllowed();

            bool hasPreviousValue = TryGetValue(key, out TValue? lastValue);

            collection[key] = value;

            if (bindings != null)
            {
                foreach (var b in bindings)
                {
                    // prevent re-adding the item back to the callee.
                    // That would result in a <see cref="StackOverflowException"/>.
                    if (b != caller)
                        b.setKey(key, value, this);
                }
            }

            notifyDictionaryChanged(hasPreviousValue
                ? new NotifyDictionaryChangedEventArgs<TKey, TValue>(new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, lastValue!))
                : new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
        }

        public ICollection<TKey> Keys => collection.Keys;

        public ICollection<TValue> Values => collection.Values;

        #endregion

        #region IDictionary

        void IDictionary.Add(object key, object? value) => Add((TKey)key, (TValue)(value ?? throw new ArgumentNullException(nameof(value))));

        /// <inheritdoc cref="IDictionary.Clear" />
        /// <exception cref="InvalidOperationException">Thrown when this <see cref="BindableDictionary{TKey, TValue}"/> is <see cref="Disabled"/>.</exception>
        public void Clear()
            => clear(null);

        private void clear(BindableDictionary<TKey, TValue>? caller)
        {
            ensureMutationAllowed();

            if (collection.Count == 0)
                return;

            // Preserve items for subscribers
            var clearedItems = collection.ToArray();

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

            notifyDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Remove, clearedItems));
        }

        bool IDictionary.Contains(object key)
        {
            return ((IDictionary)collection).Contains(key);
        }

        void IDictionary.Remove(object key) => Remove((TKey)key);

        bool IDictionary.IsFixedSize => ((IDictionary)collection).IsFixedSize;

        public bool IsReadOnly => Disabled;

        object? IDictionary.this[object key]
        {
            get => this[(TKey)key];
            set => this[(TKey)key] = (TValue)value!;
        }

        ICollection IDictionary.Values => (ICollection)Values;

        ICollection IDictionary.Keys => (ICollection)Keys;

        #endregion

        #region IReadOnlyDictionary<TKey, TValue>

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        #endregion

        #region ICollection<KeyValuePair<TKey, TValue>>

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (TryGetValue(item.Key, out TValue? value) && EqualityComparer<TValue>.Default.Equals(value, item.Value))
            {
                Remove(item.Key);
                return true;
            }

            return false;
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => ((ICollection<KeyValuePair<TKey, TValue>>)collection).Contains(item);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<TKey, TValue>>)collection).CopyTo(array, arrayIndex);

        #endregion

        #region ICollection

        void ICollection.CopyTo(Array array, int index)
            => ((ICollection)collection).CopyTo(array, index);

        bool ICollection.IsSynchronized => ((ICollection)collection).IsSynchronized;

        object ICollection.SyncRoot => ((ICollection)collection).SyncRoot;

        #endregion

        #region IReadOnlyCollection<TKey, TValue>

        public int Count => collection.Count;

        #endregion

        #region IParseable

        /// <summary>
        /// Parse an object into this instance.
        /// A collection holding items of type <see cref="KeyValuePair{TKey,TValue}"/> can be parsed. Null results in an empty <see cref="BindableDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        /// <exception cref="InvalidOperationException">Thrown if this <see cref="BindableDictionary{TKey, TValue}"/> is <see cref="Disabled"/>.</exception>
        public void Parse(object? input)
        {
            ensureMutationAllowed();

            switch (input)
            {
                case null:
                    Clear();
                    break;

                case IEnumerable<KeyValuePair<TKey, TValue>> enumerable:
                    // enumerate once locally before proceeding.
                    var newItems = enumerable.ToList();

                    if (this.SequenceEqual(newItems))
                        return;

                    Clear();
                    addRange(newItems, null);
                    break;

                default:
                    throw new ArgumentException($@"Could not parse provided {input.GetType()} ({input}) to {typeof(KeyValuePair<TKey, TValue>)}.");
            }
        }

        private void addRange(IList items, BindableDictionary<TKey, TValue>? caller)
        {
            ensureMutationAllowed();

            var typedItems = (IList<KeyValuePair<TKey, TValue>>)items;

            foreach (var (key, value) in typedItems)
                collection.Add(key, value);

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

            notifyDictionaryChanged(new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, typedItems));
        }

        #endregion

        #region ICanBeDisabled

        private bool disabled;

        /// <summary>
        /// Whether this <see cref="BindableDictionary{TKey, TValue}"/> has been disabled.
        /// When disabled, attempting to change the contents of this <see cref="BindableDictionary{TKey, TValue}"/> will result in an <see cref="InvalidOperationException"/>.
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

        public void UnbindEvents()
        {
            CollectionChanged = null;
            DisabledChanged = null;
        }

        public void UnbindBindings()
        {
            if (bindings == null)
                return;

            foreach (var b in bindings)
                b.unbind(this);

            bindings?.Clear();
        }

        public void UnbindAll()
        {
            UnbindEvents();
            UnbindBindings();
        }

        public void UnbindFrom(IUnbindable them)
        {
            if (!(them is BindableDictionary<TKey, TValue> tThem))
                throw new InvalidCastException($"Can't unbind a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            removeWeakReference(tThem.weakReference);
            tThem.removeWeakReference(weakReference);
        }

        private void unbind(BindableDictionary<TKey, TValue> binding)
        {
            Debug.Assert(bindings != null);
            bindings.Remove(binding.weakReference);
        }

        #endregion IUnbindable

        #region IHasDescription

        public string? Description { get; set; }

        #endregion IHasDescription

        #region IBindableCollection

        void IBindable.BindTo(IBindable them)
        {
            if (!(them is BindableDictionary<TKey, TValue> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        void IBindableDictionary<TKey, TValue>.BindTo(IBindableDictionary<TKey, TValue> them)
        {
            if (!(them is BindableDictionary<TKey, TValue> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        public BindableDictionary<TKey, TValue> BindTarget
        {
            set => ((IBindableDictionary<TKey, TValue>)this).BindTo(value);
        }

        /// <summary>
        /// Binds this <see cref="BindableDictionary{TKey, TValue}"/> to another.
        /// </summary>
        /// <param name="them">The <see cref="BindableDictionary{TKey, TValue}"/> to be bound to.</param>
        public void BindTo(BindableDictionary<TKey, TValue> them)
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
        /// with an <see cref="NotifyCollectionChangedAction.Add"/> event for the entire contents of this <see cref="BindableDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <param name="onChange">The action to perform when this <see cref="BindableDictionary{TKey, TValue}"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <paramref name="onChange"/> should be run once immediately.</param>
        public void BindCollectionChanged(NotifyDictionaryChangedEventHandler<TKey, TValue> onChange, bool runOnceImmediately = false)
        {
            CollectionChanged += onChange;
            if (runOnceImmediately)
                onChange(this, new NotifyDictionaryChangedEventArgs<TKey, TValue>(NotifyDictionaryChangedAction.Add, collection.ToArray()));
        }

        private void addWeakReference(WeakReference<BindableDictionary<TKey, TValue>> weakReference)
        {
            bindings ??= new LockedWeakList<BindableDictionary<TKey, TValue>>();
            bindings.Add(weakReference);
        }

        private void removeWeakReference(WeakReference<BindableDictionary<TKey, TValue>> weakReference) => bindings?.Remove(weakReference);

        IBindable IBindable.CreateInstance() => CreateInstance();

        /// <inheritdoc cref="IBindable.CreateInstance"/>
        protected virtual BindableDictionary<TKey, TValue> CreateInstance() => new BindableDictionary<TKey, TValue>();

        IBindable IBindable.GetBoundCopy() => GetBoundCopy();

        IBindableDictionary<TKey, TValue> IBindableDictionary<TKey, TValue>.GetBoundCopy() => GetBoundCopy();

        /// <inheritdoc cref="IBindable.GetBoundCopy"/>
        public BindableDictionary<TKey, TValue> GetBoundCopy() => IBindable.GetBoundCopyImplementation(this);

        #endregion IBindableCollection

        #region IEnumerable

        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() => collection.GetEnumerator();

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();

        IDictionaryEnumerator IDictionary.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable

        private void notifyDictionaryChanged(NotifyDictionaryChangedEventArgs<TKey, TValue> args) => CollectionChanged?.Invoke(this, args);

        private void ensureMutationAllowed()
        {
            if (Disabled)
                throw new InvalidOperationException($"Cannot mutate the {nameof(BindableDictionary<TKey, TValue>)} while it is disabled.");
        }

        public bool IsDefault => Count == 0;
    }
}
