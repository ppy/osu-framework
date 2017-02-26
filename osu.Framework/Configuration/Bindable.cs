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

        List<WeakReference<Bindable<T>>> welded = new List<WeakReference<Bindable<T>>>();

        public WeakReference<Bindable<T>> WeakReference => new WeakReference<Bindable<T>>(this);

        /// <summary>
        /// Welds two bindables together such that they update each other and stay in sync.
        /// </summary>
        /// <param name="them">The foreign bindable to weld. This should always be the most permanent end of the weld (ie. a ConfigManager)</param>
        /// <param name="transferValue">Whether we should transfer the value from the foreign bindable on weld.</param>
        public virtual void Weld(Bindable<T> them, bool transferValue = true)
        {
            if (them == null) return;

            if (transferValue) Value = them.Value;

            AddWeakReference(them.WeakReference);
            them.AddWeakReference(WeakReference);
        }

        protected void AddWeakReference(WeakReference<Bindable<T>> weakReference) => welded.Add(weakReference);

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

            foreach (var w in welded.ToArray())
            {
                Bindable<T> b;
                if (w.TryGetTarget(out b))
                    b.Value = value;
                else
                    welded.Remove(w);
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

        public Bindable<T> GetWelded()
        {
            var clone = MemberwiseClone() as Bindable<T>;

            clone.welded = new List<WeakReference<Bindable<T>>>();
            clone.Weld(this);

            return clone;
        }
    }
}
