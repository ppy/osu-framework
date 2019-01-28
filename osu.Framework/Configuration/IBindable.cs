﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Configuration
{
    /// <summary>
    /// An interface which can be bound to other <see cref="IBindable"/>s in order to watch for (and react to) <see cref="IBindable.Disabled"/> changes.
    /// </summary>
    public interface IBindable : IParseable, ICanBeDisabled, IHasDefaultValue, IUnbindable, IHasDescription
    {
        /// <summary>
        /// Binds ourselves to another bindable such that we receive any value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindable them);

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        IBindable GetBoundCopy();
    }

    /// <summary>
    /// An interface which can be bound to other <see cref="IBindable{T}"/>s in order to watch for (and react to) <see cref="IBindable{T}.Disabled"/> and <see cref="IBindable{T}.Value"/> changes.
    /// </summary>
    /// <typeparam name="T">The type of value encapsulated by this <see cref="IBindable{T}"/>.</typeparam>
    public interface IBindable<T> : IBindable
    {
        /// <summary>
        /// An event which is raised when <see cref="Value"/> has changed.
        /// </summary>
        event Action<T> ValueChanged;

        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// The default value of this bindable. Used when querying <see cref="IBindable{T}.IsDefault"/>.
        /// </summary>
        T Default { get; }

        /// <summary>
        /// Binds ourselves to another bindable such that we receive any values and value limitations of the bindable we bind width.
        /// </summary>
        /// <param name="them">The foreign bindable. This should always be the most permanent end of the bind (ie. a ConfigManager)</param>
        void BindTo(IBindable<T> them);

        /// <summary>
        /// Bind an action to <see cref="ValueChanged"/> with the option of running the bound action once immediately.
        /// </summary>
        /// <param name="onChange">The action to perform when <see cref="Value"/> changes.</param>
        /// <param name="runOnceImmediately">Whether the action provided in <see cref="onChange"/> should be run once immediately.</param>
        void BindValueChanged(Action<T> onChange, bool runOnceImmediately = false);

        /// <summary>
        /// Retrieve a new bindable instance weakly bound to the configuration backing.
        /// If you are further binding to events of a bindable retrieved using this method, ensure to hold
        /// a local reference.
        /// </summary>
        /// <returns>A weakly bound copy of the specified bindable.</returns>
        new IBindable<T> GetBoundCopy();
    }
}
