// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    public abstract class BindableNumberWithPrecision<T> : BindableNumber<T>
        where T : struct
    {
        /// <summary>
        /// An event which is raised when <see cref="Precision"/> has changed (or manually via <see cref="TriggerPrecisionChange"/>).
        /// </summary>
        public event Action<T> PrecisionChanged;

        private T precision;

        /// <summary>
        /// The precision up to which the value of this bindable should be rounded.
        /// </summary>
        public T Precision
        {
            get => precision;
            set
            {
                if (precision.Equals(value))
                    return;
                precision = value;

                TriggerPrecisionChange();
            }
        }

        protected BindableNumberWithPrecision(T value = default(T))
            : base(value)
        {
            precision = DefaultPrecision;
        }

        public override void TriggerChange()
        {
            base.TriggerChange();

            TriggerPrecisionChange(false);
        }

        protected void TriggerPrecisionChange(bool propagateToBindings = true)
        {
            PrecisionChanged?.Invoke(MinValue);
            if (propagateToBindings) Bindings?.ForEachAlive(b =>
            {
                if (b is BindableNumberWithPrecision<T> other)
                    other.Precision = Precision;
            });
        }

        protected abstract T DefaultPrecision { get; }
    }
}
