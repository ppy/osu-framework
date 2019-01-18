// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    public class LeasedBindable<T> : IMutableBindable<T>
    {
        private readonly IMutableBindable<T> underlyingBindable;
        private readonly LeasableBindable<T> source;

        public LeasedBindable(IMutableBindable<T> underlyingBindable, LeasableBindable<T> source)
        {
            this.underlyingBindable = underlyingBindable.GetBoundCopy();
            this.source = source;
        }

        private bool hasBeenReturned;

        /// <summary>
        /// End the lease on the source <see cref="LeasableBindable{T}"/>.
        /// </summary>
        public void Return()
        {
            if (hasBeenReturned)
                throw new InvalidOperationException($"This bindable has already been {nameof(Return)}ed.");

            UnbindAll();
            source.EndLease(this);
            hasBeenReturned = true;
        }

        public void Parse(object input)
        {
            underlyingBindable.Parse(input);
        }

        public event Action<bool> DisabledChanged
        {
            add
            {
                checkValid();
                underlyingBindable.DisabledChanged += value;
            }
            remove
            {
                checkValid();
                underlyingBindable.DisabledChanged -= value;
            }
        }

        public void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false)
        {
            checkValid();
            underlyingBindable.BindDisabledChanged(onChange, runOnceImmediately);
        }

        public bool Disabled
        {
            get
            {
                checkValid();
                return underlyingBindable.Disabled;
            }
            set
            {
                checkValid();
                underlyingBindable.Disabled = value;
            }
        }

        public bool IsDefault
        {
            get
            {
                checkValid();
                return underlyingBindable.IsDefault;
            }
        }

        public void UnbindEvents()
        {
            checkValid();
            underlyingBindable.UnbindEvents();
        }

        public void UnbindBindings()
        {
            checkValid();
            underlyingBindable.UnbindBindings();
        }

        public void UnbindAll()
        {
            checkValid();
            underlyingBindable.UnbindAll();
        }

        public void UnbindFrom(IUnbindable them)
        {
            checkValid();
            underlyingBindable.UnbindFrom(them);
        }

        public string Description
        {
            get
            {
                checkValid();
                return underlyingBindable.Description;
            }
        }

        public void BindTo(IBindable them)
        {
            checkValid();
            underlyingBindable.BindTo(them);
        }

        IBindable<T> IBindable<T>.GetBoundCopy()
        {
            return underlyingBindable.GetBoundCopy();
        }

        IBindable IBindable.GetBoundCopy()
        {
            return ((IBindable)underlyingBindable).GetBoundCopy();
        }

        public IMutableBindable<T> GetBoundCopy()
        {
            checkValid();
            return underlyingBindable.GetBoundCopy();
        }

        public event Action<T> ValueChanged
        {
            add
            {
                checkValid();
                underlyingBindable.ValueChanged += value;
            }

            remove
            {
                checkValid();
                underlyingBindable.ValueChanged -= value;
            }
        }

        public T Value
        {
            get
            {
                checkValid();
                return underlyingBindable.Value;
            }

            set
            {
                checkValid();
                underlyingBindable.Disabled = false;
                underlyingBindable.Value = value;
                underlyingBindable.Disabled = true;
            }
        }

        public T Default
        {
            get
            {
                checkValid();
                return underlyingBindable.Default;
            }

            set
            {
                checkValid();
                underlyingBindable.Default = value;
            }
        }

        public WeakReference<Bindable<T>> WeakReference => underlyingBindable.WeakReference;
        public void AddWeakReference(WeakReference<Bindable<T>> weakReference) => underlyingBindable.AddWeakReference(weakReference);

        public void BindTo(IBindable<T> them)
        {
            checkValid();
            underlyingBindable.BindTo(them);
        }

        public void BindValueChanged(Action<T> onChange, bool runOnceImmediately = false)
        {
            checkValid();
            underlyingBindable.BindValueChanged(onChange, runOnceImmediately);
        }

        private void checkValid()
        {
            if (hasBeenReturned)
                throw new InvalidOperationException($"Cannot perform operations on a {nameof(LeasedBindable<T>)} that has been {nameof(Return)}ed.");
        }
    }
}