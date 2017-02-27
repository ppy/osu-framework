// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Configuration
{
    public class Bindable<T> : IBindable
    {
        private T value;

        public T Default;

        public bool Disabled;

        public virtual bool IsDefault => Equals(value, Default);

        public event EventHandler ValueChanged;

        public virtual T Value
        {
            get { return value; }
            set
            {
                if (EqualityComparer<T>.Default.Equals(this.value, value)) return;

                if (Disabled)
                {
                    TriggerChange();
                    return;
                }

                this.value = value;

                TriggerChange();
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

        private List<WeakReference<Bindable<T>>> bindings = new List<WeakReference<Bindable<T>>>();

        public WeakReference<Bindable<T>> WeakReference => new WeakReference<Bindable<T>>(this);

        /// <summary>
        /// Binds outselves to another bindable such that they receive bi-directional updates.
        /// We will take on any value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        public virtual void BindTo(Bindable<T> them)
        {
            Value = them.Value;

            AddWeakReference(them.WeakReference);
            them.AddWeakReference(WeakReference);
        }

        protected void AddWeakReference(WeakReference<Bindable<T>> weakReference) => bindings.Add(weakReference);

        public virtual bool Parse(object s)
        {
            if (s is T)
                Value = (T)s;
            else if (typeof(T).IsEnum && s is string)
                Value = (T)Enum.Parse(typeof(T), (string)s);
            else
                return false;

            return true;
        }

        public void TriggerChange()
        {
            ValueChanged?.Invoke(this, null);

            foreach (var w in bindings.ToArray())
            {
                Bindable<T> b;
                if (w.TryGetTarget(out b))
                    b.Value = value;
                else
                    bindings.Remove(w);
            }

        }

        public void UnbindAll()
        {
            ValueChanged = null;
        }

        public string Description { get; set; }

        public override string ToString()
        {
            return value?.ToString() ?? string.Empty;
        }

        internal void Reset()
        {
            Value = Default;
        }

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        public Bindable<T> GetBoundCopy()
        {
            var copy = MemberwiseClone() as Bindable<T>;

            copy.bindings = new List<WeakReference<Bindable<T>>>();
            copy.BindTo(this);

            return copy;
        }
    }
}
