// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// A generic implementation of a <see cref="IBindable"/>
    /// </summary>
    /// <typeparam name="T">The type of our stored <see cref="Value"/>.</typeparam>
    public class Bindable<T> : IBindable
    {
        private T value;

        /// <summary>
        /// The default value of this bindable. Used when calling <see cref="SetDefault"/> or querying <see cref="IsDefault"/>.
        /// </summary>
        public T Default;

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
        /// An event which is raised when <see cref="Value"/> has changed (or manually via <see cref="TriggerValueChange"/>).
        /// </summary>
        public event BindableValueChanged<T> ValueChanged;

        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed (or manually via <see cref="TriggerDisabledChange"/>).
        /// </summary>
        public event BindableDisabledChanged DisabledChanged;

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

        private WeakList<Bindable<T>> bindings;

        private WeakReference<Bindable<T>> weakReference => new WeakReference<Bindable<T>>(this);

        /// <summary>
        /// Binds outselves to another bindable such that they receive bi-directional updates.
        /// We will take on any value limitations of the bindable we bind width.
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
            if (bindings == null)
                bindings = new WeakList<Bindable<T>>();

            bindings.Add(weakReference);
        }

        /// <summary>
        /// Parse an object into this instance.
        /// An object deriving T can be parsed, or a string can be parsed if T is an enum type.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        public virtual void Parse(object input)
        {
            if (input is T)
                Value = (T)input;
            else if (typeof(T).IsEnum && input is string)
                Value = (T)Enum.Parse(typeof(T), (string)input);
            else
                throw new ArgumentException($@"Could not parse provided {input.GetType()} ({input}) to {typeof(T)}.");
        }

        /// <summary>
        /// Raise <see cref="ValueChanged"/> and <see cref="DisabledChanged"/> once, without any changes actually occurring.
        /// This does not propagate to any outward bound bindables.
        /// </summary>
        public void TriggerChange()
        {
            TriggerValueChange(false);
            TriggerDisabledChange(false);
        }

        protected void TriggerValueChange(bool propagateToBindings = true)
        {
            ValueChanged?.Invoke(value);
            if (propagateToBindings) bindings?.ForEachAlive(b => b.Value = value);
        }

        protected void TriggerDisabledChange(bool propagateToBindings = true)
        {
            DisabledChanged?.Invoke(disabled);
            if (propagateToBindings) bindings?.ForEachAlive(b => b.Disabled = disabled);
        }

        /// <summary>
        /// Unbind any events bound to <see cref="ValueChanged"/> and <see cref="DisabledChanged"/>, along with
        /// removing all bound <see cref="Bindable{T}"/>s via <see cref="GetBoundCopy"/> or <see cref="BindTo"/>.
        /// </summary>
        public void UnbindAll()
        {
            ValueChanged = null;
            DisabledChanged = null;
            bindings.Clear();
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

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        public Bindable<T> GetBoundCopy()
        {
            var copy = (Bindable<T>)MemberwiseClone();

            copy.bindings = new WeakList<Bindable<T>>();
            copy.BindTo(this);

            return copy;
        }

        public delegate void BindableValueChanged<in TValue>(TValue newValue);

        public delegate void BindableDisabledChanged(bool isDisabled);
    }
}
