// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    public class LeasedBindableNumber<T> : BindableNumber<T>, ILeasedBindable
        where T : struct, IConvertible, IComparable<T>, IEquatable<T>
    {
        private readonly BindableNumber<T> source;

        private readonly T valueBeforeLease, minValueBeforeLease, maxValueBeforeLease, precisionBeforeLease;
        private readonly bool disabledBeforeLease;
        private readonly bool revertPropertiesOnReturn;

        internal LeasedBindableNumber([NotNull] BindableNumber<T> source, bool revertPropertiesOnReturn)
        {
            BindTo(source);

            this.source = source ?? throw new ArgumentNullException(nameof(source));

            if (revertPropertiesOnReturn)
            {
                this.revertPropertiesOnReturn = true;
                precisionBeforeLease = Precision;
                minValueBeforeLease = MinValue;
                maxValueBeforeLease = MaxValue;
                valueBeforeLease = Value;
            }

            disabledBeforeLease = Disabled;

            Disabled = true;
        }

        [UsedImplicitly]
        public LeasedBindableNumber(T value)
            : base(value)
        {
            // used for GetBoundCopy, where we don't want a source.
        }

        private bool hasBeenReturned;

        /// <summary>
        /// End the lease on the source <see cref="Bindable{T}"/>.
        /// </summary>
        public void Return()
        {
            if (source == null)
                throw new InvalidOperationException($"Must {nameof(Return)} from original leased source");

            if (hasBeenReturned)
                throw new InvalidOperationException($"This bindable has already been {nameof(Return)}ed.");

            UnbindAll();
        }

        public override T Value
        {
            get => base.Value;
            set => setLeased(Value, value, () => SetValue(value, true));
        }

        public override T Default
        {
            get => base.Default;
            set => setLeased(Default, value, () => SetDefaultValue(Default, value, true));
        }

        public override bool Disabled
        {
            get => base.Disabled;
            set => setLeased(Disabled, value, () => SetDisabled(value, true));
        }

        public override T Precision
        {
            get => base.Precision;
            set => setLeased(Precision, value, () => SetPrecision(value, true, true));
        }

        public override T MinValue
        {
            get => base.MinValue;
            set => setLeased(base.MinValue, value, () => SetMinValue(value, true, true));
        }

        public override T MaxValue
        {
            get => base.MaxValue;
            set => setLeased(base.MaxValue, value, () => SetMaxValue(value, true, true));
        }

        public override void UnbindAll()
        {
            if (source != null && !hasBeenReturned)
            {
                if (revertPropertiesOnReturn)
                {
                    Precision = precisionBeforeLease;
                    MinValue = minValueBeforeLease;
                    MaxValue = maxValueBeforeLease;
                    Value = valueBeforeLease;
                }

                Disabled = disabledBeforeLease;

                source.EndLease(this);
                hasBeenReturned = true;
            }

            base.UnbindAll();
        }

        private void setLeased<TValue>(TValue previous, TValue current, Action setProperty)
        {
            if (source != null && hasBeenReturned)
                throw new InvalidOperationException($"Cannot perform operations on a {nameof(LeasedBindable<T>)} that has been {nameof(Return)}ed.");

            if (EqualityComparer<TValue>.Default.Equals(previous, current)) return;

            setProperty.Invoke();
        }
    }
}
