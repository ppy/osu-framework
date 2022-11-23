// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Globalization;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Utils;

namespace osu.Framework.Bindables
{
    /// <summary>
    /// An interface which can be bound to other <see cref="IBindable"/>s in order to watch for (and react to) <see cref="ICanBeDisabled.Disabled">Disabled</see> changes.
    /// </summary>
    public interface IBindable : ICanBeDisabled, IHasDefaultValue, IUnbindable, IHasDescription, IFormattable
    {
        /// <summary>
        /// Binds ourselves to another bindable such that we receive any value limitations of the bindable we bind with.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindable them);

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        sealed IBindable BindTarget
        {
            set => BindTo(value);
        }

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to instantiate a copy bindable that's not matching the original's type.</exception>
        IBindable GetBoundCopy();

        /// <summary>
        /// Creates a new instance of this <see cref="IBindable"/> for use in <see cref="GetBoundCopy"/>.
        /// The returned instance must have match the most derived type of the bindable class this method is implemented on.
        /// </summary>
        protected IBindable CreateInstance();

        /// <summary>
        /// Helper method which implements <see cref="GetBoundCopy"/> for use in final classes.
        /// </summary>
        /// <param name="source">The source <see cref="IBindable"/>.</param>
        /// <typeparam name="T">The bindable type.</typeparam>
        /// <returns>The bound copy.</returns>
        protected static T GetBoundCopyImplementation<T>(T source)
            where T : IBindable
        {
            var copy = source.CreateInstance();

            if (copy.GetType() != source.GetType())
            {
                ThrowHelper.ThrowInvalidOperationException($"Attempted to create a copy of {source.GetType().ReadableName()}, but the returned instance type was {copy.GetType().ReadableName()}. "
                                                           + $"Override {source.GetType().ReadableName()}.{nameof(CreateInstance)}() for {nameof(GetBoundCopy)}() to function properly.");
            }

            copy.BindTo(source);
            return (T)copy;
        }

        string ToString() => ToString(null, CultureInfo.CurrentCulture);

        string ToString(IFormatProvider formatProvider) => ToString(null, formatProvider);
    }

    /// <summary>
    /// An interface which can be bound to other <see cref="IBindable{T}"/>s in order to watch for (and react to) <see cref="ICanBeDisabled.Disabled">Disabled</see> and <see cref="IBindable{T}.Value">Value</see> changes.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    public interface IBindable<T> : ICanBeDisabled, IHasDefaultValue, IUnbindable, IHasDescription, IFormattable
    {
        /// <summary>
        /// An event which is raised when <see cref="Value"/> has changed.
        /// </summary>
        event Action<ValueChangedEvent<T>> ValueChanged;

        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// The default value of this bindable. Used when querying <see cref="IHasDefaultValue.IsDefault">IsDefault</see>.
        /// </summary>
        T Default { get; }

        /// <summary>
        /// Binds ourselves to another bindable such that we receive any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindable<T> them);

        /// <summary>
        /// An alias of <see cref="BindTo"/> provided for use in object initializer scenarios.
        /// Passes the provided value as the foreign (more permanent) bindable.
        /// </summary>
        IBindable<T> BindTarget
        {
            set => BindTo(value);
        }

        /// <summary>
        /// Bind an action to <see cref="ValueChanged"/> with the option of running the bound action once immediately.
        /// </summary>
        /// <param name="onChange">The action to perform when <see cref="Value"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <paramref name="onChange"/> should be run once immediately.</param>
        void BindValueChanged(Action<ValueChangedEvent<T>> onChange, bool runOnceImmediately = false);

        /// <inheritdoc cref="IBindable.GetBoundCopy"/>
        IBindable<T> GetBoundCopy();

        string ToString() => ToString(null, CultureInfo.CurrentCulture);

        string ToString(IFormatProvider formatProvider) => ToString(null, formatProvider);
    }
}
