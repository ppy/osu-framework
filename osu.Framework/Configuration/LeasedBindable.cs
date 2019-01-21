// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;

namespace osu.Framework.Configuration
{
    public class LeasedBindable<T> : Bindable<T>
    {
        private readonly Bindable<T> source;

        internal LeasedBindable([NotNull] Bindable<T> source)
        {
            BindTo(source);
            this.source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public LeasedBindable(T value) : base(value)
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
            get => source == null ? base.Value : source.Value;
            set
            {
                if (source == null)
                    SetValue(value, true);
                else
                {
                    checkValid();
                    source.SetValue(value, true, this);
                }
            }
        }

        public override bool Disabled
        {
            get => source?.Disabled ?? base.Disabled;
            set
            {
                if (source == null)
                    SetDisabled(value, true);
                else
                {
                    checkValid();
                    source.SetDisabled(value, true, this);
                }
            }
        }

        public override void UnbindAll()
        {
            if (source != null && !hasBeenReturned)
            {
                source.EndLease(this);
                hasBeenReturned = true;
            }

            base.UnbindAll();
        }

        private void checkValid()
        {
            if (source != null && hasBeenReturned)
                throw new InvalidOperationException($"Cannot perform operations on a {nameof(LeasedBindable<T>)} that has been {nameof(Return)}ed.");
        }
    }
}