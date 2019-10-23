// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Newtonsoft.Json;
using osu.Framework.Caching;
using osu.Framework.IO.Serialization;
using osu.Framework.Lists;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// A generic implementation of a <see cref="IBindable"/>
    /// </summary>
    /// <typeparam name="T">The type of our stored <see cref="Value"/>.</typeparam>
    public class Bindable<T> : IBindable<T>, ISerializableBindable
    {
        /// <summary>
        /// An event which is raised when <see cref="Value"/> has changed (or manually via <see cref="TriggerValueChange"/>).
        /// </summary>
        public event Action<ValueChangedEvent<T>> ValueChanged;

        /// <summary>
        /// An event which is raised when <see cref="Disabled"/>'s state has changed (or manually via <see cref="TriggerDisabledChange"/>).
        /// </summary>
        public event Action<bool> DisabledChanged;

        private T value;

        /// <summary>
        /// The default value of this bindable. Used when calling <see cref="SetDefault"/> or querying <see cref="IsDefault"/>.
        /// </summary>
        public T Default { get; set; }

        private bool disabled;

        /// <summary>
        /// Whether this bindable has been disabled. When disabled, attempting to change the <see cref="Value"/> will result in an <see cref="InvalidOperationException"/>.
        /// </summary>
        public virtual bool Disabled
        {
            get => disabled;
            set
            {
                // if a lease is active, disabled can *only* be changed by that leased bindable.
                throwIfLeased();

                if (disabled == value) return;

                SetDisabled(value);
            }
        }

        internal void SetDisabled(bool value, bool bypassChecks = false, Bindable<T> source = null)
        {
            if (!bypassChecks)
                throwIfLeased();

            disabled = value;
            TriggerDisabledChange(source ?? this, true, bypassChecks);
        }

        /// <summary>
        /// Check whether the current <see cref="Value"/> is equal to <see cref="Default"/>.
        /// </summary>
        public virtual bool IsDefault => EqualityComparer<T>.Default.Equals(value, Default);

        /// <summary>
        /// Revert the current <see cref="Value"/> to the defined <see cref="Default"/>.
        /// </summary>
        public void SetDefault() => Value = Default;

        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        public virtual T Value
        {
            get => value;
            set
            {
                // intentionally don't have throwIfLeased() here.
                // if the leased bindable decides to disable exclusive access (by setting Disabled = false) then anything will be able to write to Value.

                if (Disabled)
                    throw new InvalidOperationException($"Can not set value to \"{value.ToString()}\" as bindable is disabled.");

                if (EqualityComparer<T>.Default.Equals(this.value, value)) return;

                SetValue(this.value, value);
            }
        }

        internal void SetValue(T previousValue, T value, bool bypassChecks = false, Bindable<T> source = null)
        {
            this.value = value;
            TriggerValueChange(previousValue, source ?? this, true, bypassChecks);
        }

        private readonly Cached<WeakReference<Bindable<T>>> weakReferenceCache = new Cached<WeakReference<Bindable<T>>>();

        private WeakReference<Bindable<T>> weakReference => weakReferenceCache.IsValid ? weakReferenceCache.Value : weakReferenceCache.Value = new WeakReference<Bindable<T>>(this);

        /// <summary>
        /// Creates a new bindable instance. This is used for deserialization of bindables.
        /// </summary>
        [UsedImplicitly]
        private Bindable()
            : this(default)
        {
        }

        /// <summary>
        /// Creates a new bindable instance.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public Bindable(T value = default)
        {
            this.value = value;
        }

        protected LockedWeakList<Bindable<T>> Bindings { get; private set; }

        void IBindable.BindTo(IBindable them)
        {
            if (!(them is Bindable<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        void IBindable<T>.BindTo(IBindable<T> them)
        {
            if (!(them is Bindable<T> tThem))
                throw new InvalidCastException($"Can't bind to a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            BindTo(tThem);
        }

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        public Bindable<T> BindTarget
        {
            set => BindTo(value);
        }

        /// <summary>
        /// Binds this bindable to another such that bi-directional updates are propagated.
        /// This will adopt any values and value limitations of the bindable bound to.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager).</param>
        public virtual void BindTo(Bindable<T> them)
        {
            Value = them.Value;
            Disabled = them.Disabled;
            Default = them.Default;

            addWeakReference(them.weakReference);
            them.addWeakReference(weakReference);
        }

        /// <summary>
        /// Bind an action to <see cref="ValueChanged"/> with the option of running the bound action once immediately.
        /// </summary>
        /// <param name="onChange">The action to perform when <see cref="Value"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <see cref="onChange"/> should be run once immediately.</param>
        public void BindValueChanged(Action<ValueChangedEvent<T>> onChange, bool runOnceImmediately = false)
        {
            ValueChanged += onChange;
            if (runOnceImmediately)
                onChange(new ValueChangedEvent<T>(Value, Value));
        }

        /// <summary>
        /// Bind an action to <see cref="DisabledChanged"/> with the option of running the bound action once immediately.
        /// </summary>
        /// <param name="onChange">The action to perform when <see cref="Disabled"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <see cref="onChange"/> should be run once immediately.</param>
        public void BindDisabledChanged(Action<bool> onChange, bool runOnceImmediately = false)
        {
            DisabledChanged += onChange;
            if (runOnceImmediately)
                onChange(Disabled);
        }

        private void addWeakReference(WeakReference<Bindable<T>> weakReference)
        {
            if (Bindings == null)
                Bindings = new LockedWeakList<Bindable<T>>();

            Bindings.Add(weakReference);
        }

        private void removeWeakReference(WeakReference<Bindable<T>> weakReference) => Bindings?.Remove(weakReference);

        /// <summary>
        /// Parse an object into this instance.
        /// An object deriving T can be parsed, or a string can be parsed if T is an enum type.
        /// </summary>
        /// <param name="input">The input which is to be parsed.</param>
        public virtual void Parse(object input)
        {
            switch (input)
            {
                case T t:
                    Value = t;
                    break;

                case string s:
                    var underlyingType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

                    if (underlyingType.IsEnum)
                        Value = (T)Enum.Parse(underlyingType, s);
                    else
                        Value = (T)Convert.ChangeType(s, underlyingType, CultureInfo.InvariantCulture);
                    break;

                default:
                    throw new ArgumentException($@"Could not parse provided {input.GetType()} ({input}) to {typeof(T)}.");
            }
        }

        /// <summary>
        /// Raise <see cref="ValueChanged"/> and <see cref="DisabledChanged"/> once, without any changes actually occurring.
        /// This does not propagate to any outward bound bindables.
        /// </summary>
        public virtual void TriggerChange()
        {
            TriggerValueChange(value, this, false);
            TriggerDisabledChange(this, false);
        }

        protected void TriggerValueChange(T previousValue, Bindable<T> source, bool propagateToBindings = true, bool bypassChecks = false)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            T beforePropagation = value;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    b.SetValue(previousValue, value, bypassChecks, this);
                }
            }

            if (EqualityComparer<T>.Default.Equals(beforePropagation, value))
                ValueChanged?.Invoke(new ValueChangedEvent<T>(previousValue, value));
        }

        protected void TriggerDisabledChange(Bindable<T> source, bool propagateToBindings = true, bool bypassChecks = false)
        {
            // check a bound bindable hasn't changed the value again (it will fire its own event)
            bool beforePropagation = disabled;

            if (propagateToBindings && Bindings != null)
            {
                foreach (var b in Bindings)
                {
                    if (b == source) continue;

                    b.SetDisabled(disabled, bypassChecks, this);
                }
            }

            if (beforePropagation == disabled)
                DisabledChanged?.Invoke(disabled);
        }

        /// <summary>
        /// Unbind any events bound to <see cref="ValueChanged"/> and <see cref="DisabledChanged"/>.
        /// </summary>
        public void UnbindEvents()
        {
            ValueChanged = null;
            DisabledChanged = null;
        }

        /// <summary>
        /// Remove all bound <see cref="Bindable{T}"/>s via <see cref="GetBoundCopy"/> or <see cref="BindTo"/>.
        /// </summary>
        public void UnbindBindings()
        {
            if (Bindings == null)
                return;

            // ToArray required as this may be called from an async disposal thread.
            // This can lead to deadlocks since each child is also enumerating its Bindings.
            foreach (var b in Bindings.ToArray())
                b.Unbind(this);

            Bindings.Clear();
        }

        protected void Unbind(Bindable<T> binding) => Bindings.Remove(binding.weakReference);

        /// <summary>
        /// Calls <see cref="UnbindEvents"/> and <see cref="UnbindBindings"/>.
        /// Also returns any active lease.
        /// </summary>
        public virtual void UnbindAll()
        {
            if (isLeased)
                leasedBindable.Return();

            UnbindEvents();
            UnbindBindings();
        }

        public void UnbindFrom(IUnbindable them)
        {
            if (!(them is Bindable<T> tThem))
                throw new InvalidCastException($"Can't unbind a bindable of type {them.GetType()} from a bindable of type {GetType()}.");

            removeWeakReference(tThem.weakReference);
            tThem.removeWeakReference(weakReference);
        }

        public string Description { get; set; }

        public override string ToString() => value?.ToString() ?? string.Empty;

        /// <summary>
        /// Create an unbound clone of this bindable.
        /// </summary>
        public Bindable<T> GetUnboundCopy()
        {
            var clone = GetBoundCopy();
            clone.UnbindAll();
            return clone;
        }

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        public Bindable<T> GetBoundCopy()
        {
            var copy = (Bindable<T>)Activator.CreateInstance(GetType(), Value);
            copy.BindTo(this);
            return copy;
        }

        IBindable IBindable.GetBoundCopy() => GetBoundCopy();

        IBindable<T> IBindable<T>.GetBoundCopy() => GetBoundCopy();

        void ISerializableBindable.SerializeTo(JsonWriter writer, JsonSerializer serializer)
        {
            serializer.Serialize(writer, Value);
        }

        void ISerializableBindable.DeserializeFrom(JsonReader reader, JsonSerializer serializer)
        {
            Value = serializer.Deserialize<T>(reader);
        }

        private LeasedBindable<T> leasedBindable;

        private bool isLeased => leasedBindable != null;

        /// <summary>
        /// Takes out a mutually exclusive lease on this bindable.
        /// During a lease, the bindable will be set to <see cref="Disabled"/>, but changes can still be applied via the <see cref="LeasedBindable{T}"/> returned by this call.
        /// You should end a lease by calling <see cref="LeasedBindable{T}.Return"/> when done.
        /// </summary>
        /// <param name="revertValueOnReturn">Whether the <see cref="Value"/> when <see cref="BeginLease"/> was called should be restored when the lease ends.</param>
        /// <returns>A bindable with a lease.</returns>
        public LeasedBindable<T> BeginLease(bool revertValueOnReturn)
        {
            if (checkForLease(this))
                throw new InvalidOperationException("Attempted to lease a bindable that is already in a leased state.");

            return leasedBindable = new LeasedBindable<T>(this, revertValueOnReturn);
        }

        private bool checkForLease(Bindable<T> source)
        {
            if (isLeased)
                return true;

            if (Bindings == null)
                return false;

            bool found = false;

            foreach (var b in Bindings)
            {
                if (b != source)
                    found |= b.checkForLease(this);
            }

            return found;
        }

        /// <summary>
        /// Called internally by a <see cref="LeasedBindable{T}"/> to end a lease.
        /// </summary>
        /// <param name="returnedBindable">The <see cref="LeasedBindable{T}"/> that was provided as a return of a <see cref="BeginLease"/> call.</param>
        internal void EndLease(Bindable<T> returnedBindable)
        {
            if (!isLeased)
                throw new InvalidOperationException("Attempted to end a lease without beginning one.");

            if (returnedBindable != leasedBindable)
                throw new InvalidOperationException("Attempted to end a lease but returned a different bindable to the one used to start the lease.");

            leasedBindable = null;
        }

        private void throwIfLeased()
        {
            if (isLeased)
                throw new InvalidOperationException($"Cannot perform this operation on a {nameof(Bindable<T>)} that is currently in a leased state.");
        }
    }
}
