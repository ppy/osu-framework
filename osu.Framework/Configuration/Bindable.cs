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

        /// <summary>
        /// Welds two bindables together such that they update each other and stay in sync.
        /// </summary>
        /// <param name="v">The foreign bindable to weld.</param>
        /// <param name="transferValue">Whether we should transfer the value from the foreign bindable on weld.</param>
        public virtual void Weld(Bindable<T> v, bool transferValue = true)
        {
            if (transferValue) Value = v.Value;

            ValueChanged += delegate { v.Value = Value; };
            v.ValueChanged += delegate { Value = v.Value; };
        }

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
    }
}
