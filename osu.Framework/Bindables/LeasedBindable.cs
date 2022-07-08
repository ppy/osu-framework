// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// A bindable carrying a mutually exclusive lease on another bindable.
    /// Can only be retrieved via <see cref="Bindable{T}.BeginLease"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LeasedBindable<T> : Bindable<T>, ILeasedBindable<T>
    {
        private readonly Bindable<T> source;

        private readonly T valueBeforeLease;
        private readonly bool disabledBeforeLease;
        private readonly bool revertValueOnReturn;

        internal LeasedBindable([NotNull] Bindable<T> source, bool revertValueOnReturn)
        {
            BindTo(source);

            this.source = source ?? throw new ArgumentNullException(nameof(source));

            if (revertValueOnReturn)
            {
                this.revertValueOnReturn = true;
                valueBeforeLease = Value;
            }

            disabledBeforeLease = Disabled;

            Disabled = true;
        }

        private LeasedBindable(T defaultValue = default)
            : base(defaultValue)
        {
            // used for GetBoundCopy, where we don't want a source.
        }

        private bool hasBeenReturned;

        public bool Return()
        {
            if (hasBeenReturned)
                return false;

            if (source == null)
                throw new InvalidOperationException($"Must {nameof(Return)} from original leased source");

            UnbindAll();
            return true;
        }

        public override T Value
        {
            get => base.Value;
            set
            {
                if (source != null)
                    checkValid();

                if (EqualityComparer<T>.Default.Equals(Value, value)) return;

                SetValue(base.Value, value, true);
            }
        }

        public override T Default
        {
            get => base.Default;
            set
            {
                if (source != null)
                    checkValid();

                if (EqualityComparer<T>.Default.Equals(Default, value)) return;

                SetDefaultValue(base.Default, value, true);
            }
        }

        public override bool Disabled
        {
            get => base.Disabled;
            set
            {
                if (source != null)
                    checkValid();

                if (Disabled == value) return;

                SetDisabled(value, true);
            }
        }

        internal override void UnbindAllInternal()
        {
            if (source != null && !hasBeenReturned)
            {
                if (revertValueOnReturn)
                    Value = valueBeforeLease;

                Disabled = disabledBeforeLease;

                source.EndLease(this);
                hasBeenReturned = true;
            }

            base.UnbindAllInternal();
        }

        protected override Bindable<T> CreateInstance() => new LeasedBindable<T>();

        private void checkValid()
        {
            if (source != null && hasBeenReturned)
                throw new InvalidOperationException($"Cannot perform operations on a {nameof(LeasedBindable<T>)} that has been {nameof(Return)}ed.");
        }
    }
}
