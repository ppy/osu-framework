// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Represents a <see cref="Vector2d"/> bindable with defined component-wise constraints applied to it.
    /// </summary>
    public class BindableVector2d : RangeConstrainedBindable<Vector2d>
    {
        public event Action<double>? PrecisionChanged;
        
        private double precision = double.Epsilon;

        public BindableVector2d(Vector2d defaultValue = default)
            : base(defaultValue)
        {
            setValue(defaultValue);
        }

        public double Precision
        {
            get => precision;
            set
            {
                if (precision.Equals(value))
                    return;

                if (value.CompareTo(default) <= 0)
                    throw new ArgumentOutOfRangeException(nameof(Precision), value, "Must be greater than 0.");

                SetPrecision(value, true, this);
            }
        }

        /// <summary>
        /// Sets the precision. This method does no equality comparisons.
        /// </summary>
        /// <param name="precision">The new precision.</param>
        /// <param name="updateCurrentValue">Whether to update the current value after the precision is set.</param>
        /// <param name="source">The bindable that triggered this. A null value represents the current bindable instance.</param>
        internal void SetPrecision(double precision, bool updateCurrentValue, BindableVector2d source)
        {
            this.precision = precision;
            TriggerPrecisionChange(source);

            if (updateCurrentValue)
            {
                // Re-apply the current value to apply the new precision
                setValue(Value);
            }
        }

        public override Vector2d Value
        {
            get => base.Value;
            set => setValue(value);
        }

        private void setValue(Vector2d value)
        {
            if (Precision.CompareTo(DefaultPrecision) > 0)
            {
                Vector2d Vector2dValue = ClampValue(value, MinValue, MaxValue);
                Vector2dValue.X = Math.Round(Vector2dValue.X / Precision) * Precision;
                Vector2dValue.Y = Math.Round(Vector2dValue.Y / Precision) * Precision;
            }
            else
                base.Value = value;
        }
       
        protected override Vector2d DefaultMinValue => new Vector2d(double.MinValue, double.MinValue);
        protected override Vector2d DefaultMaxValue => new Vector2d(double.MaxValue, double.MaxValue);
        
        /// <summary>
        /// The default <see cref="Precision"/>.
        /// </summary>
        protected virtual double DefaultPrecision => double.Epsilon;

        public override void TriggerChange()
        {
            base.TriggerChange();

            TriggerPrecisionChange(this, false);
        }

        protected void TriggerPrecisionChange(BindableVector2d source, bool propagateToBindings = true)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            double beforePropagation = precision;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    if (b is BindableVector2d bn)
                        bn.SetPrecision(precision, false, this);
                }
            }

            if (beforePropagation.Equals(precision))
                PrecisionChanged?.Invoke(precision);
        }

        public override void UnbindEvents()
        {
            base.UnbindEvents();

            PrecisionChanged = null;
        }

        public override string ToString(string format, IFormatProvider formatProvider) => ((FormattableString)$"({Value.X}x{Value.Y})").ToString(formatProvider);

        protected override Bindable<Vector2d> CreateInstance() => new BindableVector2d();

        protected sealed override Vector2d ClampValue(Vector2d value, Vector2d minValue, Vector2d maxValue)
        {
            return new Vector2d
            {
                X = Math.Clamp(value.X, minValue.X, maxValue.X),
                Y = Math.Clamp(value.Y, minValue.Y, maxValue.Y)
            };
        }

        protected sealed override bool IsValidRange(Vector2d min, Vector2d max) => min.X <= max.X && min.Y <= max.Y;
    }
}
