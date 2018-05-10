// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Lists;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// A generic implementation of a <see cref="IBindable"/>
    /// </summary>
    /// <typeparam name="T">The type of our stored <see cref="Value"/>.</typeparam>
    public class Bindable<T> : IBindable<T>, IBindable
    {
        /// <summary>
        /// An event which is raised when <see cref="Value"/> has changed (or manually via <see cref="TriggerValueChange"/>).
        /// </summary>
        public event Action<T> ValueChanged;

        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed (or manually via <see cref="TriggerDisabledChange"/>).
        /// </summary>
        public event Action<bool> DisabledChanged;

        private T value;

        /// <summary>
        /// The default value of this bindable. Used when calling <see cref="SetDefault"/> or querying <see cref="IsDefault"/>.
        /// </summary>
        public T Default { get; set; }

        private bool disabled;

        /// <summary>
        /// Whether this bindable has been disabled. When disabled, attempting to change the <see cref="Value"/> will result in an <see cref="InvalidOperationException"/>.
        /// </summary>
        public bool Disabled
        {
            get { return disabled; }
            set
            {
                if (disabled == value) return;

                disabled = value;

                TriggerDisabledChange();
            }
        }

        /// <summary>
        /// Check whether the current <see cref="Value"/> is equal to <see cref="Default"/>.
        /// </summary>
        public virtual bool IsDefault => Equals(value, Default);

        /// <summary>
        /// Revert the current <see cref="Value"/> to the defined <see cref="Default"/>.
        /// </summary>
        public void SetDefault() => Value = Default;

        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        public virtual T Value
        {
            get { return value; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(this.value, value)) return;

                if (Disabled)
                    throw new InvalidOperationException($"Can not set value to \"{value.ToString()}\" as bindable is disabled.");

                this.value = value;

                TriggerValueChange();
            }
        }

        /// <summary>
        /// Creates a new bindable instance.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public Bindable(T value = default(T))
        {
            this.value = value;
        }

        public static implicit operator T(Bindable<T> value) => value.Value;

        protected WeakList<Bindable<T>> Bindings;

        private WeakReference<Bindable<T>> weakReference => new WeakReference<Bindable<T>>(this);

        void IBindable.BindTo(IBindable them)
        {
            if (!(them is Bindable<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");
            BindTo(tThem);
        }

        void IBindable<T>.BindTo(IBindable<T> them)
        {
            if (!(them is Bindable<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");
            BindTo(tThem);
        }

        /// <summary>
        /// Binds outselves to another bindable such that bi-directional updates are propagated.
        /// We will take on any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        public virtual void BindTo(Bindable<T> them)
        {
            Value = them.Value;
            Disabled = them.Disabled;
            Default = them.Default;

            AddWeakReference(them.weakReference);
            them.AddWeakReference(weakReference);
        }

        protected void AddWeakReference(WeakReference<Bindable<T>> weakReference)
        {
            if (Bindings == null)
                Bindings = new WeakList<Bindable<T>>();

            Bindings.Add(weakReference);
        }

        /// <summary>
        /// Parse an object into this instance.
        /// An object deriving T can be parsed, or a string can be parsed if T is an enum type.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        public virtual void Parse(object input)
        {
            switch (input)
            {
                case T t:
                    Value = t;
                    break;
                case string s:
                    Value = typeof(T).IsEnum
                        ? (T)Enum.Parse(typeof(T), s)
                        : (T)Convert.ChangeType(s, typeof(T), CultureInfo.InvariantCulture);
                    break;
                default:
                    throw new ArgumentException($@"Could not parse provided {input.GetType()} ({input}) to {typeof(T)}.");
            }
        }

        /// <summary>
        /// Raise <see cref="ValueChanged"/> and <see cref="DisabledChanged"/> once, without any changes actually occurring.
        /// This does not propagate to any outward bound bindables.
        /// </summary>
        public virtual void TriggerChange()
        {
            TriggerValueChange(false);
            TriggerDisabledChange(false);
        }

        protected void TriggerValueChange(bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = value;
            if (propagateToBindings) Bindings?.ForEachAlive(b => b.Value = value);
            if (Equals(beforePropagation, value))
                ValueChanged?.Invoke(value);
        }

        protected void TriggerDisabledChange(bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            bool beforePropagation = disabled;
            if (propagateToBindings) Bindings?.ForEachAlive(b => b.Disabled = disabled);
            if (Equals(beforePropagation, disabled))
                DisabledChanged?.Invoke(disabled);
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
        /// Remove all bound <see cref="Bindable{T}"/>s via <see cref="GetBoundCopy"/> or <see cref="BindTo"/>.
        /// </summary>
        public void UnbindBindings()
        {
            Bindings?.ForEachAlive(b => b.Unbind(this));
            Bindings?.Clear();
        }

        protected void Unbind(Bindable<T> binding) => Bindings.Remove(binding.weakReference);

        /// <summary>
        /// Calls <see cref="UnbindEvents"/> and <see cref="UnbindBindings"/>
        /// </summary>
        public void UnbindAll()
        {
            UnbindEvents();
            UnbindBindings();
        }

        public string Description { get; set; }

        public override string ToString()
        {
            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Reset this bindable to its <see cref="Default"/> value and set <see cref="Disabled"/> to false.
        /// </summary>
        internal void Reset()
        {
            Value = Default;
            Disabled = false;
        }

        IBindable IBindable.GetBoundCopy() => GetBoundCopy();

        IBindable<T> IBindable<T>.GetBoundCopy() => GetBoundCopy();

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        public Bindable<T> GetBoundCopy()
        {
            var copy = (Bindable<T>)Activator.CreateInstance(GetType(), Value);
            copy.BindTo(this);
            return copy;
        }
    }
}
