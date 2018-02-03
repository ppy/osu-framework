// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Configuration
{
    public class BindableList<T> : IBindable, IList<T>, IReadOnlyList<T>
    {
        private static InvalidOperationException createDisabledExeption()
            => new InvalidOperationException("Can not modify values as bindable list is disabled");

        private readonly List<T> underlyingList = new List<T>();

        public void Parse(object input)
        {
            if (!(input is IEnumerable<T>))
                throw new ArgumentException($@"Could not parse provided {input.GetType()} ({input}) to {typeof(T)}.");
            Clear();
            underlyingList.AddRange((IList<T>)input);

            throw new ArgumentException($@"Could not parse provided {input.GetType()} ({input}) to {typeof(T)}.");
        }

        #region IList
        public IEnumerator<T> GetEnumerator()
            => underlyingList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public void Add(T item)
        {
            if (Disabled)
                throw createDisabledExeption();
            underlyingList.Add(item);
            TriggerValueChange();
        }

        public void AddAll(IEnumerable<T> enumerable)
        {
            foreach (var item in enumerable)
                Add(item);
        }

        public void Clear()
        {
            if (Disabled)
                throw createDisabledExeption();
            underlyingList.Clear();
            TriggerValueChange();
        }

        public bool Remove(T item)
        {
            if (Disabled)
                throw createDisabledExeption();
            bool removed = underlyingList.Remove(item);
            if (removed)
                TriggerValueChange();
            return removed;
        }

        public void Insert(int index, T item)
        {
            if (Disabled)
                throw createDisabledExeption();
            underlyingList.Insert(index, item);
            TriggerValueChange();
        }

        public void RemoveAt(int index)
        {
            if (Disabled)
                throw createDisabledExeption();
            underlyingList.RemoveAt(index);
            TriggerValueChange();
        }

        public T this[int index] {
            get => underlyingList[index];
            set {
                if (Disabled)
                    throw createDisabledExeption();
                underlyingList[index] = value;
                TriggerValueChange();
            }
        }

        public bool Contains(T item)
            => underlyingList.Contains(item);

        public void CopyTo(T[] array, int arrayIndex)
            => underlyingList.CopyTo(array, arrayIndex);

        public int Count => underlyingList.Count;
        public bool IsReadOnly => Disabled;

        public int IndexOf(T item)
            => underlyingList.IndexOf(item);

        public IReadOnlyList<T> AsReadOnlyList()
            => underlyingList.AsReadOnly();

        protected void OverrideItems(IReadOnlyList<T> newItems, bool propagateToBindings = true)
        {
            if (Disabled)
                throw createDisabledExeption();
            underlyingList.Clear();
            underlyingList.AddRange(newItems);
            TriggerValueChange(propagateToBindings);
        }
        #endregion IList

        #region Bindable

        /// <summary>
        /// An event which is raised when values have changed (or manually via <see cref="TriggerValueChange"/>).
        /// </summary>
        public event Action<BindableList<T>> ValueChanged;

        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed (or manually via <see cref="TriggerDisabledChange"/>).
        /// </summary>
        public event Action<bool> DisabledChanged;

        private bool disabled;

        public bool Disabled {
            get => disabled;
            set
            {
                if (disabled == value) return;
                disabled = value;
                TriggerDisabledChange();
            }
        }

        protected WeakList<BindableList<T>> Bindings;

        private WeakReference<BindableList<T>> weakReference => new WeakReference<BindableList<T>>(this);


        /// <summary>
        /// Binds outselves to another bindable such that they receive bi-directional updates.
        /// We will take on any value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        public virtual void BindTo(BindableList<T> them)
        {
            disabled = false;
            underlyingList.Clear();
            underlyingList.AddRange(them.AsReadOnlyList());

            Disabled = them.Disabled;

            AddWeakReference(them.weakReference);
            them.AddWeakReference(weakReference);

            TriggerValueChange(false);
        }


        protected void TriggerValueChange(bool propagateToBindings = true)
        {
            ValueChanged?.Invoke(this);
            if (propagateToBindings) Bindings?.ForEachAlive(b => b.OverrideItems(underlyingList, false));
        }

        protected void TriggerDisabledChange(bool propagateToBindings = true)
        {
            DisabledChanged?.Invoke(disabled);
            if (propagateToBindings) Bindings?.ForEachAlive(b => b.Disabled = disabled);
        }

        /// <summary>
        /// Unbind any events bound to <see cref="ValueChanged"/> and <see cref="DisabledChanged"/>.
        /// </summary>
        public void UnbindEvents()
        {
            ValueChanged = null;
            DisabledChanged = null;
        }

        /// <summary>
        /// Remove all bound <see cref="Bindable{T}"/> or <see cref="BindTo"/>.
        /// </summary>
        public void UnbindBindings()
        {
            Bindings?.Clear();
        }

        /// <summary>
        /// Calls <see cref="UnbindEvents"/> and <see cref="UnbindBindings"/>
        /// </summary>
        public void UnbindAll()
        {
            UnbindEvents();
            UnbindBindings();
        }

        protected void AddWeakReference(WeakReference<BindableList<T>> weakReference)
        {
            if (Bindings == null)
                Bindings = new WeakList<BindableList<T>>();

            Bindings.Add(weakReference);
        }
        #endregion

    }
}
