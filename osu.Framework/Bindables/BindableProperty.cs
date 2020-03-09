// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Represents a property that contains guards on setting a new value and propagates the change to all of <see cref="Source"/>'s bindings.
    /// </summary>
    /// <typeparam name="T">The property value type.</typeparam>
    public class BindableProperty<T> : IBindableProperty<T>
    {
        public IBindable Source { get; }

        /// <summary>
        /// Invoked when a value has changed without any propagation rejection.
        /// Used for invoking events indicating value changes.
        /// </summary>
        public Action<T, T> OnValueChange;

        /// <summary>
        /// Gets a <see cref="BindableProperty{T}"/> of a provided bindable equivalent to this property for propagating value to, or null if not found.
        /// </summary>
        private readonly Func<IBindable, BindableProperty<T>> getPropertyOf;

        private T value;

        object IBindableProperty.Value => Value;

        /// <inheritdoc />
        /// <remarks>
        /// This is guarded by <see cref="Source"/> checks to ensure changing value safely.
        /// </remarks>
        public T Value
        {
            get => value;
            set
            {
                Set(value);
            }
        }

        public BindableProperty(T initialValue, IBindable source, Func<IBindable, BindableProperty<T>> getPropertyOf)
        {
            Source = source;
            this.getPropertyOf = getPropertyOf;

            value = initialValue;
        }

        /// <summary>
        /// Sets current value to <paramref name="newValue"/> and propagates the change to <see cref="Source"/>'s <see cref="IBindable.Bindings"/>.
        /// </summary>
        /// <remarks>
        /// This sets the current value without checking from <see cref="Source"/>.
        /// </remarks>
        /// <param name="newValue">The new value, used for <see cref="OnValueChange"/> invocation and for propagation.</param>
        public void Set(T newValue) => Set(value, newValue, Source, Source);

        /// <summary>
        /// Triggers <see cref="OnValueChange"/> from <paramref name="oldValue"/> to current value (<see cref="Value"/>) and propagates the change to <see cref="Source"/>'s <see cref="IBindable.Bindings"/>.
        /// </summary>
        /// <param name="oldValue">The old value, used for <see cref="OnValueChange"/> invocation and for propagation.</param>
        /// <param name="propagateChange">Whether to propagate the change to <see cref="Source"/>'s <see cref="IBindable.Bindings"/>.</param>
        public void TriggerChange(T oldValue, bool propagateChange = true) => TriggerChange(oldValue, Source, Source, propagateChange);

        /// <summary>
        /// Sets current value to <paramref name="newValue"/> and propagates the change to <see cref="Source"/>'s <see cref="IBindable.Bindings"/>.
        /// </summary>
        /// <remarks>
        /// This sets the current value without checking from <see cref="Source"/>.
        /// </remarks>
        /// <param name="oldValue">The old value, used for <see cref="OnValueChange"/> invocation and for propagation.</param>
        /// <param name="newValue">The new value, used for <see cref="OnValueChange"/> invocation and for propagation.</param>
        /// <param name="propagationRoot">The root bindable of this propagation.</param>
        /// <param name="triggeringBindable">The bindable triggering this change.</param>
        protected virtual void Set(T oldValue, T newValue, IBindable propagationRoot, IBindable triggeringBindable)
        {
            if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
                return;

            value = newValue;
            TriggerChange(oldValue, propagationRoot, triggeringBindable);
        }

        /// <summary>
        /// Triggers <see cref="OnValueChange"/> from <paramref name="oldValue"/> to current value (<see cref="Value"/>) and propagates the change to <see cref="Source"/>'s <see cref="IBindable.Bindings"/>.
        /// </summary>
        /// <param name="oldValue">The old value, used for <see cref="OnValueChange"/> invocation and for propagation.</param>
        /// <param name="propagationRoot">The root bindable of this propagation.</param>
        /// <param name="triggeringBindable">The bindable triggering this change.</param>
        /// <param name="propagateChange">Whether to propagate the change to <see cref="Source"/>'s <see cref="IBindable.Bindings"/>.</param>
        protected void TriggerChange(T oldValue, IBindable propagationRoot, IBindable triggeringBindable, bool propagateChange = true)
        {
            var beforePropagation = value;

            if (Source.Bindings != null && propagateChange)
            {
                foreach (var b in Source.Bindings)
                {
                    if (ReferenceEquals(b, triggeringBindable)) continue;

                    getPropertyOf(b)?.Set(oldValue, value, propagationRoot, Source);
                }
            }

            // Check if the value hasn't changed during propagation to avoid firing already outdated events.
            if (EqualityComparer<T>.Default.Equals(beforePropagation, value))
                OnValueChange?.Invoke(oldValue, value);
        }
    }
}
