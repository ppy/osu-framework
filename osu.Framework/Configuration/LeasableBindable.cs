// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// A bindable which encapsulates another bindable and provides a mutex access pattern to value changes.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    public class LeasableBindable<T> : IMutableBindable<T>
    {
        private readonly IMutableBindable<T> underlyingBindable;

        private bool isLeased => leasedBindable != null;

        private LeasedBindable<T> leasedBindable;
        private T valueBeforeLease;
        private bool revertValueOnReturn;

        public LeasableBindable(IMutableBindable<T> underlyingBindable)
        {
            this.underlyingBindable = underlyingBindable;
        }

        public LeasedBindable<T> BeginLease(bool revertValueOnReturn)
        {
            if (isLeased)
                throw new InvalidOperationException("Attempted to lease a bindable that is already in a leased state.");

            if (revertValueOnReturn)
            {
                this.revertValueOnReturn = true;
                valueBeforeLease = Value;
            }

            underlyingBindable.Disabled = true;
            return leasedBindable = new LeasedBindable<T>(underlyingBindable, this);
        }

        internal void EndLease(IMutableBindable<T> returnedBindable)
        {
            if (!isLeased)
                throw new InvalidOperationException("Attempted to end a lease without beginning one.");

            if (returnedBindable != leasedBindable)
                throw new InvalidOperationException("Attempted to end a lease but returned a different bindable to the one used to start the lease.");

            leasedBindable = null;
            underlyingBindable.Disabled = false;
            if (revertValueOnReturn)
            {
                Value = valueBeforeLease;

                revertValueOnReturn = false;
                valueBeforeLease = default;
            }
        }

        public void Parse(object input) => underlyingBindable.Parse(input);

        public event Action<bool> DisabledChanged
        {
            add => underlyingBindable.DisabledChanged += value;
            remove => underlyingBindable.DisabledChanged -= value;
        }

        public void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false) => underlyingBindable.BindDisabledChanged(onChange, runOnceImmediately);

        public bool Disabled
        {
            get => underlyingBindable.Disabled;
            set
            {
                throwIfLeased();
                underlyingBindable.Disabled = value;
            }
        }

        public bool IsDefault => underlyingBindable.IsDefault;

        public void UnbindEvents() => underlyingBindable.UnbindEvents();

        public void UnbindBindings() => underlyingBindable.UnbindBindings();

        public void UnbindAll() => underlyingBindable.UnbindAll();

        public void UnbindFrom(IUnbindable them) => underlyingBindable.UnbindFrom(them);

        public string Description => underlyingBindable.Description;

        public void BindTo(IBindable them) => underlyingBindable.BindTo(them);

        IBindable<T> IBindable<T>.GetBoundCopy() => underlyingBindable.GetBoundCopy();

        public IMutableBindable<T> GetBoundCopy() => underlyingBindable.GetBoundCopy();

        public T Value
        {
            get => underlyingBindable.Value;
            set => underlyingBindable.Value = value;
        }

        public T Default
        {
            get => underlyingBindable.Default;
            set => underlyingBindable.Default = value;
        }

        public event Action<T> ValueChanged
        {
            add => underlyingBindable.ValueChanged += value;
            remove => underlyingBindable.ValueChanged -= value;
        }

        public void BindTo(IBindable<T> them) => underlyingBindable.BindTo(them);

        public void BindValueChanged(Action<T> onChange, bool runOnceImmediately = false) => underlyingBindable.BindValueChanged(onChange, runOnceImmediately);

        IBindable IBindable.GetBoundCopy() => ((IBindable)underlyingBindable).GetBoundCopy();

        public WeakReference<Bindable<T>> WeakReference => underlyingBindable.WeakReference;
        public void AddWeakReference(WeakReference<Bindable<T>> weakReference) => underlyingBindable.AddWeakReference(weakReference);

        private void throwIfLeased()
        {
            if (isLeased)
                throw new InvalidOperationException($"Cannot perform this operation on a {nameof(LeasableBindable<T>)} that is currently in a leased state.");
        }
    }
}
