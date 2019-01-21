// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    public class LeasedBindable<T> : IMutableBindable<T>
    {
        private readonly Bindable<T> source;

        public LeasedBindable(Bindable<T> source)
        {
            this.source = source;
        }

        private bool hasBeenReturned;

        /// <summary>
        /// End the lease on the source <see cref="Bindable{T}"/>.
        /// </summary>
        public void Return()
        {
            if (hasBeenReturned)
                throw new InvalidOperationException($"This bindable has already been {nameof(Return)}ed.");

            UnbindAll();
            source.EndLease(this);
            hasBeenReturned = true;
        }

        public void Parse(object input) => source.Parse(input);

        public event Action<bool> DisabledChanged
        {
            add
            {
                checkValid();
                source.DisabledChanged += value;
            }
            remove
            {
                checkValid();
                source.DisabledChanged -= value;
            }
        }

        public void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false)
        {
            checkValid();
            source.BindDisabledChanged(onChange, runOnceImmediately);
        }

        public bool Disabled
        {
            get
            {
                checkValid();
                return source.Disabled;
            }
            set
            {
                checkValid();
                source.SetDisabled(value);
            }
        }

        public bool IsDefault
        {
            get
            {
                checkValid();
                return source.IsDefault;
            }
        }

        public void UnbindEvents()
        {
            checkValid();
            source.UnbindEvents();
        }

        public void UnbindBindings()
        {
            checkValid();
            source.UnbindBindings();
        }

        public void UnbindAll()
        {
            checkValid();
            source.UnbindAll();
        }

        public void UnbindFrom(IUnbindable them)
        {
            checkValid();
            source.UnbindFrom(them);
        }

        public string Description
        {
            get
            {
                checkValid();
                return source.Description;
            }
        }

        public void BindTo(IBindable them)
        {
            checkValid();
            ((IBindable)source).BindTo(them);
        }

        IBindable<T> IBindable<T>.GetBoundCopy()
        {
            return source.GetBoundCopy();
        }

        IBindable IBindable.GetBoundCopy()
        {
            return ((IBindable)source).GetBoundCopy();
        }

        public IMutableBindable<T> GetBoundCopy()
        {
            checkValid();
            return source.GetBoundCopy();
        }

        public event Action<T> ValueChanged
        {
            add
            {
                checkValid();
                source.ValueChanged += value;
            }

            remove
            {
                checkValid();
                source.ValueChanged -= value;
            }
        }

        public T Value
        {
            get
            {
                checkValid();
                return source.Value;
            }

            set
            {
                checkValid();
                source.SetValue(value);
            }
        }

        public T Default
        {
            get
            {
                checkValid();
                return source.Default;
            }

            set
            {
                checkValid();
                source.Default = value;
            }
        }

        public WeakReference<Bindable<T>> WeakReference => source.WeakReference;
        public void AddWeakReference(WeakReference<Bindable<T>> weakReference) => source.AddWeakReference(weakReference);

        public void BindTo(IBindable<T> them)
        {
            checkValid();
            ((IBindable<T>)source).BindTo(them);
        }

        public void BindValueChanged(Action<T> onChange, bool runOnceImmediately = false)
        {
            checkValid();
            source.BindValueChanged(onChange, runOnceImmediately);
        }

        private void checkValid()
        {
            if (hasBeenReturned)
                throw new InvalidOperationException($"Cannot perform operations on a {nameof(LeasedBindable<T>)} that has been {nameof(Return)}ed.");
        }
    }
}