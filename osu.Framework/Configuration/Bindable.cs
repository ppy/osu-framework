// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Lists;

namespace osu.Framework.Configuration
{
    public class Bindable<T> : IBindable
    {
        private T value;

        public T Default;

        private bool disabled;

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

        public delegate void BindableValueChanged<in TValue>(TValue newValue);

        public delegate void BindableDisabledChanged(bool isDisabled);

        public virtual bool IsDefault => Equals(value, Default);

        public event BindableValueChanged<T> ValueChanged;

        public event BindableDisabledChanged DisabledChanged;

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

        public Bindable(T value = default(T))
        {
            this.value = value;
        }

        public static implicit operator T(Bindable<T> value)
        {
            return value.Value;
        }

        private WeakList<Bindable<T>> bindings;

        public WeakReference<Bindable<T>> WeakReference => new WeakReference<Bindable<T>>(this);

        /// <summary>
        /// Binds outselves to another bindable such that they receive bi-directional updates.
        /// We will take on any value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        public virtual void BindTo(Bindable<T> them)
        {
            Value = them.Value;
            Disabled = them.Disabled;

            AddWeakReference(them.WeakReference);
            them.AddWeakReference(WeakReference);
        }

        protected void AddWeakReference(WeakReference<Bindable<T>> weakReference)
        {
            if (bindings == null)
                bindings = new WeakList<Bindable<T>>();

            bindings.Add(weakReference);
        }

        public virtual void Parse(object s)
        {
            if (s is T)
                Value = (T)s;
            else if (typeof(T).IsEnum && s is string)
                Value = (T)Enum.Parse(typeof(T), (string)s);
            else
                throw new ArgumentException($@"Could not parse provided {s.GetType()} ({s}) to {typeof(T)}.");
        }

        public void TriggerChange()
        {
            TriggerValueChange();
            TriggerDisabledChange();
        }

        protected void TriggerValueChange()
        {
            ValueChanged?.Invoke(value);
            bindings?.ForEachAlive(b => b.Value = value);
        }

        protected void TriggerDisabledChange()
        {
            DisabledChanged?.Invoke(disabled);
            bindings?.ForEachAlive(b => b.Disabled = disabled);
        }

        public void UnbindAll()
        {
            ValueChanged = null;
            DisabledChanged = null;
        }

        public string Description { get; set; }

        public override string ToString()
        {
            return value?.ToString() ?? string.Empty;
        }

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
    }
}
